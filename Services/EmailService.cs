// Update BarberDario.Api.Services/EmailService.cs
using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BarberDario.Api.Services;

public class EmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> emailOptions,
        SkinbloomDbContext context,
        ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _context = context;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new ArgumentException("Booking not found");
        }

        var emailLog = new EmailLog
        {
            BookingId = bookingId,
            EmailType = EmailType.Confirmation,
            RecipientEmail = booking.Customer.Email,
            Subject = $"Buchungsbestätigung: {booking.Service.Name} am {booking.BookingDate:dd.MM.yyyy}",
            Status = EmailStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Skinbloom Aesthetics", _emailOptions.SenderEmail));
            message.To.Add(new MailboxAddress(booking.Customer.FullName, booking.Customer.Email));
            message.Subject = emailLog.Subject;

            // Create HTML email with confirmation/cancellation links
            var builder = new BodyBuilder();

            var confirmationToken = GenerateConfirmationToken(bookingId);
            var cancellationToken = GenerateCancellationToken(bookingId);

            var confirmationUrl = $"{_emailOptions.BaseUrl}/api/bookings/confirm/{confirmationToken}";
            var cancellationUrl = $"{_emailOptions.BaseUrl}/api/bookings/cancel/{cancellationToken}";
            var rescheduleUrl = $"{_emailOptions.BaseUrl}/booking/reschedule/{bookingId}";

            builder.HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ display: inline-block; padding: 10px 20px; margin: 10px 5px; 
                                 background-color: #007bff; color: white; text-decoration: none; 
                                 border-radius: 5px; }}
                        .button.confirm {{ background-color: #28a745; }}
                        .button.cancel {{ background-color: #dc3545; }}
                        .button.reschedule {{ background-color: #ffc107; color: #212529; }}
                        .details {{ margin: 20px 0; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Ihre Buchungsbestätigung</h1>
                        </div>
                        <div class='content'>
                            <p>Hallo {booking.Customer.FirstName},</p>
                            <p>vielen Dank für Ihre Buchung bei Skinbloom Aesthetics.</p>
                            
                            <div class='details'>
                                <h3>Buchungsdetails:</h3>
                                <p><strong>Service:</strong> {booking.Service.Name}</p>
                                <p><strong>Datum:</strong> {booking.BookingDate:dd.MM.yyyy}</p>
                                <p><strong>Uhrzeit:</strong> {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm}</p>
                                <p><strong>Dauer:</strong> {booking.Service.DurationMinutes} Minuten</p>
                                <p><strong>Preis:</strong> {booking.Service.Price:C}</p>
                            </div>

                            <p>Bitte bestätigen Sie Ihre Buchung oder wählen Sie eine Option:</p>
                            
                            <div style='text-align: center;'>
                                <a href='{confirmationUrl}' class='button confirm'>Buchung bestätigen</a>
                                <a href='{rescheduleUrl}' class='button reschedule'>Termin ändern</a>
                                <a href='{cancellationUrl}' class='button cancel'>Buchung stornieren</a>
                            </div>

                            <p><small>Sie haben 24 Stunden Zeit, Ihre Buchung zu bestätigen.</small></p>
                        </div>
                        <div class='footer'>
                            <p>Skinbloom Aesthetics<br>
                            Kontakt: +49 123 456789<br>
                            Email: info@skinbloom.de</p>
                            <p><a href='{_emailOptions.BaseUrl}/datenschutz'>Datenschutz</a> | 
                               <a href='{_emailOptions.BaseUrl}/agb'>AGB</a></p>
                        </div>
                    </div>
                </body>
                </html>";

            builder.TextBody = $@"
                Buchungsbestätigung - Skinbloom Aesthetics

                Service: {booking.Service.Name}
                Datum: {booking.BookingDate:dd.MM.yyyy}
                Uhrzeit: {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm}
                Dauer: {booking.Service.DurationMinutes} Minuten
                Preis: {booking.Service.Price:C}

                Bitte bestätigen Sie Ihre Buchung innerhalb von 24 Stunden:
                Bestätigen: {confirmationUrl}
                Ändern: {rescheduleUrl}
                Stornieren: {cancellationUrl}

                Skinbloom Aesthetics
                Kontakt: +49 123 456789
                Email: info@skinbloom.de";

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailOptions.SmtpServer, _emailOptions.SmtpPort,
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;
            booking.ConfirmationSentAt = DateTime.UtcNow;

            _logger.LogInformation("Confirmation email sent to {Email}", booking.Customer.Email);
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send confirmation email to {Email}",
                booking.Customer.Email);
        }

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync();
    }

    public async Task SendConfirmationReceiptAsync(Booking booking, Customer customer, Service service)
    {
        var emailLog = new EmailLog
        {
            BookingId = booking.Id,
            EmailType = EmailType.Confirmation,
            RecipientEmail = customer.Email,
            Subject = $"Buchungsbestätigung: {service.Name} am {booking.BookingDate:dd.MM.yyyy}",
            Status = EmailStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Skinbloom Aesthetics", _emailOptions.SenderEmail));
            message.To.Add(new MailboxAddress(customer.FullName, customer.Email));
            message.Subject = emailLog.Subject;

            var builder = new BodyBuilder();

            builder.HtmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #d4edda; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .details {{ margin: 20px 0; padding: 15px; background-color: #f8f9fa; border-radius: 5px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>✅ Buchung erfolgreich bestätigt</h1>
                    </div>
                    <div class='content'>
                        <p>Hallo {customer.FirstName},</p>
                        <p>vielen Dank für die Bestätigung Ihrer Buchung bei Skinbloom Aesthetics.</p>
                        
                        <div class='details'>
                            <h3>Ihre Buchungsdetails:</h3>
                            <p><strong>Buchungsnummer:</strong> {booking.BookingNumber}</p>
                            <p><strong>Status:</strong> <span style='color: #28a745; font-weight: bold;'>Bestätigt</span></p>
                            <p><strong>Service:</strong> {service.Name}</p>
                            <p><strong>Datum:</strong> {booking.BookingDate:dd.MM.yyyy}</p>
                            <p><strong>Uhrzeit:</strong> {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm}</p>
                            <p><strong>Dauer:</strong> {service.DurationMinutes} Minuten</p>
                            <p><strong>Preis:</strong> {service.Price:C}</p>
                        </div>

                        <p>Wir freuen uns auf Sie!</p>
                    </div>
                </div>
            </body>
            </html>";

            builder.TextBody = $@"
            Ihre Buchung wurde erfolgreich bestätigt - Skinbloom Aesthetics
            
            Buchungsnummer: {booking.BookingNumber}
            Status: Bestätigt
            Service: {service.Name}
            Datum: {booking.BookingDate:dd.MM.yyyy}
            Uhrzeit: {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm}
            Dauer: {service.DurationMinutes} Minuten
            Preis: {service.Price:C}
            
            Wir freuen uns auf Sie!
            
            Skinbloom Aesthetics
            Kontakt: +49 123 456789";

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailOptions.SmtpServer, _emailOptions.SmtpPort,
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;

            _logger.LogInformation("Confirmation receipt sent to {Email}", customer.Email);
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send confirmation receipt to {Email}",
                customer.Email);
        }

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync();
    }

    public async Task SendCancellationConfirmationAsync(Booking booking, Customer customer, Service service)
    {
        var emailLog = new EmailLog
        {
            BookingId = booking.Id,
            EmailType = EmailType.Cancellation,
            RecipientEmail = customer.Email,
            Subject = $"Stornierung: {service.Name} am {booking.BookingDate:dd.MM.yyyy}",
            Status = EmailStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Skinbloom Aesthetics", _emailOptions.SenderEmail));
            message.To.Add(new MailboxAddress(customer.FullName, customer.Email));
            message.Subject = emailLog.Subject;

            var builder = new BodyBuilder();

            builder.HtmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #f8d7da; padding: 20px; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>❌ Buchung storniert</h1>
                    </div>
                    <div class='content'>
                        <p>Hallo {customer.FirstName},</p>
                        <p>Ihre Buchung bei Skinbloom Aesthetics wurde storniert.</p>
                        
                        <h3>Stornierte Buchung:</h3>
                        <p><strong>Service:</strong> {service.Name}</p>
                        <p><strong>Datum:</strong> {booking.BookingDate:dd.MM.yyyy}</p>
                        <p><strong>Uhrzeit:</strong> {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm}</p>
                        
                        <p>Wir hoffen, Sie bald wieder bei uns begrüßen zu dürfen.</p>
                    </div>
                </div>
            </body>
            </html>";

            builder.TextBody = $@"
            Buchung storniert - Skinbloom Aesthetics
            
            Ihre Buchung wurde storniert.
            
            Service: {service.Name}
            Datum: {booking.BookingDate:dd.MM.yyyy}
            Uhrzeit: {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm}
            
            Wir hoffen, Sie bald wieder bei uns begrüßen zu dürfen.
            
            Skinbloom Aesthetics
            Kontakt: +49 123 456789";

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailOptions.SmtpServer, _emailOptions.SmtpPort,
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;

            _logger.LogInformation("Cancellation confirmation sent to {Email}", customer.Email);
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send cancellation confirmation to {Email}",
                customer.Email);
        }

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync();
    }

    public async Task SendBookingReminderAsync(Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null || booking.Status != BookingStatus.Confirmed)
            return;

        var emailLog = new EmailLog
        {
            BookingId = bookingId,
            EmailType = EmailType.Reminder,
            RecipientEmail = booking.Customer.Email,
            Subject = $"Erinnerung: Termin am {booking.BookingDate:dd.MM.yyyy}",
            Status = EmailStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Skinbloom Aesthetics", _emailOptions.SenderEmail));
            message.To.Add(new MailboxAddress(booking.Customer.FullName, booking.Customer.Email));
            message.Subject = emailLog.Subject;

            var builder = new BodyBuilder();
            var cancellationToken = GenerateCancellationToken(bookingId);
            var cancellationUrl = $"{_emailOptions.BaseUrl}/api/bookings/cancel/{cancellationToken}";

            builder.HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #fff3cd; padding: 20px; text-align: center; }}
                        .button {{ display: inline-block; padding: 10px 20px; margin: 10px; 
                                 background-color: #dc3545; color: white; text-decoration: none; 
                                 border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>📅 Terminerinnerung</h2>
                        </div>
                        <p>Hallo {booking.Customer.FirstName},</p>
                        <p>dies ist eine freundliche Erinnerung an Ihren Termin morgen:</p>
                        <p><strong>{booking.Service.Name}</strong><br>
                           {booking.BookingDate:dd.MM.yyyy} um {booking.StartTime:HH:mm} Uhr</p>
                        <p>Bitte erscheinen Sie pünktlich.</p>
                        <p>Sollten Sie den Termin nicht wahrnehmen können, stornieren Sie bitte hier:</p>
                        <a href='{cancellationUrl}' class='button'>Termin stornieren</a>
                        <p>Wir freuen uns auf Sie!</p>
                    </div>
                </body>
                </html>";

            builder.TextBody = $@"
                Terminerinnerung - Skinbloom Aesthetics

                Termin: {booking.Service.Name}
                Datum: {booking.BookingDate:dd.MM.yyyy}
                Uhrzeit: {booking.StartTime:HH:mm}

                Bitte erscheinen Sie pünktlich.

                Stornierung: {cancellationUrl}

                Skinbloom Aesthetics";

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailOptions.SmtpServer, _emailOptions.SmtpPort,
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;
            booking.ReminderSentAt = DateTime.UtcNow;

            _logger.LogInformation("Reminder email sent to {Email}", booking.Customer.Email);
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send reminder email to {Email}",
                booking.Customer.Email);
        }

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync();
    }

    private string GenerateConfirmationToken(Guid bookingId)
    {
        return Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{bookingId}:{DateTime.UtcNow.Ticks}:confirm")
        ).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateCancellationToken(Guid bookingId)
    {
        return Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{bookingId}:{DateTime.UtcNow.Ticks}:cancel")
        ).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public (Guid bookingId, string action) DecodeToken(string token)
    {
        try
        {
            var base64 = token.Replace("-", "+").Replace("_", "/");
            while (base64.Length % 4 != 0)
                base64 += "=";

            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = decoded.Split(':');

            if (parts.Length == 3 && Guid.TryParse(parts[0], out var bookingId))
            {
                return (bookingId, parts[2]);
            }
        }
        catch
        {
            // Invalid token
        }

        return (Guid.Empty, string.Empty);
    }
}