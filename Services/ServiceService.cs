using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
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

    // For public booking - show ALL active services (both general and employee-specific)
    public async Task<List<ServiceDto>> GetServicesAsync(Guid? employeeId = null)
    {
        var query = _context.Services
            .Where(s => s.IsActive)
            .AsQueryable();

        // For public booking (employeeId == null), show ALL services
        // For employee portal (employeeId has value), show only their services
        if (employeeId.HasValue)
        {
            // Employee portal - only show services assigned to this employee
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        }
        // For public booking - show ALL services (don't filter by EmployeeId)

        var services = await query
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

        _logger.LogInformation("Retrieved {Count} services for employee {EmployeeId}",
            services.Count, employeeId);
        return services;
    }

    // For public booking - show ALL categories with ALL services
    public async Task<List<ServiceCategoryDto>> GetServiceCategoriesAsync(Guid? employeeId = null)
    {
        var categoriesQuery = _context.ServiceCategories
            .Where(c => c.IsActive)
            .Include(c => c.Services.Where(s => s.IsActive))
            .AsQueryable();

        var categories = await categoriesQuery
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.DisplayOrder,
                c.IsActive,
                c.Services
                    .Where(s => s.IsActive)
                    // For public booking (employeeId == null), include ALL services
                    // For employee portal, filter by employee
                    .Where(s => !employeeId.HasValue || s.EmployeeId == employeeId.Value)
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

        // Only filter out empty categories for employee portal, not for public
        if (employeeId.HasValue)
        {
            categories = categories.Where(c => c.Services.Any()).ToList();
        }

        _logger.LogInformation("Retrieved {Count} service categories for employee {EmployeeId}",
            categories.Count, employeeId);
        return categories;
    }

    public async Task<(List<ServiceDto>? Services, bool CategoryExists)> GetServicesByCategoryAsync(
        Guid categoryId,
        Guid? employeeId = null)
    {
        var category = await _context.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);

        if (category == null)
        {
            return (null, false);
        }

        var query = _context.Services
            .Where(s => s.CategoryId == categoryId && s.IsActive);

        // For public booking (employeeId == null), show ALL services in category
        // For employee portal, filter by employee
        if (employeeId.HasValue)
        {
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        }

        var services = await query
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

        _logger.LogInformation("Retrieved {Count} services for category {CategoryId} and employee {EmployeeId}",
            services.Count, categoryId, employeeId);

        return (services, true);
    }

    public async Task<ServiceWithCategoryDto?> GetServiceDetailsAsync(Guid id, Guid? employeeId = null)
    {
        var query = _context.Services
            .Include(s => s.Category)
            .Include(s => s.Employee)
            .Where(s => s.Id == id && s.IsActive);

        // For employee portal, verify access
        if (employeeId.HasValue)
        {
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        }

        var service = await query
            .Select(s => new ServiceWithCategoryDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder,
                s.CategoryId,
                s.Category.Name,
                s.Employee != null ? new EmployeeBasicDto(
                    s.Employee.Id,
                    s.Employee.Name,
                    s.Employee.Role,
                    s.Employee.Specialty
                ) : null
            ))
            .FirstOrDefaultAsync();

        return service;
    }

    public async Task<List<ServiceDto>> GetEmployeeServicesAsync(Guid employeeId)
    {
        var services = await _context.Services
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
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

        _logger.LogInformation("Retrieved {Count} services for employee {EmployeeId}",
            services.Count, employeeId);
        return services;
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

        return categories;
    }

    public async Task<object> GetServicesSummaryAsync(Guid? employeeId = null)
    {
        var query = _context.Services.Where(s => s.IsActive);

        if (employeeId.HasValue)
        {
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        }
        // For public, include ALL services

        var totalServices = await query.CountAsync();
        var totalCategories = await _context.ServiceCategories.CountAsync(c => c.IsActive);

        var categoryBreakdown = await _context.ServiceCategories
            .Where(c => c.IsActive)
            .Select(c => new
            {
                CategoryName = c.Name,
                ServiceCount = c.Services.Count(s => s.IsActive &&
                    (!employeeId.HasValue || s.EmployeeId == employeeId.Value)),
                AveragePrice = c.Services
                    .Where(s => s.IsActive &&
                        (!employeeId.HasValue || s.EmployeeId == employeeId.Value))
                    .Average(s => (double?)s.Price) ?? 0,
                MinPrice = c.Services
                    .Where(s => s.IsActive &&
                        (!employeeId.HasValue || s.EmployeeId == employeeId.Value))
                    .Min(s => (decimal?)s.Price) ?? 0,
                MaxPrice = c.Services
                    .Where(s => s.IsActive &&
                        (!employeeId.HasValue || s.EmployeeId == employeeId.Value))
                    .Max(s => (decimal?)s.Price) ?? 0
            })
            .Where(c => c.ServiceCount > 0)
            .ToListAsync();

        return new
        {
            TotalServices = totalServices,
            TotalCategories = totalCategories,
            CategoryBreakdown = categoryBreakdown,
            ForEmployee = employeeId,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<bool> AssignServiceToEmployeeAsync(Guid serviceId, Guid employeeId)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);

        if (service == null)
            throw new ArgumentException("Service not found");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.IsActive);

        if (employee == null)
            throw new ArgumentException("Employee not found");

        service.EmployeeId = employeeId;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Service {ServiceId} assigned to employee {EmployeeId}",
            serviceId, employeeId);

        return true;
    }

    public async Task<bool> RemoveServiceFromEmployeeAsync(Guid serviceId)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
            throw new ArgumentException("Service not found");

        service.EmployeeId = null;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Service {ServiceId} removed from employee", serviceId);

        return true;
    }
}