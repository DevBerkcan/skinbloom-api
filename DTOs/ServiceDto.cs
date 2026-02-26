namespace BarberDario.Api.DTOs;

public record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder
);

public record ServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    List<ServiceDto> Services
);

public record ServiceWithCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    string CategoryName,
    EmployeeBasicDto? AssignedEmployee = null
);

public record EmployeeBasicDto(
    Guid Id,
    string Name,
    string Role,
    string? Specialty
);

// For assigning service to employee
public record AssignServiceToEmployeeDto(
    Guid ServiceId,
    Guid EmployeeId
);