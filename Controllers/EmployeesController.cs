using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly IConfiguration _config;

    public EmployeesController(EmployeeService employeeService, IConfiguration config)
    {
        _employeeService = employeeService;
        _config = config;
    }

    // ── Helper to get current employee ────────────────────────────
    private Guid? GetCurrentEmployeeId() => JwtService.GetEmployeeId(User);

    // ── GET /api/employees ────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var employees = await _employeeService.GetAllAsync(activeOnly);
        return Ok(employees);
    }

    // ── GET /api/employees/{id} ───────────────────────────────────
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    // ── GET /api/employees/{id}/stats ─────────────────────────────
    [HttpGet("{id:guid}/stats")]
    public async Task<IActionResult> GetStats(Guid id,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var currentUserId = GetCurrentEmployeeId();
        var stats = await _employeeService.GetStatsAsync(id, from, to);

        if (stats == null) return NotFound();
        return Ok(stats);
    }

    // ── POST /api/employees ───────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        var (success, employee, errorMessage) = await _employeeService.CreateAsync(request);

        if (!success)
            return BadRequest(new { message = errorMessage });

        return CreatedAtAction(nameof(GetById), new { id = ((dynamic)employee).Id }, employee);
    }

    // ── PUT /api/employees/{id} ───────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var (success, employee, errorMessage) = await _employeeService.UpdateAsync(id, request);

        if (!success)
        {
            if (errorMessage == "Mitarbeiter nicht gefunden")
                return NotFound();
            return Conflict(new { message = errorMessage });
        }

        return Ok(employee);
    }

    // ── PATCH /api/employees/{id}/toggle-active ───────────────────
    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var (success, result, errorMessage) = await _employeeService.ToggleActiveAsync(id);

        if (!success)
            return NotFound(new { message = errorMessage });

        return Ok(result);
    }

    // ── DELETE /api/employees/{id} ────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, errorMessage) = await _employeeService.DeleteAsync(id);

        if (!success)
        {
            if (errorMessage == "Mitarbeiter nicht gefunden")
                return NotFound();
            return Conflict(new { message = errorMessage });
        }

        return NoContent();
    }
}