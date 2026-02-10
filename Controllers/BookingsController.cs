using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using BarberDario.Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;
    private readonly EmailService _emailService;

    public BookingsController(BookingService bookingService, ILogger<BookingsController> logger, EmailService emailService)
    {
        _bookingService = bookingService;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromBody] CreateBookingDto dto)
    {
        // Validierung
        var validator = new CreateBookingValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return BadRequest(new { errors });
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(dto);
            _logger.LogInformation("Booking created successfully: {BookingId}", booking.Id);

            return CreatedAtAction(
                nameof(GetBooking),
                new { id = booking.Id },
                booking
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Booking conflict or validation error");
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get booking by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
        {
            return NotFound(new { message = "Buchung nicht gefunden" });
        }

        return Ok(booking);
    }

    /// <summary>
    /// Get bookings by customer email
    /// </summary>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingResponseDto>>> GetBookingsByEmail(string email)
    {
        var bookings = await _bookingService.GetBookingsByEmailAsync(email);
        return Ok(bookings);
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CancelBookingResponseDto>> CancelBooking(
        Guid id,
        [FromBody] CancelBookingDto? dto = null)
    {
        dto ??= new CancelBookingDto(null, true);

        try
        {
            var result = await _bookingService.CancelBookingAsync(id, dto);
            _logger.LogInformation("Booking cancelled: {BookingId}", id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Booking not found: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot cancel booking: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("confirm/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmBooking(string token)
    {
        try
        {
            var (bookingId, action) = _emailService.DecodeToken(token);

            if (bookingId == Guid.Empty || action != "confirm")
            {
                return Content(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Fehler - Skinbloom</title>
                    <style>
                        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
                        .error { color: #dc3545; }
                        .success { color: #28a745; }
                    </style>
                </head>
                <body>
                    <h1 class='error'>❌ Ungültiger Bestätigungslink</h1>
                    <p>Der Bestätigungslink ist ungültig oder abgelaufen.</p>
                    <p><a href='https://skinbloom.de'>Zurück zur Website</a></p>
                </body>
                </html>", "text/html");
            }

            // Buchung bestätigen
            var booking = await _bookingService.ConfirmBookingAsync(bookingId);

            // HTML-Erfolgsseite zurückgeben - ANGEPASST FÜR DEINE DTO-STRUKTUR
            return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Buchung bestätigt - Skinbloom</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                    .success {{ color: #28a745; }}
                    .details {{ 
                        margin: 30px auto; 
                        max-width: 500px; 
                        text-align: left; 
                        background: #f8f9fa; 
                        padding: 20px; 
                        border-radius: 10px;
                        border-left: 4px solid #28a745;
                    }}
                    .info-box {{ 
                        margin: 20px auto;
                        max-width: 600px;
                        background: #e7f3ff;
                        padding: 15px;
                        border-radius: 5px;
                        text-align: left;
                    }}
                </style>
            </head>
            <body>
                <h1 class='success'> Buchung erfolgreich bestätigt!</h1>
                <p>Vielen Dank für die Bestätigung Ihrer Buchung.</p>
                
                <div class='details'>
                    <h3>Ihre Buchungsdetails:</h3>
                    <p><strong>Buchungsnummer:</strong> {booking.BookingNumber}</p>
                    <p><strong>Service:</strong> {booking.Booking.ServiceName}</p>
                    <p><strong>Datum:</strong> {booking.Booking.BookingDate}</p>
                    <p><strong>Uhrzeit:</strong> {booking.Booking.StartTime} - {booking.Booking.EndTime}</p>
                    <p><strong>Preis:</strong> {booking.Booking.Price:C}</p>
                    <p><strong>Status:</strong> <span style='color: #28a745; font-weight: bold;'>{booking.Status}</span></p>
                </div>
                
                <div class='info-box'>
                    <h4> Wichtige Informationen:</h4>
                    <ul>
                        <li>Sie erhalten in Kürze eine Bestätigungs-Email mit allen Details</li>
                        <li>Bitte erscheinen Sie 10 Minuten vor Ihrem Termin</li>
                        <li>Bringen Sie einen Lichtbildausweis mit</li>
                        <li>Bei Verhinderung bitten wir um mindestens 24-stündige Absage</li>
                    </ul>
                </div>
                
                <p><a href='https://skinbloom.de' style='color: #007bff; text-decoration: none;'>
                     Zurück zur Website
                </a></p>
            </body>
            </html>", "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking with token: {Token}", token);

            return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Fehler - Skinbloom</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                    .error {{ color: #dc3545; }}
                </style>
            </head>
            <body>
                <h1 class='error'> Fehler bei der Bestätigung</h1>
                <p><strong>Fehler:</strong> {ex.Message}</p>
                <p>Bitte kontaktieren Sie uns telefonisch oder per Email.</p>
                <p><a href='https://www.skinbloom-aesthetics.ch/'>Zurück zur Website</a></p>
            </body>
            </html>", "text/html");
        }
    }

    [HttpGet("cancel/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelBookingByToken(string token)
    {
        try
        {
            var (bookingId, action) = _emailService.DecodeToken(token);

            if (bookingId == Guid.Empty || action != "cancel")
            {
                return Content(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Fehler - Skinbloom</title>
                    <style>
                        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
                        .error { color: #dc3545; }
                        .success { color: #28a745; }
                    </style>
                </head>
                <body>
                    <h1 class='error'>❌ Ungültiger Stornierungslink</h1>
                    <p>Der Stornierungslink ist ungültig oder abgelaufen.</p>
                    <p><a href='https://skinbloom.de'>Zurück zur Website</a></p>
                </body>
                </html>", "text/html");
            }

            var booking = await _bookingService.GetBookingByIdAsync(bookingId);
            if (booking == null)
            {
                return Content(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Fehler - Skinbloom</title>
                    <style>
                        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
                        .error { color: #dc3545; }
                    </style>
                </head>
                <body>
                    <h1 class='error'>❌ Buchung nicht gefunden</h1>
                    <p>Die Buchung konnte nicht gefunden werden.</p>
                    <p><a href='https://skinbloom.de'>Zurück zur Website</a></p>
                </body>
                </html>", "text/html");
            }

            if (booking.Status == "Cancelled")
            {
                return Content($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Bereits storniert - Skinbloom</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                        .info {{ color: #17a2b8; }}
                        .details {{ 
                            margin: 30px auto; 
                            max-width: 500px; 
                            text-align: left; 
                            background: #f8f9fa; 
                            padding: 20px; 
                            border-radius: 10px;
                            border-left: 4px solid #17a2b8;
                        }}
                    </style>
                </head>
                <body>
                    <h1 class='info'> Buchung bereits storniert</h1>
                    <p>Diese Buchung wurde bereits storniert.</p>
                    
                    <div class='details'>
                        <h3>Buchungsdetails:</h3>
                        <p><strong>Buchungsnummer:</strong> {booking.BookingNumber}</p>
                        <p><strong>Service:</strong> {booking.Booking.ServiceName}</p>
                        <p><strong>Datum:</strong> {booking.Booking.BookingDate}</p>
                        <p><strong>Uhrzeit:</strong> {booking.Booking.StartTime} - {booking.Booking.EndTime}</p>
                        <p><strong>Preis:</strong> {booking.Booking.Price:C}</p>
                        <p><strong>Status:</strong> <span style='color: #dc3545; font-weight: bold;'>Storniert</span></p>
                    </div>
                    
                    <p><a href='https://skinbloom.de' style='color: #007bff; text-decoration: none;'>
                         Zurück zur Website
                    </a></p>
                </body>
                </html>", "text/html");
            }

            // Prüfe ob Buchung bereits abgeschlossen ist
            if (booking.Status == "Completed")
            {
                return Content($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Buchung abgeschlossen - Skinbloom</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                        .warning {{ color: #ffc107; }}
                        .details {{ 
                            margin: 30px auto; 
                            max-width: 500px; 
                            text-align: left; 
                            background: #f8f9fa; 
                            padding: 20px; 
                            border-radius: 10px;
                            border-left: 4px solid #ffc107;
                        }}
                    </style>
                </head>
                <body>
                    <h1 class='warning'> Buchung bereits abgeschlossen</h1>
                    <p>Diese Buchung wurde bereits abgeschlossen und kann nicht mehr storniert werden.</p>
                    
                    <div class='details'>
                        <h3>Buchungsdetails:</h3>
                        <p><strong>Buchungsnummer:</strong> {booking.BookingNumber}</p>
                        <p><strong>Service:</strong> {booking.Booking.ServiceName}</p>
                        <p><strong>Datum:</strong> {booking.Booking.BookingDate}</p>
                        <p><strong>Uhrzeit:</strong> {booking.Booking.StartTime} - {booking.Booking.EndTime}</p>
                        <p><strong>Preis:</strong> {booking.Booking.Price:C}</p>
                        <p><strong>Status:</strong> <span style='color: #28a745; font-weight: bold;'>Abgeschlossen</span></p>
                    </div>
                    
                    <p>Bei Fragen kontaktieren Sie uns bitte direkt.</p>
                    <p><a href='https://skinbloom.de' style='color: #007bff; text-decoration: none;'>
                         Zurück zur Website
                    </a></p>
                </body>
                </html>", "text/html");
            }

            // Buchung stornieren
            var dto = new CancelBookingDto("Storniert vom Kunden per E-Mail-Link", true);
            var result = await _bookingService.CancelBookingAsync(bookingId, dto);

            if (!result.Success)
            {
                return Content($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Fehler - Skinbloom</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                        .error {{ color: #dc3545; }}
                    </style>
                </head>
                <body>
                    <h1 class='error'> Stornierung fehlgeschlagen</h1>
                    <p><strong>Fehler:</strong> {result.Message}</p>
                    <p>Bitte kontaktieren Sie uns telefonisch oder per Email.</p>
                    <p><a href='https://www.skinbloom-aesthetics.ch/'>Zurück zur Website</a></p>
                </body>
                </html>", "text/html");
            }

            return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Buchung storniert - Skinbloom</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                    .success {{ color: #28a745; }}
                    .details {{ 
                        margin: 30px auto; 
                        max-width: 500px; 
                        text-align: left; 
                        background: #f8f9fa; 
                        padding: 20px; 
                        border-radius: 10px;
                        border-left: 4px solid #28a745;
                    }}
                    .info-box {{ 
                        margin: 20px auto;
                        max-width: 600px;
                        background: #fff3cd;
                        padding: 15px;
                        border-radius: 5px;
                        text-align: left;
                        border-left: 4px solid #ffc107;
                    }}
                    .refund-box {{ 
                        margin: 20px auto;
                        max-width: 600px;
                        background: #d4edda;
                        padding: 15px;
                        border-radius: 5px;
                        text-align: left;
                        border-left: 4px solid #28a745;
                    }}
                </style>
            </head>
            <body>
                <h1 class='success'> Buchung erfolgreich storniert!</h1>
                <p>{result.Message}</p>
                
                <div class='details'>
                    <h3>Stornierte Buchung:</h3>
                    <p><strong>Buchungsnummer:</strong> {booking.BookingNumber}</p>
                    <p><strong>Service:</strong> {booking.Booking.ServiceName}</p>
                    <p><strong>Datum:</strong> {booking.Booking.BookingDate}</p>
                    <p><strong>Uhrzeit:</strong> {booking.Booking.StartTime} - {booking.Booking.EndTime}</p>
                    <p><strong>Preis:</strong> {booking.Booking.Price:C}</p>
                    <p><strong>Status:</strong> <span style='color: #dc3545; font-weight: bold;'>Storniert</span></p>
                    <p><strong>Grund:</strong> Storniert vom Kunden per E-Mail-Link</p>
                </div>
                
                {(result.RefundIssued ?
                    $@"<div class='refund-box'>
                    <h4> Rückerstattung</h4>
                    <p>Eine Rückerstattung in Höhe von <strong>{booking.Booking.Price:C}</strong> wurde veranlasst.</p>
                    <p>Die Rückerstattung kann 5-10 Werktage dauern.</p>
                </div>" :
                    @"<div class='info-box'>
                    <h4> Wichtige Informationen:</h4>
                    <ul>
                        <li>Die Stornierung wurde in unserem System erfasst</li>
                        <li>Bei Fragen kontaktieren Sie uns bitte</li>
                        <li>Bitte beachten Sie unsere Stornierungsbedingungen</li>
                    </ul>
                </div>")}
                
                <div style='margin: 30px;'>
                    <a href='https://www.skinbloom-aesthetics.ch/' style='
                        background-color: #007bff;
                        color: white;
                        padding: 12px 24px;
                        text-decoration: none;
                        border-radius: 5px;
                        display: inline-block;
                        margin: 10px;
                    '>
                         Neuen Termin buchen
                    </a>
                    
                    <a href='https://www.skinbloom-aesthetics.ch/' style='
                        background-color: #6c757d;
                        color: white;
                        padding: 12px 24px;
                        text-decoration: none;
                        border-radius: 5px;
                        display: inline-block;
                        margin: 10px;
                    '>
                        Zurück zur Website
                    </a>
                </div>
                
                <p style='font-size: 12px; color: #666; margin-top: 30px;'>
                    * Sie erhalten eine Bestätigungs-Email an: {booking.Customer.Email}
                </p>
            </body>
            </html>", "text/html");
        }
        catch (InvalidOperationException ex)
        {
            return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Fehler - Skinbloom</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                    .error {{ color: #dc3545; }}
                </style>
            </head>
            <body>
                <h1 class='error'>❌ Fehler bei der Stornierung</h1>
                <p><strong>Fehler:</strong> {ex.Message}</p>
                <p>Bitte kontaktieren Sie uns telefonisch oder per Email.</p>
                <p><a href='https://www.skinbloom-aesthetics.ch/'>Zurück zur Website</a></p>
            </body>
            </html>", "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking with token: {Token}", token);

            return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Fehler - Skinbloom</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                    .error {{ color: #dc3545; }}
                </style>
            </head>
            <body>
                <h1 class='error'>❌ Unerwarteter Fehler</h1>
                <p>Ein unerwarteter Fehler ist aufgetreten. Bitte versuchen Sie es später erneut oder kontaktieren Sie uns.</p>
                <p><a href='https://www.skinbloom-aesthetics.ch/'>Zurück zur Website</a></p>
            </body>
            </html>", "text/html");
        }
    }
}
