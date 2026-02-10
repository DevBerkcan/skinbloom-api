// BarberDario.Api.DTOs/BlockedTimeSlotDtos.cs
namespace BarberDario.Api.DTOs;

public record BlockedTimeSlotDto(
    Guid Id,
    DateOnly BlockDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Reason,
    DateTime CreatedAt
);

public record CreateBlockedTimeSlotDto(
    DateOnly BlockDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Reason
);

public record UpdateBlockedTimeSlotDto(
    DateOnly BlockDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Reason
);