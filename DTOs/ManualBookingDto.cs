// DTOs/ManualBookingDto.cs
namespace BarberDario.Api.DTOs;

public record CreateManualBookingDto(
    Guid ServiceId,
    string BookingDate,
    string StartTime,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? CustomerNotes
);

public record ManualBookingResponseDto(
    Guid Id,
    string BookingNumber,
    string Status,
    bool ConfirmationSent,
    BookingDetailsDto Booking,
    CustomerBasicDto Customer
);

public record CustomerBasicDto(
    string FirstName,
    string LastName,
    string Email
);