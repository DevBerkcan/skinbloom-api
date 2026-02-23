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

    public async Task<IEnumerable<object>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.Employees.AsQueryable();
        if (activeOnly) query = query.Where(e => e.IsActive);

        return await query
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Role,
                e.Specialty,
                e.Location,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt,
                e.Username,
                HasPassword = !string.IsNullOrEmpty(e.PasswordHash),
            })
            .ToListAsync();
    }

    public async Task<object?> GetByIdAsync(Guid id)
    {
        var e = await _context.Employees.FindAsync(id);
        if (e == null) return null;

        return new
        {
            e.Id,
            e.Name,
            e.Role,
            e.Specialty,
            e.Location,
            e.IsActive,
            e.CreatedAt,
            e.UpdatedAt,
            e.Username,
            HasPassword = !string.IsNullOrEmpty(e.PasswordHash),
        };
    }

    public async Task<object?> GetStatsAsync(Guid id, DateOnly? from, DateOnly? to)
    {
        var exists = await _context.Employees.AnyAsync(e => e.Id == id);
        if (!exists) return null;

        var bookingsQ = _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.EmployeeId == id);

        if (from.HasValue) bookingsQ = bookingsQ.Where(b => b.BookingDate >= from.Value);
        if (to.HasValue) bookingsQ = bookingsQ.Where(b => b.BookingDate <= to.Value);

        var bookings = await bookingsQ.ToListAsync();
        var blockedCount = await _context.BlockedTimeSlots.CountAsync(b => b.EmployeeId == id);

        return new
        {
            EmployeeId = id,
            TotalBookings = bookings.Count,
            BlockedSlots = blockedCount,
        };
    }

    public async Task<(bool Success, object? Employee, string? ErrorMessage)> CreateAsync(CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, null, "Name ist erforderlich");

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim().ToLower();
            if (await _context.Employees.AnyAsync(e => e.Username == username))
                return (false, null, "Benutzername bereits vergeben");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Role = request.Role?.Trim() ?? "Mitarbeiterin",
            Specialty = request.Specialty?.Trim(),
            Location = request.Location?.Trim(),
            IsActive = true,
            Username = request.Username?.Trim().ToLower(),
            PasswordHash = !string.IsNullOrWhiteSpace(request.Password)
                               ? BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12)
                               : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created employee {Name}", employee.Name);

        var result = new
        {
            employee.Id,
            employee.Name,
            employee.Role,
            employee.Specialty,
            employee.Location,
            employee.IsActive,
            employee.Username,
            HasPassword = employee.PasswordHash != null,
        };

        return (true, result, null);
    }

    public async Task<(bool Success, object? Employee, string? ErrorMessage)> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return (false, null, "Mitarbeiter nicht gefunden");

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim().ToLower();
            if (await _context.Employees.AnyAsync(e => e.Username == username && e.Id != id))
                return (false, null, "Benutzername bereits vergeben");
            employee.Username = username;
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) employee.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Role)) employee.Role = request.Role.Trim();
        if (request.Specialty != null) employee.Specialty = request.Specialty?.Trim();
        if (request.Location != null) employee.Location = request.Location?.Trim();
        if (request.IsActive.HasValue) employee.IsActive = request.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        }

        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = new
        {
            employee.Id,
            employee.Name,
            employee.Role,
            employee.Specialty,
            employee.Location,
            employee.IsActive,
            employee.Username,
            HasPassword = employee.PasswordHash != null,
        };

        return (true, result, null);
    }

    public async Task<(bool Success, object? Result, string? ErrorMessage)> ToggleActiveAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return (false, null, "Mitarbeiter nicht gefunden");

        employee.IsActive = !employee.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, new { employee.Id, employee.IsActive }, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(Guid id)
    {
        if (await _context.Bookings.AnyAsync(b => b.EmployeeId == id))
            return (false, "Mitarbeiter hat Buchungen und kann nicht gelöscht werden. Bitte deaktivieren.");

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return (false, "Mitarbeiter nicht gefunden");

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        return (true, null);
    }
}