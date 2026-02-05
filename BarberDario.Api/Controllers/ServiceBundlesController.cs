using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceBundlesController : ControllerBase
{
    private readonly BarberDarioDbContext _context;
    private readonly ILogger<ServiceBundlesController> _logger;

    public ServiceBundlesController(BarberDarioDbContext context, ILogger<ServiceBundlesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active bundles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceBundleDto>>> GetBundles([FromQuery] bool includeExpired = false)
    {
        var query = _context.ServiceBundles
            .Include(b => b.BundleItems)
            .ThenInclude(i => i.Service)
            .Where(b => b.IsActive);

        if (!includeExpired)
        {
            var now = DateTime.UtcNow;
            query = query.Where(b =>
                (b.ValidFrom == null || b.ValidFrom <= now) &&
                (b.ValidUntil == null || b.ValidUntil >= now)
            );
        }

        var bundles = await query
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new ServiceBundleDto(
                b.Id,
                b.Name,
                b.Description,
                b.OriginalPrice,
                b.BundlePrice,
                b.DiscountPercentage,
                b.OriginalPrice - b.BundlePrice,
                b.TotalDurationMinutes,
                b.DisplayOrder,
                b.ValidFrom,
                b.ValidUntil,
                (b.ValidFrom == null || b.ValidFrom <= DateTime.UtcNow) &&
                (b.ValidUntil == null || b.ValidUntil >= DateTime.UtcNow),
                b.BundleItems.OrderBy(i => i.DisplayOrder).Select(i => new BundleItemDto(
                    i.ServiceId,
                    i.Service.Name,
                    i.Service.Description,
                    i.Service.DurationMinutes,
                    i.Service.Price,
                    i.Quantity,
                    i.DisplayOrder,
                    i.Notes
                )).ToList()
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} active bundles", bundles.Count);

        return Ok(bundles);
    }

    /// <summary>
    /// Get a specific bundle by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceBundleDto>> GetBundle(Guid id)
    {
        var bundle = await _context.ServiceBundles
            .Include(b => b.BundleItems)
            .ThenInclude(i => i.Service)
            .Where(b => b.Id == id && b.IsActive)
            .Select(b => new ServiceBundleDto(
                b.Id,
                b.Name,
                b.Description,
                b.OriginalPrice,
                b.BundlePrice,
                b.DiscountPercentage,
                b.OriginalPrice - b.BundlePrice,
                b.TotalDurationMinutes,
                b.DisplayOrder,
                b.ValidFrom,
                b.ValidUntil,
                (b.ValidFrom == null || b.ValidFrom <= DateTime.UtcNow) &&
                (b.ValidUntil == null || b.ValidUntil >= DateTime.UtcNow),
                b.BundleItems.OrderBy(i => i.DisplayOrder).Select(i => new BundleItemDto(
                    i.ServiceId,
                    i.Service.Name,
                    i.Service.Description,
                    i.Service.DurationMinutes,
                    i.Service.Price,
                    i.Quantity,
                    i.DisplayOrder,
                    i.Notes
                )).ToList()
            ))
            .FirstOrDefaultAsync();

        if (bundle == null)
        {
            return NotFound(new { message = "Bundle not found" });
        }

        return Ok(bundle);
    }

    /// <summary>
    /// Create a new service bundle (Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceBundleDto>> CreateBundle([FromBody] CreateServiceBundleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Bundle name is required" });
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Bundle must contain at least one service" });
        }

        // Validate all services exist
        var serviceIds = dto.Items.Select(i => i.ServiceId).Distinct().ToList();
        var services = await _context.Services
            .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync();

        if (services.Count != serviceIds.Count)
        {
            return BadRequest(new { message = "One or more services not found or inactive" });
        }

        // Calculate original price and total duration
        decimal originalPrice = 0;
        int totalDuration = 0;

        foreach (var item in dto.Items)
        {
            var service = services.First(s => s.Id == item.ServiceId);
            originalPrice += service.Price * item.Quantity;
            totalDuration += service.DurationMinutes * item.Quantity;
        }

        // Calculate discount percentage
        var discountPercentage = originalPrice > 0
            ? Math.Round((originalPrice - dto.BundlePrice) / originalPrice * 100, 2)
            : 0;

        // Create bundle
        var bundle = new ServiceBundle
        {
            Name = dto.Name,
            Description = dto.Description,
            OriginalPrice = originalPrice,
            BundlePrice = dto.BundlePrice,
            DiscountPercentage = discountPercentage,
            TotalDurationMinutes = totalDuration,
            DisplayOrder = dto.DisplayOrder,
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            TermsAndConditions = dto.TermsAndConditions,
            IsActive = true
        };

        _context.ServiceBundles.Add(bundle);

        // Create bundle items
        foreach (var itemDto in dto.Items)
        {
            var item = new ServiceBundleItem
            {
                BundleId = bundle.Id,
                ServiceId = itemDto.ServiceId,
                Quantity = itemDto.Quantity,
                DisplayOrder = itemDto.DisplayOrder,
                Notes = itemDto.Notes
            };
            _context.ServiceBundleItems.Add(item);
        }

        await _context.SaveChangesAsync();

        // Reload with items
        var result = await GetBundle(bundle.Id);

        _logger.LogInformation("Created new bundle: {BundleName} ({BundleId})", bundle.Name, bundle.Id);

        return CreatedAtAction(nameof(GetBundle), new { id = bundle.Id }, (result as OkObjectResult)?.Value);
    }

    /// <summary>
    /// Update an existing service bundle (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceBundleDto>> UpdateBundle(Guid id, [FromBody] UpdateServiceBundleDto dto)
    {
        var bundle = await _context.ServiceBundles
            .Include(b => b.BundleItems)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bundle == null)
        {
            return NotFound(new { message = "Bundle not found" });
        }

        // Update basic properties
        if (dto.Name != null) bundle.Name = dto.Name;
        if (dto.Description != null) bundle.Description = dto.Description;
        if (dto.DisplayOrder.HasValue) bundle.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.ValidFrom.HasValue) bundle.ValidFrom = dto.ValidFrom;
        if (dto.ValidUntil.HasValue) bundle.ValidUntil = dto.ValidUntil;
        if (dto.TermsAndConditions != null) bundle.TermsAndConditions = dto.TermsAndConditions;
        if (dto.IsActive.HasValue) bundle.IsActive = dto.IsActive.Value;

        // Update items if provided
        if (dto.Items != null)
        {
            // Validate services
            var serviceIds = dto.Items.Select(i => i.ServiceId).Distinct().ToList();
            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
                .ToListAsync();

            if (services.Count != serviceIds.Count)
            {
                return BadRequest(new { message = "One or more services not found or inactive" });
            }

            // Remove old items
            _context.ServiceBundleItems.RemoveRange(bundle.BundleItems);

            // Recalculate prices and duration
            decimal originalPrice = 0;
            int totalDuration = 0;

            foreach (var itemDto in dto.Items)
            {
                var service = services.First(s => s.Id == itemDto.ServiceId);
                originalPrice += service.Price * itemDto.Quantity;
                totalDuration += service.DurationMinutes * itemDto.Quantity;

                var item = new ServiceBundleItem
                {
                    BundleId = bundle.Id,
                    ServiceId = itemDto.ServiceId,
                    Quantity = itemDto.Quantity,
                    DisplayOrder = itemDto.DisplayOrder,
                    Notes = itemDto.Notes
                };
                _context.ServiceBundleItems.Add(item);
            }

            bundle.OriginalPrice = originalPrice;
            bundle.TotalDurationMinutes = totalDuration;

            // Use new bundle price if provided, otherwise recalculate based on discount
            if (dto.BundlePrice.HasValue)
            {
                bundle.BundlePrice = dto.BundlePrice.Value;
            }

            bundle.DiscountPercentage = originalPrice > 0
                ? Math.Round((originalPrice - bundle.BundlePrice) / originalPrice * 100, 2)
                : 0;
        }
        else if (dto.BundlePrice.HasValue)
        {
            // Just update price, recalculate discount
            bundle.BundlePrice = dto.BundlePrice.Value;
            bundle.DiscountPercentage = bundle.OriginalPrice > 0
                ? Math.Round((bundle.OriginalPrice - bundle.BundlePrice) / bundle.OriginalPrice * 100, 2)
                : 0;
        }

        bundle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = await GetBundle(id);

        _logger.LogInformation("Updated bundle: {BundleName} ({BundleId})", bundle.Name, bundle.Id);

        return Ok((result as OkObjectResult)?.Value);
    }

    /// <summary>
    /// Delete a service bundle (Admin only) - Soft delete
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBundle(Guid id)
    {
        var bundle = await _context.ServiceBundles
            .Include(b => b.Bookings)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bundle == null)
        {
            return NotFound(new { message = "Bundle not found" });
        }

        // Check for active bookings
        var activeBookingsCount = bundle.Bookings.Count(b =>
            b.Status != BookingStatus.Cancelled &&
            b.BookingDate >= DateOnly.FromDateTime(DateTime.UtcNow)
        );

        if (activeBookingsCount > 0)
        {
            return BadRequest(new
            {
                message = $"Cannot delete bundle with {activeBookingsCount} active future bookings. Cancel bookings first."
            });
        }

        // Soft delete
        bundle.IsActive = false;
        bundle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted bundle: {BundleName} ({BundleId})", bundle.Name, bundle.Id);

        return NoContent();
    }
}
