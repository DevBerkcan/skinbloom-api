using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Skinbloom.Api.Services;

public class ServiceService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<ServiceService> _logger;

    public ServiceService(SkinbloomDbContext context, ILogger<ServiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ServiceDto>> GetServicesAsync()
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
        return services;
    }

    public async Task<List<ServiceCategoryDto>> GetServiceCategoriesAsync()
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
        return categories;
    }

    public async Task<(List<ServiceDto>? Services, bool CategoryExists)> GetServicesByCategoryAsync(Guid categoryId)
    {
        var category = await _context.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);

        if (category == null)
        {
            return (null, false);
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

        return (services, true);
    }

    public async Task<ServiceWithCategoryDto?> GetServiceDetailsAsync(Guid id)
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

        return service;
    }

    public async Task<List<ServiceCategoryDto>> GetAllCategoriesAsync()
    {
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

        return categories;
    }

    public async Task<object> GetServicesSummaryAsync()
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

        return new
        {
            TotalServices = totalServices,
            TotalCategories = totalCategories,
            CategoryBreakdown = categoryBreakdown,
            LastUpdated = DateTime.UtcNow
        };
    }
}