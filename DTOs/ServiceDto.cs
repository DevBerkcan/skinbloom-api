// DTOs/ServiceDto.cs
namespace BarberDario.Api.DTOs;

// Original DTOs - DO NOT DELETE
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

// NEW ADMIN DTOs - Add these below existing ones
public record AdminServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    string CategoryName,
    Guid? EmployeeId,
    string? EmployeeName,
    bool IsActive
);

public record AdminServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    List<AdminServiceDto> Services
);

public record CreateServiceDto(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    Guid? EmployeeId
);

public record UpdateServiceDto(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    Guid? EmployeeId,
    bool IsActive
);

public record CreateCategoryDto(
    string Name,
    string? Description,
    int DisplayOrder
);

public record UpdateCategoryDto(
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive
);

public record EmployeeForAssignmentDto(
    Guid Id,
    string Name,
    string Role,
    string? Specialty,
    int ServiceCount
);