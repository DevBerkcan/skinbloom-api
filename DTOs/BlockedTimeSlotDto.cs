// BarberDario.Api/DTOs/BlockedTimeSlotDtos.cs

namespace BarberDario.Api.DTOs;

public record BlockedTimeSlotDto(
    Guid Id,
    DateOnly BlockDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Reason,
    DateTime CreatedAt
);

// For single day blocking
public record CreateBlockedTimeSlotDto(
    string BlockDate,  // Format: "YYYY-MM-DD"
    string StartTime,  // Format: "HH:mm"
    string EndTime,    // Format: "HH:mm"
    string? Reason
);

// NEW: For date range blocking
public record CreateBlockedDateRangeDto(
    string FromDate,   // Format: "YYYY-MM-DD"
    string ToDate,     // Format: "YYYY-MM-DD"
    string StartTime,  // Format: "HH:mm"
    string EndTime,    // Format: "HH:mm"
    string? Reason
);

public record UpdateBlockedTimeSlotDto(
    string BlockDate,
    string StartTime,
    string EndTime,
    string? Reason
);