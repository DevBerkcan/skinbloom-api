// DTOs/EmployeeDto.cs
namespace BarberDario.Api.DTOs;

public record EmployeeListItemDto(
    Guid Id,
    string Name,
    string Role,
    string? Specialty,
    bool IsActive
);

public record CreateEmployeeDto(
    string Name,
    string Role,
    string? Specialty
);

public record UpdateEmployeeDto(
    string Name,
    string Role,
    string? Specialty,
    bool IsActive
);