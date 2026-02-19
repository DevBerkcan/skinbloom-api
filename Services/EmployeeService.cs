// Services/EmployeeService.cs
using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class EmployeeService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(SkinbloomDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EmployeeListItemDto>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.Employees.AsQueryable();
        if (activeOnly) query = query.Where(e => e.IsActive);

        return await query
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeListItemDto(e.Id, e.Name, e.Role, e.Specialty, e.IsActive))
            .ToListAsync();
    }

    public async Task<EmployeeListItemDto?> GetByIdAsync(Guid id)
    {
        var e = await _context.Employees.FindAsync(id);
        if (e == null) return null;
        return new EmployeeListItemDto(e.Id, e.Name, e.Role, e.Specialty, e.IsActive);
    }

    public async Task<EmployeeListItemDto> CreateAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            Name = dto.Name,
            Role = dto.Role,
            Specialty = dto.Specialty
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created employee {Name}", employee.Name);
        return new EmployeeListItemDto(employee.Id, employee.Name, employee.Role, employee.Specialty, employee.IsActive);
    }

    public async Task<EmployeeListItemDto?> UpdateAsync(Guid id, UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return null;

        employee.Name = dto.Name;
        employee.Role = dto.Role;
        employee.Specialty = dto.Specialty;
        employee.IsActive = dto.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return new EmployeeListItemDto(employee.Id, employee.Name, employee.Role, employee.Specialty, employee.IsActive);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return false;

        // Soft-delete: just deactivate so existing bookings keep the reference
        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}