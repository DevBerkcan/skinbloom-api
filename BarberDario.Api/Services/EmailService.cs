using BarberDario.Api.Data.Entities;
using BarberDario.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BarberDario.Api.Services;

public class EmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(Booking booking, Customer customer, Service service)
    {
        var subject = $"Terminbestätigung - {service.Name}";
        var body = GenerateBookingConfirmationHtml(booking, customer, service);

        await SendEmailAsync(customer.Email, customer.FirstName, subject, body);
    }

    public async Task SendCancellationConfirmationAsync(Booking booking, Customer customer, Service service)
    {
        var subject = "Terminstornierung bestätigt";
        var body = GenerateCancellationConfirmationHtml(booking, customer, service);

        await SendEmailAsync(customer.Email, customer.FirstName, subject, body);
    }

    public async Task SendReminderEmailAsync(Booking booking, Customer customer, Service service)
    {
        var subject = $"Erinnerung: Dein Termin morgen - {service.Name}";
        var body = GenerateReminderEmailHtml(booking, customer, service);

        await SendEmailAsync(customer.Email, customer.FirstName, subject, body);
    }

    public async Task SendFollowUpAsync(Booking booking, Customer customer, Service service)
    {
        var subject = "Wie war Ihre Behandlung? Wir freuen uns auf Ihr Feedback!";
        var body = GenerateFollowUpEmailHtml(booking, customer, service);

        await SendEmailAsync(customer.Email, customer.FirstName, subject, body);
    }

    public async Task SendNewsletterEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.SenderName, _emailOptions.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailOptions.SmtpHost, _emailOptions.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    private string GenerateBookingConfirmationHtml(Booking booking, Customer customer, Service service)
    {
        var bookingDate = booking.BookingDate.ToString("dddd, dd. MMMM yyyy", new System.Globalization.CultureInfo("de-DE"));

        return $@"
<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #000000 0%, #1F2937 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.95; font-size: 16px; }}
        .content {{ padding: 40px 30px; }}
        .booking-details {{ background-color: #fef5e7; border-left: 4px solid #000000; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .booking-details h2 {{ margin: 0 0 15px 0; color: #000000; font-size: 18px; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f0f0f0; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: 600; color: #555; }}
        .detail-value {{ color: #333; }}
        .info-box {{ background-color: #e8f4f8; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .info-box p {{ margin: 5px 0; color: #0066a1; }}
        .footer {{ background-color: #2d2d2d; color: #ffffff; padding: 30px; text-align: center; }}
        .footer p {{ margin: 5px 0; font-size: 14px; }}
        .button {{ display: inline-block; background-color: #000000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: 600; }}
        .checkmark {{ width: 60px; height: 60px; margin: 0 auto 20px; background-color: #4caf50; border-radius: 50%; display: flex; align-items: center; justify-content: center; }}
        .checkmark::after {{ content: '✓'; color: white; font-size: 36px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='checkmark'></div>
            <h1>Termin bestätigt!</h1>
            <p>Buchungsnummer: {booking.BookingNumber}</p>
        </div>

        <div class='content'>
            <p>Hallo {customer.FirstName},</p>
            <p>vielen Dank für deine Buchung! Dein Termin wurde erfolgreich bestätigt.</p>

            <div class='booking-details'>
                <h2>📅 Deine Buchungsdetails</h2>
                <div class='detail-row'>
                    <span class='detail-label'>Leistung:</span>
                    <span class='detail-value'>{service.Name}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Datum:</span>
                    <span class='detail-value'>{bookingDate}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Uhrzeit:</span>
                    <span class='detail-value'>{booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm} Uhr</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Dauer:</span>
                    <span class='detail-value'>{service.DurationMinutes} Minuten</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Preis:</span>
                    <span class='detail-value'>ab CHF {service.Price:F2}</span>
                </div>
            </div>

            <div class='info-box'>
                <p><strong>📍 Standort:</strong> Elisabethenstrasse 41, 4051 Basel</p>
                <p><strong>📧 Du erhältst 24 Stunden vor deinem Termin eine Erinnerung.</strong></p>
            </div>

            <p><strong>Wichtige Hinweise:</strong></p>
            <ul style='color: #555; line-height: 1.8;'>
                <li>Bitte erscheine pünktlich zu deinem Termin</li>
                <li>Falls du verhindert bist, storniere bitte rechtzeitig</li>
                <li>Bei Verspätungen über 10 Minuten kann der Termin verkürzt werden</li>
            </ul>

            <p>Wir freuen uns auf deinen Besuch!</p>
            <p>Dein Team von Skinbloom Aesthetics</p>
        </div>

        <div class='footer'>
            <p><strong>Skinbloom Aesthetics</strong></p>
            <p>Elisabethenstrasse 41, 4051 Basel</p>
            <p>Tel: +41 78 241 87 04 | Email: info@skinbloom-aesthetics.ch</p>
            <p style='margin-top: 15px; font-size: 12px; color: #999;'>
                © 2025 Skinbloom Aesthetics. Alle Rechte vorbehalten.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateCancellationConfirmationHtml(Booking booking, Customer customer, Service service)
    {
        var bookingDate = booking.BookingDate.ToString("dddd, dd. MMMM yyyy", new System.Globalization.CultureInfo("de-DE"));

        return $@"
<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #666 0%, #444 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .booking-details {{ background-color: #f5f5f5; border-left: 4px solid #666; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .footer {{ background-color: #2d2d2d; color: #ffffff; padding: 30px; text-align: center; }}
        .footer p {{ margin: 5px 0; font-size: 14px; }}
        .button {{ display: inline-block; background-color: #000000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: 600; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Termin storniert</h1>
            <p>Buchungsnummer: {booking.BookingNumber}</p>
        </div>

        <div class='content'>
            <p>Hallo {customer.FirstName},</p>
            <p>dein Termin wurde erfolgreich storniert.</p>

            <div class='booking-details'>
                <h3>Stornierte Buchung:</h3>
                <p><strong>Leistung:</strong> {service.Name}</p>
                <p><strong>Datum:</strong> {bookingDate}</p>
                <p><strong>Uhrzeit:</strong> {booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm} Uhr</p>
            </div>

            <p>Du kannst jederzeit einen neuen Termin buchen.</p>

            <a href='https://skinbloom-aesthetics.ch/booking' class='button'>Neuen Termin buchen</a>

            <p>Wir hoffen, dich bald wieder begrüßen zu dürfen!</p>
            <p>Dein Team von Skinbloom Aesthetics</p>
        </div>

        <div class='footer'>
            <p><strong>Skinbloom Aesthetics</strong></p>
            <p>Elisabethenstrasse 41, 4051 Basel</p>
            <p>Tel: +41 78 241 87 04 | Email: info@skinbloom-aesthetics.ch</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateReminderEmailHtml(Booking booking, Customer customer, Service service)
    {
        var bookingDate = booking.BookingDate.ToString("dddd, dd. MMMM yyyy", new System.Globalization.CultureInfo("de-DE"));
        var tomorrow = booking.BookingDate.AddDays(-1);

        return $@"
<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #ff9800 0%, #f57c00 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .booking-details {{ background-color: #fff3e0; border-left: 4px solid #ff9800; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .booking-details h2 {{ margin: 0 0 15px 0; color: #ff9800; font-size: 18px; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f0f0f0; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: 600; color: #555; }}
        .detail-value {{ color: #333; }}
        .alert-box {{ background-color: #fff3cd; border: 2px solid #ffc107; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center; }}
        .alert-box p {{ margin: 5px 0; color: #856404; font-weight: 600; font-size: 16px; }}
        .footer {{ background-color: #2d2d2d; color: #ffffff; padding: 30px; text-align: center; }}
        .footer p {{ margin: 5px 0; font-size: 14px; }}
        .clock-icon {{ font-size: 48px; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='clock-icon'>⏰</div>
            <h1>Terminerinnerung</h1>
            <p>Dein Termin ist morgen!</p>
        </div>

        <div class='content'>
            <p>Hallo {customer.FirstName},</p>

            <div class='alert-box'>
                <p>🗓️ Dein Termin ist morgen!</p>
            </div>

            <p>Wir freuen uns darauf, dich morgen bei uns begrüßen zu dürfen.</p>

            <div class='booking-details'>
                <h2>📋 Deine Termindetails</h2>
                <div class='detail-row'>
                    <span class='detail-label'>Buchungsnummer:</span>
                    <span class='detail-value'>{booking.BookingNumber}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Leistung:</span>
                    <span class='detail-value'>{service.Name}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Datum:</span>
                    <span class='detail-value'>{bookingDate}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Uhrzeit:</span>
                    <span class='detail-value'>{booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm} Uhr</span>
                </div>
            </div>

            <p><strong>📍 Standort:</strong><br>
            Skinbloom Aesthetics<br>
            Elisabethenstrasse 41<br>
            4051 Basel</p>

            <p><strong>Bitte beachte:</strong></p>
            <ul style='color: #555; line-height: 1.8;'>
                <li>Erscheine bitte pünktlich</li>
                <li>Bei Verhinderung, bitte rechtzeitig stornieren</li>
                <li>Parkplätze sind vor Ort verfügbar</li>
            </ul>

            <p>Bis morgen!</p>
            <p>Dein Team von Skinbloom Aesthetics</p>
        </div>

        <div class='footer'>
            <p><strong>Skinbloom Aesthetics</strong></p>
            <p>Elisabethenstrasse 41, 4051 Basel</p>
            <p>Tel: +41 78 241 87 04 | Email: info@skinbloom-aesthetics.ch</p>
            <p style='margin-top: 15px; font-size: 12px; color: #999;'>
                © 2025 Skinbloom Aesthetics. Alle Rechte vorbehalten.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateFollowUpEmailHtml(Booking booking, Customer customer, Service service)
    {
        var bookingDate = booking.BookingDate.ToString("dd. MMMM yyyy", new System.Globalization.CultureInfo("de-DE"));

        return $@"
<!DOCTYPE html>
<html lang='de'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #000000 0%, #1F2937 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.95; font-size: 16px; }}
        .content {{ padding: 40px 30px; }}
        .treatment-box {{ background-color: #fef5e7; border-left: 4px solid #000000; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .treatment-box h2 {{ margin: 0 0 10px 0; color: #000000; font-size: 18px; }}
        .feedback-box {{ background-color: #e8f4f8; border-radius: 8px; padding: 25px; margin: 25px 0; text-align: center; }}
        .feedback-box h3 {{ margin: 0 0 15px 0; color: #0066a1; font-size: 20px; }}
        .btn {{ display: inline-block; background: linear-gradient(135deg, #000000 0%, #1F2937 100%); color: white; text-decoration: none; padding: 15px 35px; border-radius: 8px; font-weight: 600; margin: 10px 5px; }}
        .btn:hover {{ opacity: 0.9; }}
        .social-links {{ text-align: center; margin: 30px 0; }}
        .social-links a {{ display: inline-block; margin: 0 10px; color: #000000; text-decoration: none; font-size: 14px; }}
        .footer {{ background-color: #2d2d2d; color: #ffffff; padding: 30px; text-align: center; }}
        .footer p {{ margin: 5px 0; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Vielen Dank für Ihren Besuch!</h1>
            <p>Wir hoffen, Sie hatten eine angenehme Behandlung</p>
        </div>
        <div class='content'>
            <p>Liebe/r {customer.FirstName},</p>

            <p>vielen Dank, dass Sie sich am {bookingDate} für die Behandlung <strong>{service.Name}</strong> bei uns entschieden haben.</p>

            <div class='treatment-box'>
                <h2>Ihre Behandlung</h2>
                <p><strong>{service.Name}</strong></p>
                <p style='color: #666; font-size: 14px;'>{service.Description}</p>
            </div>

            <p>Wir hoffen sehr, dass Sie mit dem Ergebnis zufrieden sind und würden uns freuen, von Ihnen zu hören!</p>

            <div class='feedback-box'>
                <h3>Wie war Ihre Erfahrung?</h3>
                <p style='color: #555; margin-bottom: 20px;'>Ihr Feedback hilft uns, unseren Service stetig zu verbessern</p>
                <a href='https://g.page/r/YOUR_GOOGLE_REVIEW_LINK' class='btn'>Bewertung auf Google hinterlassen</a>
                <br>
                <a href='mailto:info@skinbloom-aesthetics.ch?subject=Feedback%20zu%20meiner%20Behandlung' style='color: #0066a1; text-decoration: underline; font-size: 14px; display: inline-block; margin-top: 15px;'>
                    Oder direkt per E-Mail antworten
                </a>
            </div>

            <h3 style='color: #000000; margin-top: 30px;'>Nachbehandlung</h3>
            <p>Bitte beachten Sie folgende Hinweise für optimale Ergebnisse:</p>
            <ul style='color: #555; line-height: 1.8;'>
                <li>Vermeiden Sie direkte Sonneneinstrahlung für 48h</li>
                <li>Keine intensiven sportlichen Aktivitäten für 24h</li>
                <li>Bei Fragen oder Unsicherheiten kontaktieren Sie uns gerne</li>
            </ul>

            <h3 style='color: #000000; margin-top: 30px;'>Nächster Termin?</h3>
            <p>Für optimale Ergebnisse empfehlen wir eine Folgebehandlung. Vereinbaren Sie jetzt Ihren nächsten Termin:</p>
            <p style='text-align: center; margin: 25px 0;'>
                <a href='https://gentlelink-skinbloom.vercel.app/booking' class='btn'>Jetzt Termin buchen</a>
            </p>

            <div class='social-links'>
                <p style='margin-bottom: 15px; color: #555;'>Folgen Sie uns auf Social Media:</p>
                <a href='https://instagram.com/skinbloom' target='_blank'>Instagram</a> •
                <a href='https://facebook.com/skinbloom' target='_blank'>Facebook</a>
            </div>

            <p style='margin-top: 30px; color: #555;'>Wir freuen uns darauf, Sie bald wiederzusehen!</p>

            <p style='margin-top: 20px;'>
                Herzliche Grüße,<br>
                <strong>Ihr Skinbloom Aesthetics Team</strong>
            </p>
        </div>
        <div class='footer'>
            <p><strong>Skinbloom Aesthetics</strong></p>
            <p>Elisabethenstrasse 41, 4051 Basel</p>
            <p>Tel: +41 78 241 87 04 | Email: info@skinbloom-aesthetics.ch</p>
            <p style='margin-top: 15px; font-size: 12px; color: #999;'>
                © 2025 Skinbloom Aesthetics. Alle Rechte vorbehalten.
            </p>
        </div>
    </div>
</body>
</html>";
    }
}
