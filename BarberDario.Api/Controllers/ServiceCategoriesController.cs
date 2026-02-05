using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceCategoriesController : ControllerBase
{
    private readonly BarberDarioDbContext _context;
    private readonly ILogger<ServiceCategoriesController> _logger;

    public ServiceCategoriesController(BarberDarioDbContext context, ILogger<ServiceCategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active service categories with service count
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetCategories()
    {
        var categories = await _context.ServiceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.Icon,
                c.Color,
                c.DisplayOrder,
                c.Services.Count(s => s.IsActive)
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} active service categories", categories.Count);

        return Ok(categories);
    }

    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategoryDto>> GetCategory(Guid id)
    {
        var category = await _context.ServiceCategories
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.Icon,
                c.Color,
                c.DisplayOrder,
                c.Services.Count(s => s.IsActive)
            ))
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        return Ok(category);
    }

    /// <summary>
    /// Get all services in a specific category
    /// </summary>
    [HttpGet("{id}/services")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ServiceDto>>> GetCategoryServices(Guid id)
    {
        var categoryExists = await _context.ServiceCategories
            .AnyAsync(c => c.Id == id && c.IsActive);

        if (!categoryExists)
        {
            return NotFound(new { message = "Category not found" });
        }

        var services = await _context.Services
            .Include(s => s.Category)
            .Where(s => s.CategoryId == id && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder,
                s.CategoryId,
                s.Category != null ? s.Category.Name : null
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} services for category {CategoryId}", services.Count, id);

        return Ok(services);
    }

    /// <summary>
    /// Create a new service category (Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceCategoryDto>> CreateCategory([FromBody] CreateServiceCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Category name is required" });
        }

        var category = new Data.Entities.ServiceCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            Color = dto.Color,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        _context.ServiceCategories.Add(category);
        await _context.SaveChangesAsync();

        var result = new ServiceCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Icon,
            category.Color,
            category.DisplayOrder,
            0 // New category has no services yet
        );

        _logger.LogInformation("Created new service category: {CategoryName} ({CategoryId})", category.Name, category.Id);

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, result);
    }

    /// <summary>
    /// Update an existing service category (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateServiceCategoryDto dto)
    {
        var category = await _context.ServiceCategories.FindAsync(id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        // Update only provided fields
        if (dto.Name != null) category.Name = dto.Name;
        if (dto.Description != null) category.Description = dto.Description;
        if (dto.Icon != null) category.Icon = dto.Icon;
        if (dto.Color != null) category.Color = dto.Color;
        if (dto.DisplayOrder.HasValue) category.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;

        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var serviceCount = await _context.Services
            .CountAsync(s => s.CategoryId == id && s.IsActive);

        var result = new ServiceCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Icon,
            category.Color,
            category.DisplayOrder,
            serviceCount
        );

        _logger.LogInformation("Updated service category: {CategoryName} ({CategoryId})", category.Name, category.Id);

        return Ok(result);
    }

    /// <summary>
    /// Delete a service category (Admin only) - Soft delete by setting IsActive = false
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        var activeServicesCount = category.Services.Count(s => s.IsActive);
        if (activeServicesCount > 0)
        {
            return BadRequest(new
            {
                message = $"Cannot delete category with {activeServicesCount} active services. Remove or reassign services first."
            });
        }

        // Soft delete
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted service category: {CategoryName} ({CategoryId})", category.Name, category.Id);

        return NoContent();
    }
}
