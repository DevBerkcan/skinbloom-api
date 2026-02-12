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
                <title>Fehler - Skinbloom Aesthetics</title>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body { 
                        font-family: 'Helvetica', 'Arial', sans-serif; 
                        text-align: center; 
                        padding: 0; 
                        margin: 0;
                        background-color: #F5EDEB;
                        color: #1E1E1E;
                        line-height: 1.6;
                    }
                    .container {
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #FFFFFF;
                        border-radius: 24px;
                        box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                        overflow: hidden;
                    }
                    .header {
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        padding: 40px 20px;
                    }
                    .content {
                        padding: 40px;
                    }
                    .error {
                        color: #D8B0AC;
                        font-size: 48px;
                        margin-bottom: 20px;
                    }
                    .title {
                        font-size: 24px;
                        font-weight: 700;
                        color: #1E1E1E;
                        margin-bottom: 20px;
                    }
                    .message {
                        color: #8A8A8A;
                        margin-bottom: 30px;
                    }
                    .button {
                        display: inline-block;
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        color: #FFFFFF;
                        text-decoration: none;
                        padding: 14px 32px;
                        border-radius: 40px;
                        font-weight: 600;
                        font-size: 16px;
                        transition: all 0.3s ease;
                        box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                    }
                    .button:hover {
                        transform: translateY(-2px);
                        box-shadow: 0 8px 24px rgba(232,199,195,0.4);
                    }
                    .footer {
                        background-color: #F5EDEB;
                        padding: 24px;
                        color: #8A8A8A;
                        font-size: 14px;
                    }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <div style='color: #FFFFFF; font-size: 48px; font-weight: 300; margin-bottom: 10px;'>✧</div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                    </div>
                    <div class='content'>
                        <div class='error'>✧</div>
                        <h2 class='title'>Ungültiger Stornierungslink</h2>
                        <p class='message'>Der Stornierungslink ist ungültig oder abgelaufen.</p>
                        <p style='margin-top: 30px;'>
                            <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                        </p>
                    </div>
                    <div class='footer'>
                        <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                    </div>
                </div>
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
                <title>Fehler - Skinbloom Aesthetics</title>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body { 
                        font-family: 'Helvetica', 'Arial', sans-serif; 
                        text-align: center; 
                        padding: 0; 
                        margin: 0;
                        background-color: #F5EDEB;
                        color: #1E1E1E;
                        line-height: 1.6;
                    }
                    .container {
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #FFFFFF;
                        border-radius: 24px;
                        box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                        overflow: hidden;
                    }
                    .header {
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        padding: 40px 20px;
                    }
                    .content {
                        padding: 40px;
                    }
                    .error {
                        color: #D8B0AC;
                        font-size: 48px;
                        margin-bottom: 20px;
                    }
                    .title {
                        font-size: 24px;
                        font-weight: 700;
                        color: #1E1E1E;
                        margin-bottom: 20px;
                    }
                    .message {
                        color: #8A8A8A;
                        margin-bottom: 30px;
                    }
                    .button {
                        display: inline-block;
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        color: #FFFFFF;
                        text-decoration: none;
                        padding: 14px 32px;
                        border-radius: 40px;
                        font-weight: 600;
                        font-size: 16px;
                        transition: all 0.3s ease;
                        box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                    }
                    .footer {
                        background-color: #F5EDEB;
                        padding: 24px;
                        color: #8A8A8A;
                        font-size: 14px;
                    }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                    </div>
                    <div class='content'>
                        <div class='error'>✧</div>
                        <h2 class='title'>Buchung nicht gefunden</h2>
                        <p class='message'>Die Buchung konnte nicht gefunden werden.</p>
                        <p style='margin-top: 30px;'>
                            <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                        </p>
                    </div>
                    <div class='footer'>
                        <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                    </div>
                </div>
            </body>
            </html>", "text/html");
            }

            if (booking.Status == "Cancelled")
            {
                return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Bereits storniert - Skinbloom Aesthetics</title>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ 
                        font-family: 'Helvetica', 'Arial', sans-serif; 
                        text-align: center; 
                        padding: 0; 
                        margin: 0;
                        background-color: #F5EDEB;
                        color: #1E1E1E;
                        line-height: 1.6;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #FFFFFF;
                        border-radius: 24px;
                        box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                        overflow: hidden;
                    }}
                    .header {{
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        padding: 40px 20px;
                    }}
                    .content {{
                        padding: 40px;
                    }}
                    .info-icon {{
                        color: #8A8A8A;
                        font-size: 48px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        font-size: 28px;
                        font-weight: 700;
                        color: #1E1E1E;
                        margin-bottom: 10px;
                    }}
                    .booking-number {{
                        background-color: #F5EDEB;
                        padding: 12px 24px;
                        border-radius: 40px;
                        display: inline-block;
                        color: #8A8A8A;
                        font-size: 14px;
                        font-weight: 600;
                        margin-bottom: 30px;
                    }}
                    .details-card {{
                        background-color: #F5EDEB;
                        border-radius: 16px;
                        padding: 30px;
                        text-align: left;
                        margin-bottom: 30px;
                    }}
                    .detail-row {{
                        display: flex;
                        padding: 12px 0;
                        border-bottom: 1px solid #E8C7C3;
                    }}
                    .detail-row:last-child {{
                        border-bottom: none;
                    }}
                    .detail-label {{
                        width: 120px;
                        color: #8A8A8A;
                        font-weight: 500;
                    }}
                    .detail-value {{
                        flex: 1;
                        color: #1E1E1E;
                        font-weight: 600;
                    }}
                    .status-cancelled {{
                        color: #D8B0AC;
                        font-weight: 700;
                    }}
                    .button {{
                        display: inline-block;
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        color: #FFFFFF;
                        text-decoration: none;
                        padding: 14px 32px;
                        border-radius: 40px;
                        font-weight: 600;
                        font-size: 16px;
                        transition: all 0.3s ease;
                        box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                    }}
                    .footer {{
                        background-color: #F5EDEB;
                        padding: 24px;
                        color: #8A8A8A;
                        font-size: 14px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <div style='color: #FFFFFF; font-size: 48px; font-weight: 300; margin-bottom: 10px;'>✧</div>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                    </div>
                    <div class='content'>
                        <div class='info-icon'>✧</div>
                        <h2 class='title'>Bereits storniert</h2>
                        <div class='booking-number'>
                            Buchungsnummer: {booking.BookingNumber}
                        </div>
                        
                        <div class='details-card'>
                            <div class='detail-row'>
                                <span class='detail-label'>Leistung</span>
                                <span class='detail-value'>{booking.Booking.ServiceName}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Datum</span>
                                <span class='detail-value'>{booking.Booking.BookingDate}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Uhrzeit</span>
                                <span class='detail-value'>{booking.Booking.StartTime} - {booking.Booking.EndTime}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Preis</span>
                                <span class='detail-value'>{booking.Booking.Price:0.00} CHF</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Status</span>
                                <span class='detail-value'><span class='status-cancelled'>Storniert</span></span>
                            </div>
                        </div>
                        
                        <p style='margin-top: 30px;'>
                            <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                        </p>
                    </div>
                    <div class='footer'>
                        <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                    </div>
                </div>
            </body>
            </html>", "text/html");
            }

            if (booking.Status == "Completed")
            {
                return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Buchung abgeschlossen - Skinbloom Aesthetics</title>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ 
                        font-family: 'Helvetica', 'Arial', sans-serif; 
                        text-align: center; 
                        padding: 0; 
                        margin: 0;
                        background-color: #F5EDEB;
                        color: #1E1E1E;
                        line-height: 1.6;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #FFFFFF;
                        border-radius: 24px;
                        box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                        overflow: hidden;
                    }}
                    .header {{
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        padding: 40px 20px;
                    }}
                    .content {{
                        padding: 40px;
                    }}
                    .info-icon {{
                        color: #C09995;
                        font-size: 48px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        font-size: 28px;
                        font-weight: 700;
                        color: #1E1E1E;
                        margin-bottom: 10px;
                    }}
                    .booking-number {{
                        background-color: #F5EDEB;
                        padding: 12px 24px;
                        border-radius: 40px;
                        display: inline-block;
                        color: #8A8A8A;
                        font-size: 14px;
                        font-weight: 600;
                        margin-bottom: 30px;
                    }}
                    .details-card {{
                        background-color: #F5EDEB;
                        border-radius: 16px;
                        padding: 30px;
                        text-align: left;
                        margin-bottom: 30px;
                    }}
                    .detail-row {{
                        display: flex;
                        padding: 12px 0;
                        border-bottom: 1px solid #E8C7C3;
                    }}
                    .detail-row:last-child {{
                        border-bottom: none;
                    }}
                    .detail-label {{
                        width: 120px;
                        color: #8A8A8A;
                        font-weight: 500;
                    }}
                    .detail-value {{
                        flex: 1;
                        color: #1E1E1E;
                        font-weight: 600;
                    }}
                    .status-completed {{
                        color: #C09995;
                        font-weight: 700;
                    }}
                    .button {{
                        display: inline-block;
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        color: #FFFFFF;
                        text-decoration: none;
                        padding: 14px 32px;
                        border-radius: 40px;
                        font-weight: 600;
                        font-size: 16px;
                        transition: all 0.3s ease;
                        box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                    }}
                    .footer {{
                        background-color: #F5EDEB;
                        padding: 24px;
                        color: #8A8A8A;
                        font-size: 14px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                    </div>
                    <div class='content'>
                        <div class='info-icon'>✓</div>
                        <h2 class='title'>Buchung abgeschlossen</h2>
                        <div class='booking-number'>
                            Buchungsnummer: {booking.BookingNumber}
                        </div>
                        
                        <div class='details-card'>
                            <div class='detail-row'>
                                <span class='detail-label'>Leistung</span>
                                <span class='detail-value'>{booking.Booking.ServiceName}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Datum</span>
                                <span class='detail-value'>{booking.Booking.BookingDate}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Uhrzeit</span>
                                <span class='detail-value'>{booking.Booking.StartTime} - {booking.Booking.EndTime}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Status</span>
                                <span class='detail-value'><span class='status-completed'>Abgeschlossen</span></span>
                            </div>
                        </div>
                        
                        <p style='margin-top: 30px;'>
                            <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                        </p>
                    </div>
                    <div class='footer'>
                        <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                    </div>
                </div>
            </body>
            </html>", "text/html");
            }

            var dto = new CancelBookingDto("Storniert vom Kunden per E-Mail-Link", true);
            var result = await _bookingService.CancelBookingAsync(bookingId, dto);

            if (!result.Success)
            {
                return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Fehler - Skinbloom Aesthetics</title>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ 
                        font-family: 'Helvetica', 'Arial', sans-serif; 
                        text-align: center; 
                        padding: 0; 
                        margin: 0;
                        background-color: #F5EDEB;
                        color: #1E1E1E;
                        line-height: 1.6;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #FFFFFF;
                        border-radius: 24px;
                        box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                        overflow: hidden;
                    }}
                    .header {{
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        padding: 40px 20px;
                    }}
                    .content {{
                        padding: 40px;
                    }}
                    .error {{
                        color: #D8B0AC;
                        font-size: 48px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        font-size: 24px;
                        font-weight: 700;
                        color: #1E1E1E;
                        margin-bottom: 20px;
                    }}
                    .message {{
                        color: #8A8A8A;
                        margin-bottom: 30px;
                    }}
                    .button {{
                        display: inline-block;
                        background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                        color: #FFFFFF;
                        text-decoration: none;
                        padding: 14px 32px;
                        border-radius: 40px;
                        font-weight: 600;
                        font-size: 16px;
                        transition: all 0.3s ease;
                        box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                    }}
                    .footer {{
                        background-color: #F5EDEB;
                        padding: 24px;
                        color: #8A8A8A;
                        font-size: 14px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                    </div>
                    <div class='content'>
                        <div class='error'>✧</div>
                        <h2 class='title'>Stornierung fehlgeschlagen</h2>
                        <p class='message'>{result.Message}</p>
                        <p style='margin-top: 30px;'>
                            <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                        </p>
                    </div>
                    <div class='footer'>
                        <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                    </div>
                </div>
            </body>
            </html>", "text/html");
            }

            return Content($@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Buchung storniert - Skinbloom Aesthetics</title>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ 
                    font-family: 'Helvetica', 'Arial', sans-serif; 
                    text-align: center; 
                    padding: 0; 
                    margin: 0;
                    background-color: #F5EDEB;
                    color: #1E1E1E;
                    line-height: 1.6;
                }}
                .container {{
                    max-width: 600px;
                    margin: 40px auto;
                    background-color: #FFFFFF;
                    border-radius: 24px;
                    box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                    overflow: hidden;
                }}
                .header {{
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    padding: 40px 20px;
                }}
                .content {{
                    padding: 40px;
                }}
                .success-icon {{
                    color: #E8C7C3;
                    font-size: 64px;
                    margin-bottom: 20px;
                }}
                .title {{
                    font-size: 28px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 10px;
                }}
                .booking-number {{
                    background-color: #F5EDEB;
                    padding: 12px 24px;
                    border-radius: 40px;
                    display: inline-block;
                    color: #8A8A8A;
                    font-size: 14px;
                    font-weight: 600;
                    margin-bottom: 30px;
                }}
                .details-card {{
                    background-color: #F5EDEB;
                    border-radius: 16px;
                    padding: 30px;
                    text-align: left;
                    margin-bottom: 30px;
                }}
                .detail-row {{
                    display: flex;
                    padding: 12px 0;
                    border-bottom: 1px solid #E8C7C3;
                }}
                .detail-row:last-child {{
                    border-bottom: none;
                }}
                .detail-label {{
                    width: 120px;
                    color: #8A8A8A;
                    font-weight: 500;
                }}
                .detail-value {{
                    flex: 1;
                    color: #1E1E1E;
                    font-weight: 600;
                }}
                .status-cancelled {{
                    color: #D8B0AC;
                    font-weight: 700;
                }}
                .info-box {{
                    background-color: #FFFFFF;
                    border: 2px solid #E8C7C3;
                    border-radius: 16px;
                    padding: 24px;
                    text-align: left;
                    margin-bottom: 30px;
                }}
                .refund-box {{
                    background-color: #F5EDEB;
                    border: 2px solid #C09995;
                    border-radius: 16px;
                    padding: 24px;
                    text-align: left;
                    margin-bottom: 30px;
                }}
                .info-title {{
                    color: #1E1E1E;
                    font-weight: 700;
                    margin-bottom: 16px;
                    font-size: 18px;
                }}
                .info-list {{
                    list-style: none;
                    padding: 0;
                    margin: 0;
                }}
                .info-list li {{
                    padding: 8px 0;
                    padding-left: 24px;
                    position: relative;
                    color: #8A8A8A;
                }}
                .info-list li:before {{
                    content: '✧';
                    color: #E8C7C3;
                    position: absolute;
                    left: 0;
                    top: 8px;
                    font-size: 14px;
                }}
                .button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    color: #FFFFFF;
                    text-decoration: none;
                    padding: 14px 32px;
                    border-radius: 40px;
                    font-weight: 600;
                    font-size: 16px;
                    transition: all 0.3s ease;
                    box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                    margin: 10px;
                }}
                .button-secondary {{
                    display: inline-block;
                    background-color: #F5EDEB;
                    color: #1E1E1E;
                    text-decoration: none;
                    padding: 14px 32px;
                    border-radius: 40px;
                    font-weight: 600;
                    font-size: 16px;
                    transition: all 0.3s ease;
                    border: 2px solid #E8C7C3;
                    margin: 10px;
                }}
                .button:hover {{
                    transform: translateY(-2px);
                    box-shadow: 0 8px 24px rgba(232,199,195,0.4);
                }}
                .button-secondary:hover {{
                    background-color: #FFFFFF;
                }}
                .footer {{
                    background-color: #F5EDEB;
                    padding: 24px;
                    color: #8A8A8A;
                    font-size: 14px;
                }}
                .price {{
                    color: #1E1E1E;
                    font-size: 20px;
                    font-weight: 700;
                }}
                .email-notice {{
                    color: #8A8A8A;
                    font-size: 12px;
                    margin-top: 30px;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='content'>
                    <div class='success-icon'>✓</div>
                    <h2 class='title'>{result.Message}</h2>
                    <div class='booking-number'>
                        Buchungsnummer: {booking.BookingNumber}
                    </div>
                    
                    <div class='details-card'>
                        <div class='detail-row'>
                            <span class='detail-label'>Leistung</span>
                            <span class='detail-value'>{booking.Booking.ServiceName}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Datum</span>
                            <span class='detail-value'>{booking.Booking.BookingDate}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Uhrzeit</span>
                            <span class='detail-value'>{booking.Booking.StartTime} - {booking.Booking.EndTime}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Preis</span>
                            <span class='detail-value'><span class='price'>{booking.Booking.Price:0.00} CHF</span></span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Status</span>
                            <span class='detail-value'><span class='status-cancelled'>Storniert</span></span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Grund</span>
                            <span class='detail-value'>Storniert vom Kunden per E-Mail-Link</span>
                        </div>
                    </div>
                    
                    <div style='margin: 30px 0;'>
                        <a href='https://www.skinbloom-aesthetics.ch/booking' class='button'>
                            Neuen Termin buchen
                        </a>
                        <a href='https://www.skinbloom-aesthetics.ch/' class='button-secondary'>
                            Zurück zur Website
                        </a>
                    </div>
                    
                    <p class='email-notice'>
                        * Sie erhalten eine Bestätigungs-Email an: {booking.Customer.Email}
                    </p>
                </div>
                <div class='footer'>
                    <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                    <p style='margin: 10px 0 0 0;'>www.skinbloom-aesthetics.ch</p>
                    <p style='margin: 10px 0 0 0;'>© 2026 Skinbloom Aesthetics</p>
                </div>
            </div>
        </body>
        </html>", "text/html");
        }
        catch (InvalidOperationException ex)
        {
            return Content($@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Fehler - Skinbloom Aesthetics</title>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ 
                    font-family: 'Helvetica', 'Arial', sans-serif; 
                    text-align: center; 
                    padding: 0; 
                    margin: 0;
                    background-color: #F5EDEB;
                    color: #1E1E1E;
                    line-height: 1.6;
                }}
                .container {{
                    max-width: 600px;
                    margin: 40px auto;
                    background-color: #FFFFFF;
                    border-radius: 24px;
                    box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                    overflow: hidden;
                }}
                .header {{
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    padding: 40px 20px;
                }}
                .content {{
                    padding: 40px;
                }}
                .error {{
                    color: #D8B0AC;
                    font-size: 48px;
                    margin-bottom: 20px;
                }}
                .title {{
                    font-size: 24px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 20px;
                }}
                .message {{
                    color: #8A8A8A;
                    margin-bottom: 30px;
                }}
                .button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    color: #FFFFFF;
                    text-decoration: none;
                    padding: 14px 32px;
                    border-radius: 40px;
                    font-weight: 600;
                    font-size: 16px;
                    transition: all 0.3s ease;
                    box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                }}
                .footer {{
                    background-color: #F5EDEB;
                    padding: 24px;
                    color: #8A8A8A;
                    font-size: 14px;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                </div>
                <div class='content'>
                    <div class='error'>✧</div>
                    <h2 class='title'>Fehler bei der Stornierung</h2>
                    <p class='message'>{ex.Message}</p>
                    <p style='margin-top: 30px;'>
                        <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                    </p>
                </div>
                <div class='footer'>
                    <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                </div>
            </div>
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
            <title>Fehler - Skinbloom Aesthetics</title>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ 
                    font-family: 'Helvetica', 'Arial', sans-serif; 
                    text-align: center; 
                    padding: 0; 
                    margin: 0;
                    background-color: #F5EDEB;
                    color: #1E1E1E;
                    line-height: 1.6;
                }}
                .container {{
                    max-width: 600px;
                    margin: 40px auto;
                    background-color: #FFFFFF;
                    border-radius: 24px;
                    box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                    overflow: hidden;
                }}
                .header {{
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    padding: 40px 20px;
                }}
                .content {{
                    padding: 40px;
                }}
                .error {{
                    color: #D8B0AC;
                    font-size: 48px;
                    margin-bottom: 20px;
                }}
                .title {{
                    font-size: 24px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 20px;
                }}
                .message {{
                    color: #8A8A8A;
                    margin-bottom: 30px;
                }}
                .button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    color: #FFFFFF;
                    text-decoration: none;
                    padding: 14px 32px;
                    border-radius: 40px;
                    font-weight: 600;
                    font-size: 16px;
                    transition: all 0.3s ease;
                    box-shadow: 0 4px 12px rgba(232,199,195,0.3);
                }}
                .footer {{
                    background-color: #F5EDEB;
                    padding: 24px;
                    color: #8A8A8A;
                    font-size: 14px;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1 style='color: #FFFFFF; font-size: 28px; font-weight: 600; margin: 0;'>Skinbloom Aesthetics</h1>
                </div>
                <div class='content'>
                    <div class='error'>✧</div>
                    <h2 class='title'>Unerwarteter Fehler</h2>
                    <p class='message'>Ein unerwarteter Fehler ist aufgetreten. Bitte versuchen Sie es später erneut oder kontaktieren Sie uns.</p>
                    <p style='margin-top: 30px;'>
                        <a href='https://www.skinbloom-aesthetics.ch/' class='button'>Zurück zur Website</a>
                    </p>
                </div>
                <div class='footer'>
                    <p style='margin: 0;'>Elisabethenstrasse 41, 4051 Basel, Schweiz</p>
                </div>
            </div>
        </body>
        </html>", "text/html");
        }
    }
}
