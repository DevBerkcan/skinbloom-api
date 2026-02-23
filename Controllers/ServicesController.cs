using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Skinbloom.Api.Services;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly ServiceService _serviceService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(ServiceService serviceService, ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active services
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        var services = await _serviceService.GetServicesAsync();
        return Ok(services);
    }

    /// <summary>
    /// Get all active service categories with their services
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetServiceCategories()
    {
        var categories = await _serviceService.GetServiceCategoriesAsync();
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
        var (services, categoryExists) = await _serviceService.GetServicesByCategoryAsync(categoryId);

        if (!categoryExists)
        {
            return NotFound(new { message = "Category not found" });
        }

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
        var service = await _serviceService.GetServiceDetailsAsync(id);

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
        var categories = await _serviceService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Get services summary with category counts
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetServicesSummary()
    {
        var summary = await _serviceService.GetServicesSummaryAsync();
        return Ok(summary);
    }
}