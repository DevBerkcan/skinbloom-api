using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/employee-auth")]
public class EmployeeAuthController : ControllerBase
{
    private readonly SkinbloomDbContext _context;
    private readonly JwtService _jwt;
    private readonly IConfiguration _config;
    private readonly ILogger<EmployeeAuthController> _logger;

    public EmployeeAuthController(
        SkinbloomDbContext context,
        JwtService jwt,
        IConfiguration config,
        ILogger<EmployeeAuthController> logger)
    {
        _context = context;
        _jwt = jwt;
        _config = config;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Benutzername und Passwort sind erforderlich" });

        var employee = await _context.Employees
            .Where(e => e.IsActive && e.Username == request.Username.Trim().ToLower())
            .FirstOrDefaultAsync();

        bool passwordOk = employee != null
            && !string.IsNullOrEmpty(employee.PasswordHash)
            && BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash);

        if (!passwordOk || employee == null)
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            return Unauthorized(new { message = "Ungültiger Benutzername oder Passwort" });
        }

        var token = _jwt.GenerateToken(
            employee.Id, employee.Name, employee.Username!, employee.Role);

        _logger.LogInformation("Employee {Name} logged in successfully", employee.Name);

        return Ok(new
        {
            success = true,
            token = token,
            employee = new
            {
                employee.Id,
                employee.Name,
                employee.Username,
                employee.Role,
                employee.Specialty
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { success = true, message = "Erfolgreich abgemeldet" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var employeeId = JwtService.GetEmployeeId(User);
        if (employeeId == null)
            return Unauthorized(new { message = "Nicht angemeldet" });

        var employee = await _context.Employees
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new { e.Id, e.Name, e.Username, e.Role, e.Specialty })
            .FirstOrDefaultAsync();

        if (employee == null)
            return Unauthorized(new { message = "Mitarbeiter nicht gefunden oder deaktiviert" });

        return Ok(new { success = true, employee });
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Alle Felder sind erforderlich" });

        if (request.NewPassword.Length < 8)
            return BadRequest(new { message = "Neues Passwort muss mindestens 8 Zeichen haben" });

        var employeeId = JwtService.GetEmployeeId(User);
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound();

        if (string.IsNullOrEmpty(employee.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, employee.PasswordHash))
            return BadRequest(new { message = "Aktuelles Passwort ist falsch" });

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Passwort erfolgreich geändert" });
    }

    [HttpPost("set-password")]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Benutzername und Passwort erforderlich" });

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Username == request.Username.Trim().ToLower());

        if (employee == null)
            return NotFound(new { message = "Mitarbeiter nicht gefunden" });

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"Passwort für {employee.Name} gesetzt" });
    }
}