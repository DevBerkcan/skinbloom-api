using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly BarberDarioDbContext _context;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(BarberDarioDbContext context, ILogger<ServicesController> logger)
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
            .Include(s => s.Category)
            .Where(s => s.IsActive)
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

        _logger.LogInformation("Retrieved {Count} active services", services.Count);

        return Ok(services);
    }

    /// <summary>
    /// Get a specific service by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetService(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Where(s => s.Id == id && s.IsActive)
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
            .FirstOrDefaultAsync();

        if (service == null)
        {
            return NotFound(new { message = "Service not found" });
        }

        return Ok(service);
    }
}
