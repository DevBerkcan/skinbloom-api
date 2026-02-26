namespace BarberDario.Api.DTOs;

// Original DTOs - Keep these for public API
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
    List<EmployeeBasicDto>? AssignedEmployees = null  // Changed from single to list
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

// NEW ADMIN DTOs
public record AdminServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    string CategoryName,
    List<EmployeeBasicDto> AssignedEmployees,  // Changed from single to list
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
    List<Guid>? EmployeeIds = null  // Changed from single to list
);

public record UpdateServiceDto(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    List<Guid>? EmployeeIds,  // Changed from single to list
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

public record BulkAssignDto(
    Guid EmployeeId,
    List<Guid> ServiceIds
);