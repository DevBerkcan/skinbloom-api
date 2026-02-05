namespace BarberDario.Api.DTOs;

public record ServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    string? Color,
    int DisplayOrder,
    int ServiceCount
);

public record CreateServiceCategoryDto(
    string Name,
    string? Description,
    string? Icon,
    string? Color,
    int DisplayOrder
);

public record UpdateServiceCategoryDto(
    string? Name,
    string? Description,
    string? Icon,
    string? Color,
    int? DisplayOrder,
    bool? IsActive
);
