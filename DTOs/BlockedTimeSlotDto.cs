// BarberDario.Api/DTOs/BlockedTimeSlotDtos.cs

namespace BarberDario.Api.DTOs;

// DTOs that match the complex object format your API expects
public record DateOnlyDto(
    int Year,
    int Month,
    int Day,
    int DayOfWeek
);

public record TimeOnlyDto(
    int Hour,
    int Minute
);

public record CreateBlockedTimeSlotDto(
    DateOnlyDto BlockDate,
    TimeOnlyDto StartTime,
    TimeOnlyDto EndTime,
    string? Reason
);

public record UpdateBlockedTimeSlotDto(
    DateOnlyDto BlockDate,
    TimeOnlyDto StartTime,
    TimeOnlyDto EndTime,
    string? Reason
);

public record BlockedTimeSlotDto(
    Guid Id,
    DateOnly BlockDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Reason,
    DateTime CreatedAt
);