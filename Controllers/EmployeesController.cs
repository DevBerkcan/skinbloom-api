// Controllers/EmployeesController.cs
using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;

    public EmployeesController(EmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>GET /api/employees – list active employees (for booking form)</summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeListItemDto>>> GetAll([FromQuery] bool activeOnly = true)
    {
        var list = await _employeeService.GetAllAsync(activeOnly);
        return Ok(list);
    }

    /// <summary>GET /api/employees/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeListItemDto>> GetById(Guid id)
    {
        var emp = await _employeeService.GetByIdAsync(id);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    /// <summary>POST /api/employees – admin creates employee</summary>
    [HttpPost]
    public async Task<ActionResult<EmployeeListItemDto>> Create([FromBody] CreateEmployeeDto dto)
    {
        var emp = await _employeeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
    }

    /// <summary>PUT /api/employees/{id} – admin updates employee</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeListItemDto>> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        var emp = await _employeeService.UpdateAsync(id, dto);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    /// <summary>DELETE /api/employees/{id} – soft delete</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _employeeService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}