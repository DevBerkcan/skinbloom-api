# Email-System - Komplettlösung

## Übersicht

Das Email-System sendet automatisch E-Mails zu verschiedenen Buchungsphasen und ermöglicht Newsletter-Versand an Kunden.

## Features

✅ **Buchungsbestätigung** (bereits vorhanden)
✅ **Stornierungsbestätigung** (bereits vorhanden)
✅ **Automatische Erinnerungen** (24h vor Termin) - NEU
✅ **Follow-up Emails** (1 Tag nach Termin) - NEU
✅ **Newsletter-Funktion** - Wird noch implementiert

## Automatische Emails

### 1. Buchungsbestätigung
**Trigger**: Sofort bei Buchungserstellung
**Inhalt**:
- Termindetails (Datum, Uhrzeit, Service)
- Buchungsnummer
- Preis
- Standort & Kontaktdaten
- Stornierungshinweise

**Bereits implementiert**: ✅ `EmailService.SendBookingConfirmationAsync()`

### 2. Terminbestätigung (durch Admin)
**Trigger**: Admin bestätigt Buchung (Status: Pending → Confirmed)
**Inhalt**: Ähnlich wie Buchungsbestätigung
**Bereits implementiert**: ✅ Verwendet `SendBookingConfirmationAsync()`

### 3. Erinnerung (24h vorher)
**Trigger**: Automatisch, 24h vor Termin
**Frequenz**: Stündliche Prüfung durch Hangfire Job
**Inhalt**:
- "Ihr Termin ist morgen!"
- Termindetails
- Vorbereitungshinweise
- Stornierungslink (optional)

**Implementierung**:
- Service: `EmailReminderService.SendUpcomingBookingRemindersAsync()`
- Email-Template: `EmailService.GenerateReminderEmailHtml()`
- Hangfire Job: Läuft stündlich

**Konfiguration**:
```csharp
// In Program.cs nach Hangfire-Initialisierung:
EmailJobsConfiguration.ConfigureEmailJobs();
```

**Funktionsweise**:
1. Job läuft jede Stunde
2. Findet alle bestätigten Buchungen in den nächsten 23-25 Stunden
3. Prüft ob bereits Erinnerung gesendet (`ReminderSentAt` ist null)
4. Sendet Email
5. Markiert `ReminderSentAt = DateTime.UtcNow`
6. Loggt Email in `EmailLogs` Tabelle

### 4. Follow-up Email (nach Termin)
**Trigger**: Automatisch, 1 Tag nach abgeschlossenem Termin
**Frequenz**: Täglich um 10:00 Uhr
**Inhalt**:
- Dankeschön für Besuch
- Feedback-Anfrage
- Google Review Link
- Nachbehandlungs-Hinweise
- Call-to-Action: Nächsten Termin buchen
- Social Media Links

**Implementierung**:
- Service: `EmailReminderService.SendFollowUpEmailsAsync()`
- Email-Template: `EmailService.GenerateFollowUpEmailHtml()`
- Hangfire Job: Läuft täglich um 10:00 Uhr

**Funktionsweise**:
1. Job läuft täglich um 10:00 Uhr
2. Findet alle abgeschlossenen Buchungen von gestern
3. Prüft ob bereits Follow-up gesendet (in `EmailLogs`)
4. Sendet Email
5. Loggt Email in `EmailLogs` Tabelle

### 5. Stornierungsbestätigung
**Trigger**: Bei Buchungsstornierung
**Inhalt**:
- Bestätigung der Stornierung
- Stornierungsgrund (falls angegeben)
- Link zur erneuten Buchung

**Bereits implementiert**: ✅ `EmailService.SendCancellationConfirmationAsync()`

## Email-Templates

Alle Emails verwenden:
- **Branding**: Skinbloom Aesthetics Farben (Schwarz/Weiß)
- **Responsive Design**: Mobile-optimiert
- **Professionelles Layout**: Header, Content, Footer
- **Call-to-Actions**: Buttons für Buchungen, Reviews, etc.
- **Kontaktdaten**: Adresse, Telefon, Email im Footer

## Newsletter-System (TODO)

### Funktionen
- Kunden können Newsletter abonnieren
- Admin kann Newsletter erstellen und versenden
- Segmentierung nach Kundengruppen (optional)
- Tracking: Öffnungsrate, Klickrate

### Implementierung (ausstehend)
```csharp
// Customer.cs erweitern
public bool NewsletterSubscribed { get; set; }
public DateTime? NewsletterSubscribedAt { get; set; }

// Neue Entity: Newsletter.cs
public class Newsletter
{
    public Guid Id { get; set; }
    public string Subject { get; set; }
    public string HtmlContent { get; set; }
    public DateTime? SentAt { get; set; }
    public int RecipientCount { get; set; }
}

// Service: NewsletterService.cs
public async Task SendNewsletterAsync(Newsletter newsletter)
{
    var subscribers = await _context.Customers
        .Where(c => c.NewsletterSubscribed)
        .ToListAsync();

    foreach (var customer in subscribers)
    {
        await _emailService.SendNewsletterAsync(customer, newsletter);
    }
}
```

## Installation & Konfiguration

### 1. Email-Konfiguration in appsettings.Production.json

Bereits konfiguriert mit Brevo (Sendinblue):
```json
{
  "Email": {
    "SmtpHost": "smtp-relay.brevo.com",
    "SmtpPort": 587,
    "SmtpUsername": "berkcan@gentle-webdesign.com",
    "SmtpPassword": "xsmtpsib-...",
    "SenderEmail": "noreply@skinbloom-aesthetics.ch",
    "SenderName": "Skinbloom Aesthetics",
    "EnableSsl": true
  }
}
```

### 2. Hangfire Konfiguration in Program.cs

```csharp
using BarberDario.Api.BackgroundJobs;
using Hangfire;

// ... Nach Hangfire-Initialisierung:

// Configure recurring email jobs
EmailJobsConfiguration.ConfigureEmailJobs();
```

### 3. Service Registration in Program.cs

```csharp
// Services registrieren
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EmailReminderService>();
```

### 4. Datenbank

Keine neuen Tabellen erforderlich! Verwendet bestehende `EmailLogs` Tabelle.

**EmailLog Enum-Erweiterung** (falls noch nicht vorhanden):
```csharp
public enum EmailType
{
    Confirmation,
    Reminder,      // NEU
    FollowUp,      // NEU
    Cancellation,
    Newsletter     // Für später
}
```

## API Endpoints (für manuelle Auslösung, optional)

### POST /api/admin/emails/send-reminders
Manuell alle fälligen Erinnerungen senden

### POST /api/admin/emails/send-followups
Manuell alle Follow-ups senden

```csharp
// In AdminController.cs
[HttpPost("emails/send-reminders")]
public async Task<IActionResult> TriggerReminders(
    [FromServices] EmailReminderService reminderService)
{
    await reminderService.SendUpcomingBookingRemindersAsync();
    return Ok(new { message = "Reminders sent" });
}

[HttpPost("emails/send-followups")]
public async Task<IActionResult> TriggerFollowUps(
    [FromServices] EmailReminderService reminderService)
{
    await reminderService.SendFollowUpEmailsAsync();
    return Ok(new { message = "Follow-ups sent" });
}
```

## Email-Logs überwachen

### Alle Email-Logs abrufen
```sql
SELECT
    el.EmailType,
    el.Status,
    el.RecipientEmail,
    el.SentAt,
    el.ErrorMessage,
    b.BookingNumber,
    b.BookingDate
FROM EmailLogs el
LEFT JOIN Bookings b ON el.BookingId = b.Id
ORDER BY el.SentAt DESC;
```

### Fehlgeschlagene Emails
```sql
SELECT
    el.*,
    c.FirstName,
    c.LastName
FROM EmailLogs el
JOIN Bookings b ON el.BookingId = b.Id
JOIN Customers c ON b.CustomerId = c.Id
WHERE el.Status = 'Failed'
ORDER BY el.SentAt DESC;
```

### Email-Statistiken
```sql
SELECT
    EmailType,
    Status,
    COUNT(*) AS Count,
    COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (PARTITION BY EmailType) AS Percentage
FROM EmailLogs
GROUP BY EmailType, Status
ORDER BY EmailType, Status;
```

## Häufige Probleme & Lösungen

### Problem: Erinnerungen werden nicht gesendet

**Ursache 1**: Hangfire Job nicht konfiguriert
**Lösung**: `EmailJobsConfiguration.ConfigureEmailJobs()` in Program.cs aufrufen

**Ursache 2**: Booking Status ist nicht "Confirmed"
**Lösung**: Nur bestätigte Buchungen bekommen Erinnerungen

**Ursache 3**: ReminderSentAt ist bereits gesetzt
**Lösung**: Erinnerung wird nur einmal gesendet

### Problem: Follow-ups werden mehrfach gesendet

**Ursache**: Doppelte Job-Ausführung
**Lösung**: Job prüft bereits ob Follow-up in EmailLogs existiert

### Problem: SMTP Fehler

**Ursache**: Falsche Brevo-Credentials
**Lösung**: appsettings.Production.json prüfen

## Best Practices

### 1. Zeitzone beachten
Alle Zeiten in UTC speichern, für Anzeige konvertieren:
```csharp
var localTime = DateTime.SpecifyKind(booking.BookingDate.ToDateTime(booking.StartTime), DateTimeKind.Utc)
    .ToLocalTime();
```

### 2. Email-Rate-Limits
Brevo Free Plan: 300 Emails/Tag
→ Bei vielen Buchungen auf bezahlten Plan upgraden

### 3. Unsubscribe-Link
In Follow-up Emails Abmelde-Link einbauen (DSGVO):
```html
<a href="https://gentlelink-skinbloom.vercel.app/unsubscribe?email={customer.Email}">
    Keine weiteren Emails erhalten
</a>
```

### 4. A/B Testing
Verschiedene Email-Varianten testen:
- Betreffzeilen
- Call-to-Action Texte
- Sendezeiten für Follow-ups

### 5. Personalisierung
- Kundenname verwenden
- Behandlung erwähnen
- Individuelle Nachbehandlungs-Tipps (basierend auf Service)

## Monitoring & Analytics

### Metriken zu tracken:
- **Öffnungsrate**: Wie viele Emails werden geöffnet?
- **Klickrate**: Wie viele klicken auf "Termin buchen"?
- **Conversion Rate**: Wie viele buchen erneut?
- **Abmelderate**: Wie viele melden Newsletter ab?

### Empfohlene Tools:
- Brevo Analytics Dashboard
- Google Analytics (UTM-Parameter in Email-Links)
- Custom Tracking in eigener DB

## Zusammenfassung

### Bereits implementiert ✅
- Buchungsbestätigung
- Stornierungsbestätigung
- Automatische Erinnerungen (24h vorher)
- Follow-up Emails (1 Tag nach Termin)
- Hangfire Background Jobs
- Email-Logging

### Noch zu implementieren 📋
- Newsletter-System
- Unsubscribe-Funktion
- Email-Vorlagen im Admin-Bereich editierbar machen
- A/B Testing Framework

### Dateien erstellt
- `Services/EmailReminderService.cs` - Background Service für Erinnerungen & Follow-ups
- `BackgroundJobs/EmailJobsConfiguration.cs` - Hangfire Job-Konfiguration
- `Services/EmailService.cs` - Erweitert mit `SendFollowUpAsync()` und Template

---

**Nächste Schritte nach Deployment:**
1. Hangfire Dashboard prüfen: `/hangfire`
2. Jobs manuell auslösen zum Testen
3. Email-Logs überwachen
4. Bei Bedarf Sendezeiten anpassen
