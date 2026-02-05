# Skinbloom Aesthetics API

Backend API für das Buchungssystem von Skinbloom Aesthetics.

## 🚀 Tech Stack

- **.NET 8.0** - Web API
- **Entity Framework Core 8.0** - ORM
- **SQL Server** - Datenbank
- **Brevo API** - Email-Versand
- **Swagger/OpenAPI** - API-Dokumentation

## 📋 Voraussetzungen

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- **SQL Server** - Datenbank (MonsterASP oder andere)
- [Brevo](https://www.brevo.com/) Account für Email-Versand

## ⚙️ Setup

### 1. Datenbank erstellen

Erstelle eine neue SQL Server Datenbank auf MonsterASP oder einem anderen Hosting-Provider.

### 2. Konfiguration

Aktualisiere `appsettings.json` mit deinen Datenbank- und Brevo-Daten:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_DB_SERVER; Database=YOUR_DB_NAME; User Id=YOUR_USER; Password=YOUR_PASSWORD; Encrypt=False; MultipleActiveResultSets=True;"
  },
  "Brevo": {
    "ApiKey": "YOUR_BREVO_API_KEY",
    "SenderEmail": "noreply@skinbloom-aesthetics.ch",
    "SenderName": "Skinbloom Aesthetics"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://skinbloom-aesthetics.ch",
      "https://www.skinbloom-aesthetics.ch"
    ]
  }
}
```

**Wichtig:** Füge `appsettings.Development.json` zu `.gitignore` hinzu!

### 3. Migration durchführen

```bash
cd BarberDario.Api

# EF Core Tools installieren (falls noch nicht installiert)
dotnet tool install --global dotnet-ef

# Datenbank aktualisieren
dotnet ef database update

# Services-Daten einfügen (siehe SkinbloomServices.sql)
# Führe das SQL-Script in deiner Datenbank aus
```

Siehe [BACKEND_MIGRATION_PLAN.md](BACKEND_MIGRATION_PLAN.md) für detaillierte Migrations-Anweisungen.

### 4. Projekt starten

```bash
dotnet run
```

Die API läuft nun auf:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## 📁 Projektstruktur

```
BarberDario.Api/
├── Controllers/           # API Controller
├── Data/
│   ├── Entities/         # Entity Models
│   └── BarberDarioDbContext.cs
├── Services/             # Business Logic Services
├── DTOs/                 # Data Transfer Objects
├── Validators/           # FluentValidation Validators
├── Models/               # View Models
├── Program.cs            # App Entry Point
└── appsettings.json      # Configuration
```

## 📊 Datenbank-Schema

### Tabellen

- **Services** - Dienstleistungen (Herrenschnitt, Bart, etc.)
- **Customers** - Kundendaten
- **Bookings** - Terminbuchungen
- **BusinessHours** - Öffnungszeiten
- **BlockedTimeSlots** - Gesperrte Zeiten (Urlaub, etc.)
- **EmailLogs** - Email-Versand-Protokoll
- **Settings** - System-Einstellungen

### Services

Die Datenbank enthält folgende Beauty-Behandlungen (siehe `SkinbloomServices.sql`):

**Hyaluron Behandlungen:**
- Hyaluron - Jawline (45 Min, ab CHF 249.-)
- Hyaluron - Kinn-Aufbau (45 Min, ab CHF 249.-)
- Hyaluron - Lippenunterspritzung (45 Min, ab CHF 249.-)
- Hyaluron - Nasolabialfalte (30 Min, ab CHF 249.-)

**PRP / Eigenbluttherapie:**
- PRP bei Haarausfall (60 Min, ab CHF 450.-)
- Vampire Lifting inkl. Maske (60 Min, ab CHF 450.-)

**HydraFacial:**
- HydraFacial Basic (30 Min, ab CHF 189.-)
- HydraFacial MD (45 Min, ab CHF 199.-)
- HydraFacial Deluxe (60 Min, ab CHF 229.-)

...und weitere Behandlungen wie Mesotherapie, Skinbooster, Profhilo, Radiofrequenz Microneedling, BioPeelX

## 🔌 API Endpunkte

Vollständige Dokumentation in [BOOKING_SYSTEM_PLAN.md](../BOOKING_SYSTEM_PLAN.md)

### Öffentliche Endpunkte

- `GET /api/services` - Alle Services abrufen
- `GET /api/availability/{serviceId}?date=2025-12-27` - Verfügbare Zeitslots
- `POST /api/bookings` - Neuen Termin buchen
- `GET /api/bookings/{id}` - Termin-Details
- `DELETE /api/bookings/{id}` - Termin stornieren
- `GET /api/customers/bookings?email={email}` - Kunden-Termine

### Admin Endpunkte (Authentifizierung erforderlich)

- `GET /api/admin/dashboard` - Dashboard-Daten
- `GET /api/admin/bookings` - Alle Termine
- `PATCH /api/bookings/{id}/confirm` - Termin bestätigen
- `PATCH /api/bookings/{id}/complete` - Als abgeschlossen markieren
- `POST /api/admin/blocked-slots` - Zeit sperren
- `GET /api/admin/statistics` - Statistiken

## 🧪 Testen

### Mit Swagger UI

1. Starte die API (`dotnet run`)
2. Öffne `https://localhost:5001/swagger`
3. Teste die Endpunkte direkt im Browser

### Mit HTTP-Datei

Die Datei `BarberDario.Api.http` enthält vorkonfigurierte Requests:

```http
### Get all services
GET https://localhost:5001/api/services

### Get availability
GET https://localhost:5001/api/availability/11111111-1111-1111-1111-111111111111?date=2025-12-28
```

## 🚢 Deployment

### Railway.app (Empfohlen)

1. Erstelle ein Railway-Account
2. Neues Projekt erstellen
3. PostgreSQL-Service hinzufügen
4. Verknüpfe GitHub-Repository
5. Setze Environment Variables:
   - `ConnectionStrings__DefaultConnection`
   - `Brevo__ApiKey`
   - `Brevo__SenderEmail`

### Fly.io

```bash
# Fly CLI installieren
brew install flyctl

# Projekt initialisieren
fly launch

# Deploy
fly deploy
```

## 📝 Nächste Schritte

- [x] Backend-Projekt aufsetzen
- [x] Entity Framework Models erstellen
- [x] DbContext konfigurieren
- [ ] Datenbank-Migration durchführen
- [ ] Erste API-Endpunkte implementieren
- [ ] Brevo Email-Integration
- [ ] Admin-Authentifizierung
- [ ] Background Jobs (Hangfire)
- [ ] Deployment

## 📚 Ressourcen

- [.NET Web API Docs](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Entity Framework Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [Brevo API Docs](https://developers.brevo.com/)
- [Supabase Docs](https://supabase.com/docs)

## 🤝 Support

Bei Fragen oder Problemen, siehe die [vollständige Dokumentation](../BOOKING_SYSTEM_PLAN.md).
