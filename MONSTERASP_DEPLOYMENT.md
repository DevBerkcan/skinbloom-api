# MonsterASP Datenbank Setup (Direkte Deployment)

Da die MonsterASP Datenbank nur von Websites auf dem Server erreichbar ist, führen wir die Migrations-Scripts direkt über WebMSSQL aus.

## Schritt 1: Datenbank-Tabellen erstellen

### 1.1 WebMSSQL öffnen

Öffne den Link aus deinem MonsterASP Control Panel:
```
https://webmssql.monsterasp.net
```

Oder klicke im Control Panel auf **"WebMSSQL"** → **"web interface"**

### 1.2 Login

- **Server**: `db40004.databaseasp.net`
- **Username**: `db40004`
- **Password**: `Ht3!%2MiFs9_`
- **Database**: `db40004`

### 1.3 Migrations-Script ausführen

1. Im WebMSSQL Interface: **"SQL"** → **"Execute SQL"**

2. Öffne die Datei: `skinbloom-api/migrations.sql`

3. Kopiere den gesamten Inhalt und füge ihn in das SQL-Fenster ein

4. Klicke auf **"Execute"** oder **"Run"**

**Erwartetes Ergebnis:**
```
Commands completed successfully.
Tables created: Services, Customers, Bookings, BusinessHours, BlockedTimeSlots, EmailLogs, Settings
```

### 1.4 Verifizieren

Führe aus:
```sql
SELECT name FROM sys.tables ORDER BY name;
```

**Erwartete Tabellen:**
- `__EFMigrationsHistory`
- `BlockedTimeSlots`
- `BookingServiceItems`
- `Bookings`
- `BusinessHours`
- `Customers`
- `EmailLogs`
- `ServiceCategories`
- `Services`
- `Settings`

---

## Schritt 2: Services-Daten einfügen

### 2.1 Services-Script ausführen

1. Öffne die Datei: `skinbloom-api/SkinbloomServices.sql`

2. Kopiere den gesamten Inhalt

3. Füge in WebMSSQL ein und führe aus

**Erwartetes Ergebnis:**
```
TotalServices
25

(25 rows affected)
```

### 2.2 Verifizieren

```sql
SELECT COUNT(*) AS Total FROM Services;
SELECT Name, Price, DurationMinutes FROM Services ORDER BY DisplayOrder;
```

**Erwartete Services:**
1. Kostenloses Beratungsgespräch (CHF 0.00)
2. Hyaluron - Jawline (CHF 249.00)
3. Hyaluron - Kinn-Aufbau (CHF 249.00)
... (25 Services insgesamt)

---

## Schritt 3: API auf MonsterASP deployen

### Option A: FTP Upload (Empfohlen)

#### 3.1 Projekt builden

```bash
cd skinbloom-api/BarberDario.Api

# Build für Windows Server
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

#### 3.2 FTP Upload

1. Öffne FileZilla oder WinSCP
2. Verbinde mit MonsterASP FTP
3. Upload den Ordner `publish/` nach `/httpdocs/api/` (oder `/api/`)

#### 3.3 Konfiguration

Stelle sicher, dass `appsettings.Production.json` die richtigen Werte hat:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db40004.databaseasp.net,1433; Database=db40004; User Id=db40004; Password=Ht3!%2MiFs9_; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;"
  }
}
```

### Option B: ZIP Upload via Control Panel

```bash
# Erstelle ZIP
cd skinbloom-api/BarberDario.Api/publish
zip -r skinbloom-api.zip .

# Upload via MonsterASP File Manager
```

---

## Schritt 4: API URL konfigurieren

### 4.1 Subdomain erstellen

Im MonsterASP Control Panel:

1. **Domains** → **Subdomains**
2. Erstelle Subdomain: `api.skinbloom-aesthetics.ch`
3. Document Root: `/api/` (wo die API deployed ist)

### 4.2 SSL Zertifikat

1. **SSL/TLS** → **Let's Encrypt**
2. Erstelle Zertifikat für `api.skinbloom-aesthetics.ch`

### 4.3 API Testen

```bash
curl https://api.skinbloom-aesthetics.ch/api/services

# Erwartete Ausgabe: JSON mit 25 Services
```

---

## Schritt 5: Frontend verbinden

### 5.1 Environment Variable aktualisieren

In `gentlelink-skinbloom/.env.local`:

```bash
NEXT_PUBLIC_API_URL=https://api.skinbloom-aesthetics.ch/api
NEXT_PUBLIC_CLARITY_PROJECT_ID=vbnguu902y
```

### 5.2 Frontend deployen

```bash
cd gentlelink-skinbloom

# Deploy zu Vercel
vercel --prod

# Setze Environment Variables in Vercel Dashboard:
# NEXT_PUBLIC_API_URL=https://api.skinbloom-aesthetics.ch/api
# NEXT_PUBLIC_CLARITY_PROJECT_ID=vbnguu902y
```

---

## Troubleshooting

### Problem: "Cannot find migrations.sql"

Falls das Script nicht generiert wurde:

```bash
cd skinbloom-api/BarberDario.Api
dotnet ef migrations script -o ../migrations.sql
```

### Problem: SQL Script Fehler

Führe Tabellen-Erstellung einzeln aus:

```sql
-- Prüfe ob Tabellen existieren
SELECT name FROM sys.tables;

-- Falls Fehler, lösche alte Tabellen und versuche erneut
DROP TABLE IF EXISTS Services;
DROP TABLE IF EXISTS Customers;
-- etc.
```

### Problem: Services werden nicht eingefügt

```sql
-- Prüfe ob Services-Tabelle existiert
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Services';

-- Falls existiert, lösche alte Daten
DELETE FROM Services;

-- Führe SkinbloomServices.sql erneut aus
```

---

## Alternative: Azure Data Studio (GUI)

Falls WebMSSQL nicht funktioniert:

1. Download Azure Data Studio: https://aka.ms/azuredatastudio

2. **WICHTIG**: Funktioniert nur, wenn du die API auf MonsterASP deployed hast und von dort aus auf die DB zugreifst

3. Connection String vom Screenshot verwenden

---

## Zusammenfassung - Quick Steps

```bash
# 1. Migrations-Script generiert (bereits erledigt ✅)
cd skinbloom-api/BarberDario.Api
dotnet ef migrations script -o ../migrations.sql

# 2. WebMSSQL öffnen
# https://webmssql.monsterasp.net

# 3. migrations.sql ausführen (erstellt Tabellen)
# 4. SkinbloomServices.sql ausführen (fügt 25 Services ein)

# 5. API builden und deployen
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

# 6. Upload zu MonsterASP via FTP

# 7. Frontend aktualisieren und deployen
cd gentlelink-skinbloom
# Update .env.local mit API URL
vercel --prod
```

---

## Wichtige Links

- **WebMSSQL**: https://webmssql.monsterasp.net
- **MonsterASP Control Panel**: Login über dein Dashboard
- **API Dokumentation**: Siehe `BACKEND_MIGRATION_PLAN.md`

---

## Status Check

Nach Abschluss solltest du haben:

- ✅ Datenbank `db40004` mit 10 Tabellen
- ✅ 25 Beauty-Services in der `Services` Tabelle
- ✅ API deployed auf `api.skinbloom-aesthetics.ch`
- ✅ Frontend deployed auf `skinbloom-aesthetics.ch`
- ✅ Buchungssystem funktioniert End-to-End
