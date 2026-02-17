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
            Subject = $"Ihre Buchungsbestätigung - Skinbloom Aesthetics",
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
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Buchungsbestätigung - Skinbloom Aesthetics</title>
            <style>
                body {{
                    font-family: 'Helvetica', 'Arial', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: #F5EDEB;
                    color: #1E1E1E;
                    line-height: 1.6;
                }}
                .container {{
                    max-width: 600px;
                    margin: 30px auto;
                    background-color: #FFFFFF;
                    border-radius: 24px;
                    box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                    overflow: hidden;
                }}
                .header {{
                    background: linear-gradient(135deg, #E8C7C3 0%, #D8B0AC 100%);
                    padding: 40px 30px;
                    text-align: center;
                }}
                .header h1 {{
                    color: #FFFFFF;
                    font-size: 28px;
                    font-weight: 600;
                    margin: 0;
                    letter-spacing: 1px;
                }}
                .header p {{
                    color: #FFFFFF;
                    font-size: 16px;
                    margin: 10px 0 0 0;
                    opacity: 0.9;
                }}
                .content {{
                    padding: 40px 30px;
                }}
                .greeting {{
                    font-size: 18px;
                    font-weight: 600;
                    color: #1E1E1E;
                    margin-bottom: 20px;
                }}
                .booking-card {{
                    background-color: #F5EDEB;
                    border-radius: 16px;
                    padding: 30px;
                    margin: 30px 0;
                }}
                .booking-title {{
                    font-size: 18px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 20px;
                    padding-bottom: 15px;
                    border-bottom: 2px solid #E8C7C3;
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
                .price {{
                    color: #1E1E1E;
                    font-size: 20px;
                    font-weight: 700;
                }}
                .status-badge {{
                    display: inline-block;
                    background-color: #E8C7C3;
                    color: #FFFFFF;
                    padding: 6px 16px;
                    border-radius: 40px;
                    font-size: 14px;
                    font-weight: 600;
                    letter-spacing: 0.5px;
                }}
                .info-box {{
                    background-color: #FFFFFF;
                    border: 2px solid #E8C7C3;
                    border-radius: 16px;
                    padding: 24px;
                    margin: 30px 0;
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
                .cancel-section {{
                    text-align: center;
                    margin: 40px 0 20px;
                    padding: 30px;
                    background-color: #F5EDEB;
                    border-radius: 16px;
                }}
                .cancel-title {{
                    font-size: 18px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 10px;
                }}
                .cancel-text {{
                    color: #8A8A8A;
                    margin-bottom: 25px;
                    font-size: 15px;
                }}
                .button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #D8B0AC 0%, #C09995 100%);
                    color: #FFFFFF;
                    text-decoration: none;
                    padding: 14px 32px;
                    border-radius: 40px;
                    font-weight: 600;
                    font-size: 16px;
                    transition: all 0.3s ease;
                    box-shadow: 0 4px 12px rgba(216,176,172,0.3);
                    border: none;
                    cursor: pointer;
                }}
                .button:hover {{
                    transform: translateY(-2px);
                    box-shadow: 0 8px 24px rgba(216,176,172,0.4);
                }}
                .footer {{
                    background-color: #F5EDEB;
                    padding: 30px;
                    text-align: center;
                    color: #8A8A8A;
                    font-size: 14px;
                }}
                .footer-links {{
                    margin-top: 20px;
                }}
                .footer-links a {{
                    color: #8A8A8A;
                    text-decoration: none;
                    margin: 0 10px;
                    font-size: 13px;
                }}
                .footer-links a:hover {{
                    color: #E8C7C3;
                    text-decoration: underline;
                }}
                .divider {{
                    display: inline-block;
                    color: #E8C7C3;
                    margin: 0 5px;
                }}
                .address {{
                    margin-top: 20px;
                    font-style: normal;
                }}
                @media only screen and (max-width: 600px) {{
                    .container {{
                        margin: 20px;
                        width: auto;
                    }}
                    .content {{
                        padding: 30px 20px;
                    }}
                    .detail-row {{
                        flex-direction: column;
                    }}
                    .detail-label {{
                        width: 100%;
                        margin-bottom: 5px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div style='color: #FFFFFF; font-size: 48px; font-weight: 300; margin-bottom: 15px;'>✧</div>
                    <h1>Skinbloom Aesthetics</h1>
                    <p>Ihre Buchung ist bestätigt</p>
                </div>
                
                <div class='content'>
                    <div class='greeting'>
                        Hallo {booking.Customer.FirstName},
                    </div>
                    
                    <p style='color: #8A8A8A; margin-bottom: 30px;'>
                        Vielen Dank für Ihre Buchung bei Skinbloom Aesthetics. Ihr Termin wurde erfolgreich bestätigt.
                    </p>
                    
                    <div class='booking-card'>
                        <div class='booking-title'>
                            Buchungsdetails
                            <span style='float: right;'><span class='status-badge'>Bestätigt</span></span>
                        </div>
                        
                        <div class='detail-row'>
                            <span class='detail-label'>Buchungsnr.</span>
                            <span class='detail-value'>{booking.BookingNumber}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Service</span>
                            <span class='detail-value'>{booking.Service.Name}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Datum</span>
                            <span class='detail-value'>{booking.BookingDate:dd.MM.yyyy}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Uhrzeit</span>
                            <span class='detail-value'>{booking.StartTime:HH:mm} - {booking.EndTime:HH:mm} Uhr</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Dauer</span>
                            <span class='detail-value'>{booking.Service.DurationMinutes} Minuten</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Preis</span>
                            <span class='detail-value'><span class='price'>{booking.Service.Price:0.00} CHF</span></span>
                        </div>
                    </div>
                    
                    <div class='cancel-section'>
                        <div class='cancel-title'>Termin stornieren?</div>
                        <div class='cancel-text'>
                            Falls Sie Ihren Termin nicht wahrnehmen können, stornieren Sie diesen bitte rechtzeitig.
                        </div>
                        <a href='{cancellationUrl}' class='button'>
                            Termin stornieren
                        </a>
                        <p style='color: #8A8A8A; font-size: 12px; margin-top: 15px;'>
                            Die Stornierung ist bis 24 Stunden vor dem Termin kostenlos möglich.
                        </p>
                    </div>
                    
                    <div style='text-align: center; margin-top: 30px;'>
                        <p style='color: #8A8A8A; font-size: 14px;'>
                            Haben Sie Fragen? Kontaktieren Sie uns gerne.
                        </p>
                    </div>
                </div>
                
                <div class='footer'>
                    <div style='font-size: 24px; color: #E8C7C3; margin-bottom: 15px;'>✧</div>
                    <div style='font-weight: 600; color: #1E1E1E; margin-bottom: 10px;'>
                        Skinbloom Aesthetics
                    </div>
                    <div class='address'>
                        Elisabethenstrasse 41<br>
                        4051 Basel, Schweiz
                    </div>
                    <div style='margin-top: 15px;'>
                        Tel: +41 61 123 45 67<br>
                        Email: info@skinbloom-aesthetics.ch
                    </div>
                    <div class='footer-links'>
                        <a href='{_emailOptions.BaseUrl}/datenschutz'>Datenschutz</a>
                        <span class='divider'>|</span>
                        <a href='{_emailOptions.BaseUrl}/impressum'>Impressum</a>
                        <span class='divider'>|</span>
                        <a href='{_emailOptions.BaseUrl}/agb'>AGB</a>
                    </div>
                    <div style='margin-top: 20px; font-size: 12px;'>
                        © {DateTime.UtcNow.Year} Skinbloom Aesthetics. Alle Rechte vorbehalten.
                    </div>
                </div>
            </div>
        </body>
        </html>";

            builder.TextBody = $@"
SKINBLOOM AESTHETICS - IHRE BUCHUNGSBESTÄTIGUNG

------------------------------------------------
Hallo {booking.Customer.FirstName},

vielen Dank für Ihre Buchung bei Skinbloom Aesthetics. Ihr Termin wurde erfolgreich bestätigt.

BUCHUNGSDETAILS:
------------------------------------------------
Buchungsnummer: {booking.BookingNumber}
Service: {booking.Service.Name}
Datum: {booking.BookingDate:dd.MM.yyyy}
Uhrzeit: {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm} Uhr
Dauer: {booking.Service.DurationMinutes} Minuten
Preis: {booking.Service.Price:0.00} CHF
Status: Bestätigt

TERMIN STORNIEREN:
------------------------------------------------
Falls Sie Ihren Termin nicht wahrnehmen können:
{cancellationUrl}

Die Stornierung ist bis 24 Stunden vor dem Termin kostenlos möglich.

KONTAKT:
------------------------------------------------
Skinbloom Aesthetics
Elisabethenstrasse 41
4051 Basel, Schweiz

Tel: +41 61 123 45 67
Email: info@skinbloom-aesthetics.ch
Web: www.skinbloom-aesthetics.ch

------------------------------------------------
© {DateTime.UtcNow.Year} Skinbloom Aesthetics. Alle Rechte vorbehalten.
Datenschutz: {_emailOptions.BaseUrl}/datenschutz
Impressum: {_emailOptions.BaseUrl}/impressum
AGB: {_emailOptions.BaseUrl}/agb";

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
            Subject = $"Ihre Stornierung - Skinbloom Aesthetics",
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
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Stornierung bestätigt - Skinbloom Aesthetics</title>
            <style>
                body {{
                    font-family: 'Helvetica', 'Arial', sans-serif;
                    margin: 0;
                    padding: 0;
                    background-color: #F5EDEB;
                    color: #1E1E1E;
                    line-height: 1.6;
                }}
                .container {{
                    max-width: 600px;
                    margin: 30px auto;
                    background-color: #FFFFFF;
                    border-radius: 24px;
                    box-shadow: 0 20px 40px rgba(0,0,0,0.05);
                    overflow: hidden;
                }}
                .header {{
                    background: linear-gradient(135deg, #D8B0AC 0%, #C09995 100%);
                    padding: 40px 30px;
                    text-align: center;
                }}
                .header h1 {{
                    color: #FFFFFF;
                    font-size: 28px;
                    font-weight: 600;
                    margin: 0;
                    letter-spacing: 1px;
                }}
                .header p {{
                    color: #FFFFFF;
                    font-size: 16px;
                    margin: 10px 0 0 0;
                    opacity: 0.9;
                }}
                .content {{
                    padding: 40px 30px;
                }}
                .greeting {{
                    font-size: 18px;
                    font-weight: 600;
                    color: #1E1E1E;
                    margin-bottom: 20px;
                }}
                .cancellation-card {{
                    background-color: #F5EDEB;
                    border-radius: 16px;
                    padding: 30px;
                    margin: 30px 0;
                    border-left: 4px solid #C09995;
                }}
                .cancellation-title {{
                    font-size: 18px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 20px;
                    padding-bottom: 15px;
                    border-bottom: 2px solid #C09995;
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
                .price {{
                    color: #1E1E1E;
                    font-size: 20px;
                    font-weight: 700;
                }}
                .status-badge {{
                    display: inline-block;
                    background-color: #C09995;
                    color: #FFFFFF;
                    padding: 6px 16px;
                    border-radius: 40px;
                    font-size: 14px;
                    font-weight: 600;
                    letter-spacing: 0.5px;
                }}
                .info-box {{
                    background-color: #FFFFFF;
                    border: 2px solid #C09995;
                    border-radius: 16px;
                    padding: 24px;
                    margin: 30px 0;
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
                    color: #C09995;
                    position: absolute;
                    left: 0;
                    top: 8px;
                    font-size: 14px;
                }}
                .booking-section {{
                    text-align: center;
                    margin: 40px 0 20px;
                    padding: 30px;
                    background: linear-gradient(135deg, #F5EDEB 0%, #FFFFFF 100%);
                    border-radius: 16px;
                    border: 2px solid #E8C7C3;
                }}
                .booking-title {{
                    font-size: 18px;
                    font-weight: 700;
                    color: #1E1E1E;
                    margin-bottom: 10px;
                }}
                .booking-text {{
                    color: #8A8A8A;
                    margin-bottom: 25px;
                    font-size: 15px;
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
                    border: none;
                    cursor: pointer;
                }}
                .button:hover {{
                    transform: translateY(-2px);
                    box-shadow: 0 8px 24px rgba(232,199,195,0.4);
                }}
                .footer {{
                    background-color: #F5EDEB;
                    padding: 30px;
                    text-align: center;
                    color: #8A8A8A;
                    font-size: 14px;
                }}
                .footer-links {{
                    margin-top: 20px;
                }}
                .footer-links a {{
                    color: #8A8A8A;
                    text-decoration: none;
                    margin: 0 10px;
                    font-size: 13px;
                }}
                .footer-links a:hover {{
                    color: #C09995;
                    text-decoration: underline;
                }}
                .divider {{
                    display: inline-block;
                    color: #C09995;
                    margin: 0 5px;
                }}
                .address {{
                    margin-top: 20px;
                    font-style: normal;
                }}
                @media only screen and (max-width: 600px) {{
                    .container {{
                        margin: 20px;
                        width: auto;
                    }}
                    .content {{
                        padding: 30px 20px;
                    }}
                    .detail-row {{
                        flex-direction: column;
                    }}
                    .detail-label {{
                        width: 100%;
                        margin-bottom: 5px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div style='color: #FFFFFF; font-size: 48px; font-weight: 300; margin-bottom: 15px;'>✧</div>
                    <h1>Skinbloom Aesthetics</h1>
                    <p>Ihre Stornierung wurde bestätigt</p>
                </div>
                
                <div class='content'>
                    <div class='greeting'>
                        Hallo {customer.FirstName},
                    </div>
                    
                    <p style='color: #8A8A8A; margin-bottom: 30px;'>
                        Ihre Buchung bei Skinbloom Aesthetics wurde erfolgreich storniert.
                    </p>
                    
                    <div class='cancellation-card'>
                        <div class='cancellation-title'>
                            Stornierte Buchung
                            <span style='float: right;'><span class='status-badge'>Storniert</span></span>
                        </div>
                        
                        <div class='detail-row'>
                            <span class='detail-label'>Buchungsnr.</span>
                            <span class='detail-value'>{booking.BookingNumber}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Service</span>
                            <span class='detail-value'>{service.Name}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Datum</span>
                            <span class='detail-value'>{booking.BookingDate:dd.MM.yyyy}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Uhrzeit</span>
                            <span class='detail-value'>{booking.StartTime:HH:mm} - {booking.EndTime:HH:mm} Uhr</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Dauer</span>
                            <span class='detail-value'>{service.DurationMinutes} Minuten</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Preis</span>
                            <span class='detail-value'><span class='price'>{service.Price:0.00} CHF</span></span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Storniert am</span>
                            <span class='detail-value'>{DateTime.UtcNow:dd.MM.yyyy HH:mm} Uhr</span>
                        </div>
                    </div>
                    
                    
                    <div class='booking-section'>
                        <div class='booking-title'>Neuen Termin buchen?</div>
                        <div class='booking-text'>
                            Wir freuen uns, Sie bald wieder bei uns begrüßen zu dürfen.
                        </div>
                        <a href='https://skinbloombooking.gentlegroup.de/booking' class='button'>
                            Neuen Termin buchen
                        </a>
                    </div>
                    
                    <div style='text-align: center; margin-top: 30px;'>
                        <p style='color: #8A8A8A; font-size: 14px;'>
                            Wir hoffen, Sie bald wieder bei uns begrüßen zu dürfen.
                        </p>
                    </div>
                </div>
                
                <div class='footer'>
                    <div style='font-size: 24px; color: #C09995; margin-bottom: 15px;'>✧</div>
                    <div style='font-weight: 600; color: #1E1E1E; margin-bottom: 10px;'>
                        Skinbloom Aesthetics
                    </div>
                    <div class='address'>
                        Elisabethenstrasse 41<br>
                        4051 Basel, Schweiz
                    </div>
                    <div style='margin-top: 15px;'>
                        Tel: +41 61 123 45 67<br>
                        Email: info@skinbloom-aesthetics.ch
                    </div>
                    <div class='footer-links'>
                        <a href='{_emailOptions.BaseUrl}/datenschutz'>Datenschutz</a>
                        <span class='divider'>|</span>
                        <a href='{_emailOptions.BaseUrl}/impressum'>Impressum</a>
                        <span class='divider'>|</span>
                        <a href='{_emailOptions.BaseUrl}/agb'>AGB</a>
                    </div>
                    <div style='margin-top: 20px; font-size: 12px;'>
                        © {DateTime.UtcNow.Year} Skinbloom Aesthetics. Alle Rechte vorbehalten.
                    </div>
                </div>
            </div>
        </body>
        </html>";

            builder.TextBody = $@"
SKINBLOOM AESTHETICS - IHRE STORNIERUNGSBESTÄTIGUNG

------------------------------------------------
Hallo {customer.FirstName},

Ihre Buchung bei Skinbloom Aesthetics wurde erfolgreich storniert.

STORNIERTE BUCHUNG:
------------------------------------------------
Buchungsnummer: {booking.BookingNumber}
Service: {service.Name}
Datum: {booking.BookingDate:dd.MM.yyyy}
Uhrzeit: {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm} Uhr
Dauer: {service.DurationMinutes} Minuten
Preis: {service.Price:0.00} CHF
Status: Storniert
Storniert am: {DateTime.UtcNow:dd.MM.yyyy HH:mm} Uhr

INFORMATIONEN ZUR STORNIERUNG:
------------------------------------------------
• Die Stornierung wurde in unserem System erfasst
• Sie erhalten keine weitere Bestätigung per Post
• Bei bereits getätigten Zahlungen erfolgt die Rückerstattung innerhalb von 5-10 Werktagen
• Bei Fragen kontaktieren Sie uns bitte direkt

NEUEN TERMIN BUCHEN:
------------------------------------------------
Wir freuen uns, Sie bald wieder bei uns begrüßen zu dürfen.
https://skinbloombooking.gentlegroup.de/booking

KONTAKT:
------------------------------------------------
Skinbloom Aesthetics
Elisabethenstrasse 41
4051 Basel, Schweiz

Tel: +41 61 123 45 67
Email: info@skinbloom-aesthetics.ch
Web: www.skinbloom-aesthetics.ch

------------------------------------------------
© {DateTime.UtcNow.Year} Skinbloom Aesthetics. Alle Rechte vorbehalten.
Datenschutz: {_emailOptions.BaseUrl}/datenschutz
Impressum: {_emailOptions.BaseUrl}/impressum
AGB: {_emailOptions.BaseUrl}/agb";

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