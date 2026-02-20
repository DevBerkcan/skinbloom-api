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

public record CreateEmployeeRequest(
    string Name,
    string? Role,
    string? Specialty,
    string? Username,
    string? Password      
);

public record UpdateEmployeeRequest(
    string? Name,
    string? Role,
    string? Specialty,
    string? Username,
    bool? IsActive,
    string? NewPassword  
);

public record LoginRequest(string Username, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record SetPasswordRequest(string Username, string Password);