// BarberDario.Api.Controllers/EmployeesController.cs
using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly SkinbloomDbContext _context;
    private readonly IConfiguration _config;

    public EmployeesController(SkinbloomDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // ── GET /api/employees ────────────────────────────────────────
    // AllowAnonymous: the public booking widget calls this to list stylists.
    // activeOnly=false is only useful for the admin — but since we can't gate
    // the query param, the worst case is a public caller sees inactive names.
    // If that's a concern, remove activeOnly support and always return active.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = _context.Employees.AsQueryable();
        if (activeOnly) query = query.Where(e => e.IsActive);

        var employees = await query
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Role,
                e.Specialty,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt,
                e.Username,
                HasPassword = !string.IsNullOrEmpty(e.PasswordHash),
            })
            .ToListAsync();

        return Ok(employees);
    }

    // ── GET /api/employees/{id} ───────────────────────────────────
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var e = await _context.Employees.FindAsync(id);
        if (e == null) return NotFound();

        return Ok(new
        {
            e.Id,
            e.Name,
            e.Role,
            e.Specialty,
            e.IsActive,
            e.CreatedAt,
            e.UpdatedAt,
            e.Username,
            HasPassword = !string.IsNullOrEmpty(e.PasswordHash),
        });
    }

    // ── GET /api/employees/{id}/stats ─────────────────────────────
    [HttpGet("{id:guid}/stats")]
    [Authorize]
    public async Task<IActionResult> GetStats(Guid id,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var exists = await _context.Employees.AnyAsync(e => e.Id == id);
        if (!exists) return NotFound();

        var bookingsQ = _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.EmployeeId == id);

        if (from.HasValue) bookingsQ = bookingsQ.Where(b => b.BookingDate >= from.Value);
        if (to.HasValue) bookingsQ = bookingsQ.Where(b => b.BookingDate <= to.Value);

        var bookings = await bookingsQ.ToListAsync();
        var blockedCount = await _context.BlockedTimeSlots.CountAsync(b => b.EmployeeId == id);

        return Ok(new
        {
            EmployeeId = id,
            TotalBookings = bookings.Count,
            BlockedSlots = blockedCount,
        });
    }

    // ── POST /api/employees ───────────────────────────────────────
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name ist erforderlich" });

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim().ToLower();
            if (await _context.Employees.AnyAsync(e => e.Username == username))
                return Conflict(new { message = "Benutzername bereits vergeben" });
        }

        if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 8)
            return BadRequest(new { message = "Passwort muss mindestens 8 Zeichen haben" });

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Role = request.Role?.Trim() ?? "Mitarbeiterin",
            Specialty = request.Specialty?.Trim(),
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

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new
        {
            employee.Id,
            employee.Name,
            employee.Role,
            employee.Specialty,
            employee.IsActive,
            employee.Username,
            HasPassword = employee.PasswordHash != null,
        });
    }

    // ── PUT /api/employees/{id} ───────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim().ToLower();
            if (await _context.Employees.AnyAsync(e => e.Username == username && e.Id != id))
                return Conflict(new { message = "Benutzername bereits vergeben" });
            employee.Username = username;
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) employee.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Role)) employee.Role = request.Role.Trim();
        if (request.Specialty != null) employee.Specialty = request.Specialty.Trim();
        if (request.IsActive.HasValue) employee.IsActive = request.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var adminSecret = _config["AdminBootstrapSecret"] ?? "skinbloom-admin-bootstrap-2026";
            if (!Request.Headers.TryGetValue("X-Admin-Secret", out var provided) || provided != adminSecret)
                return Forbid();

            if (request.NewPassword.Length < 8)
                return BadRequest(new { message = "Passwort muss mindestens 8 Zeichen haben" });

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        }

        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            employee.Id,
            employee.Name,
            employee.Role,
            employee.Specialty,
            employee.IsActive,
            employee.Username,
            HasPassword = employee.PasswordHash != null,
        });
    }

    // ── PATCH /api/employees/{id}/toggle-active ───────────────────
    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound();

        employee.IsActive = !employee.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { employee.Id, employee.IsActive });
    }

    // ── DELETE /api/employees/{id} ────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (await _context.Bookings.AnyAsync(b => b.EmployeeId == id))
            return Conflict(new
            {
                message = "Mitarbeiter hat Buchungen und kann nicht gelöscht werden. Bitte deaktivieren."
            });

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound();

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}