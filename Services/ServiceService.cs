using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Skinbloom.Api.Data.Entities;

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

    // Add these methods to your existing ServiceService class

    // ── ADMIN METHODS ──────────────────────────────────────────────

    public async Task<List<AdminServiceDto>> GetAllServicesAdminAsync()
    {
        var services = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.Employee)
            .OrderBy(s => s.Category.DisplayOrder)
            .ThenBy(s => s.DisplayOrder)
            .Select(s => new AdminServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder,
                s.CategoryId,
                s.Category.Name,
                s.EmployeeId,
                s.Employee != null ? s.Employee.Name : null,
                s.IsActive
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} services for admin", services.Count);
        return services;
    }

    public async Task<AdminServiceDto?> GetServiceByIdAdminAsync(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
            return null;

        return new AdminServiceDto(
            service.Id,
            service.Name,
            service.Description,
            service.DurationMinutes,
            service.Price,
            service.DisplayOrder,
            service.CategoryId,
            service.Category.Name,
            service.EmployeeId,
            service.Employee?.Name,
            service.IsActive
        );
    }

    public async Task<AdminServiceDto> CreateServiceAsync(CreateServiceDto dto)
    {
        // Validate category
        var category = await _context.ServiceCategories.FindAsync(dto.CategoryId);
        if (category == null)
            throw new ArgumentException("Kategorie nicht gefunden");

        // Validate employee if provided
        if (dto.EmployeeId.HasValue)
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId.Value);
            if (employee == null)
                throw new ArgumentException("Mitarbeiter nicht gefunden");
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price,
            DisplayOrder = dto.DisplayOrder,
            CategoryId = dto.CategoryId,
            EmployeeId = dto.EmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new service: {ServiceName}", service.Name);

        return await GetServiceByIdAdminAsync(service.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created service");
    }

    public async Task<AdminServiceDto> UpdateServiceAsync(Guid id, UpdateServiceDto dto)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            throw new ArgumentException("Service nicht gefunden");

        // Validate category
        var category = await _context.ServiceCategories.FindAsync(dto.CategoryId);
        if (category == null)
            throw new ArgumentException("Kategorie nicht gefunden");

        // Validate employee if provided
        if (dto.EmployeeId.HasValue)
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId.Value);
            if (employee == null)
                throw new ArgumentException("Mitarbeiter nicht gefunden");
        }

        // Check if service has bookings and is being deactivated
        if (!dto.IsActive && service.IsActive)
        {
            var hasFutureBookings = await _context.Bookings
                .AnyAsync(b => b.ServiceId == id &&
                              b.BookingDate >= DateOnly.FromDateTime(DateTime.UtcNow) &&
                              b.Status != BookingStatus.Cancelled);

            if (hasFutureBookings)
                throw new InvalidOperationException("Kann Service mit zukünftigen Buchungen nicht deaktivieren");
        }

        service.Name = dto.Name;
        service.Description = dto.Description;
        service.DurationMinutes = dto.DurationMinutes;
        service.Price = dto.Price;
        service.DisplayOrder = dto.DisplayOrder;
        service.CategoryId = dto.CategoryId;
        service.EmployeeId = dto.EmployeeId;
        service.IsActive = dto.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated service: {ServiceName}", service.Name);

        return await GetServiceByIdAdminAsync(service.Id)
            ?? throw new InvalidOperationException("Failed to retrieve updated service");
    }

    public async Task<bool> DeleteServiceAsync(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
            throw new ArgumentException("Service nicht gefunden");

        // Check if service has any bookings
        if (service.Bookings.Any())
            throw new InvalidOperationException("Kann Service mit bestehenden Buchungen nicht löschen. Deaktivieren Sie ihn stattdessen.");

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted service: {ServiceName}", service.Name);
        return true;
    }

    public async Task<bool> ToggleServiceActiveAsync(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
            throw new ArgumentException("Service nicht gefunden");

        // If trying to deactivate, check for future bookings
        if (service.IsActive)
        {
            var hasFutureBookings = await _context.Bookings
                .AnyAsync(b => b.ServiceId == id &&
                              b.BookingDate >= DateOnly.FromDateTime(DateTime.UtcNow) &&
                              b.Status != BookingStatus.Cancelled);

            if (hasFutureBookings)
                throw new InvalidOperationException("Kann Service mit zukünftigen Buchungen nicht deaktivieren");
        }

        service.IsActive = !service.IsActive;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Toggled service active status: {ServiceName} -> {IsActive}",
            service.Name, service.IsActive);

        return service.IsActive;
    }

    // ── CATEGORY ADMIN METHODS ─────────────────────────────────────

    public async Task<List<AdminServiceCategoryDto>> GetAllCategoriesAdminAsync()
    {
        var categories = await _context.ServiceCategories
            .Include(c => c.Services)
                .ThenInclude(s => s.Employee)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new AdminServiceCategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.DisplayOrder,
                c.IsActive,
                c.Services.Select(s => new AdminServiceDto(
                    s.Id,
                    s.Name,
                    s.Description,
                    s.DurationMinutes,
                    s.Price,
                    s.DisplayOrder,
                    s.CategoryId,
                    c.Name,
                    s.EmployeeId,
                    s.Employee != null ? s.Employee.Name : null,
                    s.IsActive
                )).ToList()
            ))
            .ToListAsync();

        return categories;
    }

    public async Task<AdminServiceCategoryDto?> GetCategoryByIdAdminAsync(Guid id)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
                .ThenInclude(s => s.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return null;

        return new AdminServiceCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.DisplayOrder,
            category.IsActive,
            category.Services.Select(s => new AdminServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder,
                s.CategoryId,
                category.Name,
                s.EmployeeId,
                s.Employee != null ? s.Employee.Name : null,
                s.IsActive
            )).ToList()
        );
    }

    public async Task<AdminServiceCategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        // Check if category with same name exists
        var existing = await _context.ServiceCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower());

        if (existing != null)
            throw new ArgumentException("Eine Kategorie mit diesem Namen existiert bereits");

        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ServiceCategories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new category: {CategoryName}", category.Name);

        return await GetCategoryByIdAdminAsync(category.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created category");
    }

    public async Task<AdminServiceCategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _context.ServiceCategories.FindAsync(id);
        if (category == null)
            throw new ArgumentException("Kategorie nicht gefunden");

        // Check if another category with same name exists
        var existing = await _context.ServiceCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id);

        if (existing != null)
            throw new ArgumentException("Eine andere Kategorie mit diesem Namen existiert bereits");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.DisplayOrder = dto.DisplayOrder;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated category: {CategoryName}", category.Name);

        return await GetCategoryByIdAdminAsync(category.Id)
            ?? throw new InvalidOperationException("Failed to retrieve updated category");
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new ArgumentException("Kategorie nicht gefunden");

        // Check if category has any services
        if (category.Services.Any())
            throw new InvalidOperationException("Kann Kategorie mit bestehenden Services nicht löschen");

        _context.ServiceCategories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted category: {CategoryName}", category.Name);
        return true;
    }

    // ── EMPLOYEE ASSIGNMENT METHODS ────────────────────────────────

    public async Task<List<EmployeeForAssignmentDto>> GetEmployeesForAssignmentAsync()
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeForAssignmentDto(
                e.Id,
                e.Name,
                e.Role,
                e.Specialty,
                e.Services.Count(s => s.IsActive)
            ))
            .ToListAsync();

        return employees;
    }

    public async Task<List<AdminServiceDto>> GetServicesByEmployeeAsync(Guid employeeId)
    {
        var services = await _context.Services
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .Include(s => s.Category)
            .Include(s => s.Employee)
            .OrderBy(s => s.Category.DisplayOrder)
            .ThenBy(s => s.DisplayOrder)
            .Select(s => new AdminServiceDto(
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                s.DisplayOrder,
                s.CategoryId,
                s.Category.Name,
                s.EmployeeId,
                s.Employee != null ? s.Employee.Name : null,
                s.IsActive
            ))
            .ToListAsync();

        return services;
    }

    public async Task<bool> BulkAssignServicesToEmployeeAsync(Guid employeeId, List<Guid> serviceIds)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new ArgumentException("Mitarbeiter nicht gefunden");

        // Get all services that need to be updated
        var services = await _context.Services
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync();

        // Remove employee assignment from services not in the list
        var servicesToUnassign = await _context.Services
            .Where(s => s.EmployeeId == employeeId && !serviceIds.Contains(s.Id))
            .ToListAsync();

        foreach (var service in servicesToUnassign)
        {
            service.EmployeeId = null;
            service.UpdatedAt = DateTime.UtcNow;
        }

        // Assign employee to selected services
        foreach (var service in services)
        {
            service.EmployeeId = employeeId;
            service.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk assigned {Count} services to employee {EmployeeId}",
            services.Count, employeeId);

        return true;
    }
}