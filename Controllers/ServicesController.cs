using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(SkinbloomDbContext context, ILogger<ServicesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active services
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} active services", services.Count);

        return Ok(services);
    }

    /// <summary>
    /// Get all active service categories with their services
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetServiceCategories()
    {
        var categories = await _context.ServiceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.DisplayOrder,
                c.IsActive,
                c.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new ServiceDto(
                        s.Id,
                        s.Name,
                        s.Description,
                        s.DurationMinutes,
                        s.Price,
                        s.DisplayOrder
                    ))
                    .ToList()
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} service categories", categories.Count);

        return Ok(categories);
    }

    /// <summary>
    /// Get services by category ID
    /// </summary>
    [HttpGet("services/{categoryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ServiceDto>>> GetServicesByCategory(Guid categoryId)
    {
        var category = await _context.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        var services = await _context.Services
            .Where(s => s.CategoryId == categoryId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} services for category {CategoryId}", 
            services.Count, categoryId);

        return Ok(services);
    }

    /// <summary>
    /// Get a specific service by ID with category information
    /// </summary>
    [HttpGet("{id}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceWithCategoryDto>> GetServiceDetails(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Where(s => s.Id == id && s.IsActive)
            .Select(s => new ServiceWithCategoryDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder,
                s.CategoryId,
                s.Category.Name
            ))
            .FirstOrDefaultAsync();

        if (service == null)
        {
            return NotFound(new { message = "Service not found" });
        }

        return Ok(service);
    }


    /// <summary>
    /// Get all categories (including inactive ones) - Admin only
    /// </summary>
    [HttpGet("admin/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetAllCategories()
    {
        // TODO: Add admin authorization
        var categories = await _context.ServiceCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.DisplayOrder,
                c.IsActive,
                c.Services
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new ServiceDto(
                        s.Id,
                        s.Name,
                        s.Description,
                        s.DurationMinutes,
                        s.Price,
                        s.DisplayOrder
                    ))
                    .ToList()
            ))
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Get services summary with category counts
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetServicesSummary()
    {
        var totalServices = await _context.Services.CountAsync(s => s.IsActive);
        var totalCategories = await _context.ServiceCategories.CountAsync(c => c.IsActive);
        
        var categoryBreakdown = await _context.ServiceCategories
            .Where(c => c.IsActive)
            .Select(c => new
            {
                CategoryName = c.Name,
                ServiceCount = c.Services.Count(s => s.IsActive),
                AveragePrice = c.Services.Where(s => s.IsActive).Average(s => (double?)s.Price) ?? 0,
                TotalRevenue = c.Services.Where(s => s.IsActive).Sum(s => s.Price)
            })
            .ToListAsync();

        return Ok(new
        {
            TotalServices = totalServices,
            TotalCategories = totalCategories,
            CategoryBreakdown = categoryBreakdown,
            LastUpdated = DateTime.UtcNow
        });
    }
}