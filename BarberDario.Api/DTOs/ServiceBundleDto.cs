namespace BarberDario.Api.DTOs;

public record ServiceBundleDto(
    Guid Id,
    string Name,
    string? Description,
    decimal OriginalPrice,
    decimal BundlePrice,
    decimal DiscountPercentage,
    decimal Savings, // OriginalPrice - BundlePrice
    int TotalDurationMinutes,
    int DisplayOrder,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool IsCurrentlyValid, // Check if bundle is valid right now
    List<BundleItemDto> Items
);

public record BundleItemDto(
    Guid ServiceId,
    string ServiceName,
    string? ServiceDescription,
    int ServiceDurationMinutes,
    decimal ServicePrice,
    int Quantity,
    int DisplayOrder,
    string? Notes
);

public record CreateServiceBundleDto(
    string Name,
    string? Description,
    decimal BundlePrice, // OriginalPrice will be calculated from services
    int DisplayOrder,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    string? TermsAndConditions,
    List<CreateBundleItemDto> Items
);

public record CreateBundleItemDto(
    Guid ServiceId,
    int Quantity,
    int DisplayOrder,
    string? Notes
);

public record UpdateServiceBundleDto(
    string? Name,
    string? Description,
    decimal? BundlePrice,
    int? DisplayOrder,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    string? TermsAndConditions,
    bool? IsActive,
    List<CreateBundleItemDto>? Items // If provided, replaces all items
);
