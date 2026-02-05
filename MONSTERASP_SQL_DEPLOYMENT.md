# MonsterASP SQL Server Deployment Guide

## Migration Abgeschlossen! ✅

Die API wurde erfolgreich von PostgreSQL auf MS SQL Server migriert:

- ✅ NuGet Packages aktualisiert (SQL Server statt PostgreSQL)
- ✅ DbContext auf SQL Server umgestellt
- ✅ Hangfire auf SQL Server vorbereitet
- ✅ Alte PostgreSQL Migrations gelöscht
- ✅ Neue SQL Server Migrations generiert
- ✅ Connection String Template erstellt

## Nächste Schritte

### 1. SQL Server Datenbank in MonsterASP erstellen

1. Gehe zu **https://admin.monsterasp.net**
2. Navigiere zu **"Databases"** oder **"MS SQL"** im Menü
3. Klicke auf **"Create Database"** oder **"Add Database"**
4. Wähle einen Datenbanknamen (z.B. `barberdario_db`)
5. **Notiere dir die folgenden Credentials**:
   - **Server/Host** (z.B. `sql.site4you.com` oder ähnlich)
   - **Database Name** (z.B. `barberdario_db`)
   - **Username** (z.B. `site48430_barberdario`)
   - **Password** (das Passwort, das du festlegst)
   - **Port** (normalerweise `1433`)

### 2. Connection String aktualisieren

Öffne die Datei:
```
BarberDario.Api/appsettings.Production.json
```

Ersetze diese Zeile:
```json
"DefaultConnection": "Server=YOUR_MONSTERASP_SQL_SERVER;Database=YOUR_DATABASE_NAME;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;"
```

Mit deinen echten Werten:
```json
"DefaultConnection": "Server=dein-server.site4you.com;Database=barberdario_db;User Id=site48430_barberdario;Password=dein-passwort;TrustServerCertificate=True;Encrypt=True;"
```

**Beispiel mit echten Werten:**
```json
"DefaultConnection": "Server=sql.site4you.com;Database=site48430_barberdario;User Id=site48430;Password=MeinSicheresPasswort123!;TrustServerCertificate=True;Encrypt=True;"
```

### 3. Build und Publish

Führe in PowerShell/Terminal aus:

```bash
cd "C:\Pfad\zu\barberdario-api\BarberDario.Api"
dotnet publish -c Release -o ./publish --self-contained false
```

Dies erstellt einen `publish` Ordner mit allen Dateien.

### 4. Upload zu MonsterASP

**Option A: FileZilla (FTP/SFTP)**

1. Öffne FileZilla
2. Verbinde mit:
   - Host: `site48430.siteapp.net`
   - Username: `site48430`
   - Password: [dein MonsterASP Passwort]
   - Port: 21 (FTP) oder 22 (SFTP)

3. Navigiere zu `/wwwroot` auf dem Server
4. **Lösche alle alten Dateien** in `/wwwroot`
5. Lade **alle Dateien** aus dem `publish` Ordner hoch

**Option B: Web Deploy**

Falls MonsterASP Web Deploy unterstützt:
```bash
dotnet publish -c Release /p:PublishProfile=MonsterASP
```

### 5. Datenbank Migrations anwenden

Es gibt zwei Möglichkeiten:

**Option A: Automatisch beim ersten Start (Empfohlen für Production)**

Die Migrations werden automatisch beim ersten API-Start angewendet, weil in `Program.cs` bereits vorhanden:

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BarberDarioDbContext>();
    await db.Database.MigrateAsync();
}
```

**ABER**: Dies funktioniert nur in Development-Modus. Für Production musst du die Migrations manuell anwenden.

**Option B: Manuell mit dotnet ef (Empfohlen)**

Auf deinem lokalen PC, **nachdem du die Connection String aktualisiert hast**:

```bash
cd "C:\Pfad\zu\barberdario-api\BarberDario.Api"

# Setze Environment auf Production
$env:ASPNETCORE_ENVIRONMENT="Production"

# Wende Migrations an
dotnet ef database update --configuration Release

# Zurücksetzen
$env:ASPNETCORE_ENVIRONMENT=""
```

**Option C: SQL Script generieren und manuell ausführen**

Falls die Connection von deinem PC nicht funktioniert:

```bash
# Generiere SQL Script
dotnet ef migrations script -o migrations.sql

# Öffne migrations.sql und führe es in MonsterASP SQL Management aus
```

### 6. Hangfire wieder aktivieren

Nachdem die Datenbank funktioniert, öffne `Program.cs` und entferne die Kommentare:

**Zeilen 37-43:**
```csharp
// Entferne die // am Anfang jeder Zeile
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
```

**Zeilen 75-80 (Hangfire Dashboard):**
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
```

**Zeilen 92-103 (Recurring Jobs):**
```csharp
using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobs.AddOrUpdate<BarberDario.Api.Services.ReminderService>(
        "send-daily-reminders",
        service => service.SendDailyRemindersAsync(),
        Cron.Daily(9)
    );
}
```

Dann:
```bash
# Neu builden und deployen
dotnet publish -c Release -o ./publish --self-contained false
# Upload zu MonsterASP
```

### 7. API testen

Teste deine API Endpoints:

1. **Services abrufen:**
   ```
   https://barberdarioapi.runasp.net/api/services
   ```

2. **Verfügbarkeit prüfen:**
   ```
   POST https://barberdarioapi.runasp.net/api/availability/check
   Body: { "date": "2025-01-15", "serviceId": 1 }
   ```

3. **Admin Dashboard:**
   ```
   GET https://barberdarioapi.runasp.net/api/admin/bookings?startDate=2025-01-01&endDate=2025-12-31
   ```

4. **Hangfire Dashboard** (wenn wieder aktiviert):
   ```
   https://barberdarioapi.runasp.net/hangfire
   ```
   (Nur in Development-Modus sichtbar)

### 8. Frontend Connection String aktualisieren

Vergiss nicht, im Frontend (`limktree_keinfriseur`) die API URL zu aktualisieren, falls noch nicht geschehen.

In `lib/api/client.ts`:
```typescript
const API_BASE_URL = "https://barberdarioapi.runasp.net";
```

## Troubleshooting

### Fehler: "Cannot open database"

- ❌ Connection String ist falsch
- ✅ Überprüfe Server, Database Name, Username, Password
- ✅ Stelle sicher, dass die Datenbank in MonsterASP erstellt wurde

### Fehler: "Login failed for user"

- ❌ Username oder Password ist falsch
- ✅ Überprüfe die Credentials in MonsterASP Dashboard
- ✅ Stelle sicher, dass der User Zugriff auf die Datenbank hat

### Fehler: "A network-related or instance-specific error"

- ❌ Server-Adresse ist falsch
- ❌ Firewall blockiert die Verbindung
- ✅ Überprüfe die Server-Adresse (z.B. `sql.site4you.com`)
- ✅ Stelle sicher, dass Port 1433 geöffnet ist

### 500.30 ANCM In-Process Start Failure

- ❌ .NET Runtime fehlt
- ❌ web.config ist fehlerhaft
- ✅ Stelle sicher, dass `web.config` vorhanden ist
- ✅ Überprüfe die Logs in `/logs/stdout` auf dem Server

## Wichtige Hinweise

1. **Connection String niemals committen!**
   - Verwende für Entwicklung `appsettings.Development.json`
   - Für Production verwende Environment Variables oder Azure Key Vault (falls MonsterASP unterstützt)

2. **CORS Konfiguration**
   - Die API erlaubt bereits `https://limktree-keinfriseur.vercel.app`
   - Stelle sicher, dass dies die korrekte Frontend-URL ist

3. **Email Konfiguration**
   - Die Brevo SMTP Konfiguration ist bereits in `appsettings.Production.json`
   - Überprüfe, ob die Credentials noch gültig sind

4. **Backup**
   - Sichere regelmäßig deine Datenbank in MonsterASP
   - Verwende `dotnet ef migrations script` um SQL Backups zu erstellen

## Zusammenfassung

**Was wurde bereits gemacht:**
✅ Code auf SQL Server migriert
✅ NuGet Packages aktualisiert
✅ Migrations generiert
✅ web.config erstellt
✅ Connection String Template vorbereitet

**Was du noch tun musst:**
1. ⏳ SQL Server Datenbank in MonsterASP erstellen
2. ⏳ Connection String mit echten Werten aktualisieren
3. ⏳ Build und Publish ausführen
4. ⏳ Zu MonsterASP uploaden
5. ⏳ Migrations auf die Datenbank anwenden
6. ⏳ Hangfire wieder aktivieren
7. ⏳ API testen

Bei Problemen oder Fragen, sag mir Bescheid!
