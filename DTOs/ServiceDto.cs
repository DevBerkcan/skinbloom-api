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

// Optional: Detailed ServiceDto with Category info
public record ServiceWithCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    int DisplayOrder,
    Guid CategoryId,
    string CategoryName
);
