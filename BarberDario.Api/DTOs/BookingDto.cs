namespace BarberDario.Api.DTOs;

public record CreateBookingDto(
    Guid ServiceId,
    string BookingDate,  // YYYY-MM-DD
    string StartTime,    // HH:mm
    CustomerInfoDto Customer,
    string? CustomerNotes,
    // Tracking
    string? ReferrerUrl = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmContent = null,
    string? UtmTerm = null
);

public record CustomerInfoDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone
);

public record BookingResponseDto(
    Guid Id,
    string BookingNumber,
    string Status,
    bool ConfirmationSent,
    BookingDetailsDto Booking,
    CustomerDto Customer
);

public record BookingDetailsDto(
    Guid ServiceId,
    string ServiceName,
    string BookingDate,
    string StartTime,
    string EndTime,
    decimal Price
);

public record CustomerDto(
    string FirstName,
    string LastName,
    string Email
);

public record CancelBookingDto(
    string? Reason,
    bool NotifyCustomer = true
);

public record CancelBookingResponseDto(
    bool Success,
    string Message,
    bool RefundIssued = false
);

public record UpdateBookingStatusDto(
    Data.Entities.BookingStatus Status,
    string? AdminNotes = null
);
