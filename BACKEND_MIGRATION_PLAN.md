# Backend Migration Plan: Barber Dario → Skinbloom Aesthetics

## Übersicht

Das Backend ist ein **.NET 8 ASP.NET Core Web API** das migriert werden muss von Barbershop zu Beauty Salon.

---

## 🗄️ Datenbank Migration

### 1. Neue Datenbank erstellen

**Option A: Neue SQL Server Datenbank (Empfohlen)**
- Neue DB auf MonsterASP oder anderem Hosting erstellen
- Connection String in `appsettings.json` aktualisieren
- Migrationen laufen lassen

**Option B: Bestehende DB leeren**
- Alle Tabellen droppen
- Migrationen neu laufen lassen
- Services neu anlegen

### 2. Connection String aktualisieren

`appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=NEUE_DB_SERVER; Database=NEUE_DB_NAME; User Id=USER; Password=PASSWORD; Encrypt=False; MultipleActiveResultSets=True;"
  }
}
```

### 3. Migrationen laufen lassen

```bash
# Im Backend-Verzeichnis
dotnet ef database update
```

---

## 📧 E-Mail Konfiguration (Brevo)

### appsettings.json aktualisieren:

```json
{
  "Brevo": {
    "ApiKey": "DEIN_BREVO_API_KEY",
    "SenderEmail": "noreply@skinbloom-aesthetics.ch",
    "SenderName": "Skinbloom Aesthetics"
  }
}
```

**Setup:**
1. Brevo Account erstellen (https://www.brevo.com/) - **Kostenlos** bis 300 E-Mails/Tag
2. API Key erstellen unter **Settings** > **API Keys**
3. Sender-Email verifizieren: `noreply@skinbloom-aesthetics.ch`

---

## 🌐 CORS Konfiguration

### appsettings.json aktualisieren:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://skinbloom-aesthetics.ch",
      "https://www.skinbloom-aesthetics.ch"
    ]
  }
}
```

---

## 💅 Services-Daten (Beauty Behandlungen)

Die Services müssen in der Datenbank angelegt werden.

### SQL Script für neue Services:

```sql
-- Alte Barbershop Services löschen
DELETE FROM Services;

-- Neue Beauty Services einfügen
-- HYALURON BEHANDLUNGEN
INSERT INTO Services (Id, Name, Description, DurationMinutes, Price, DisplayOrder, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'Hyaluron - Jawline', 'Konturierung der Kinnlinie mit Hyaluronsäure', 45, 249.00, 1, GETDATE(), GETDATE()),
(NEWID(), 'Hyaluron - Kinn-Aufbau', 'Kinn-Modellierung für harmonische Gesichtsproportionen', 45, 249.00, 2, GETDATE(), GETDATE()),
(NEWID(), 'Hyaluron - Lippenfalten', 'Glättung der Lippenfalten', 30, 249.00, 3, GETDATE(), GETDATE()),
(NEWID(), 'Hyaluron - Lippenunterspritzung', 'Volumenaufbau und Konturierung der Lippen', 45, 249.00, 4, GETDATE(), GETDATE()),
(NEWID(), 'Hyaluron - Marionettenfalte', 'Behandlung der Mundwinkelfalten', 30, 249.00, 5, GETDATE(), GETDATE()),
(NEWID(), 'Hyaluron - Wangenaufbau', 'Volumenaufbau im Wangenbereich', 45, 249.00, 6, GETDATE(), GETDATE()),
(NEWID(), 'Hyaluron - Nasolabialfalte', 'Glättung der Nasen-Mund-Falten', 30, 249.00, 7, GETDATE(), GETDATE()),

-- ANDERE BEHANDLUNGEN
(NEWID(), 'Hylase', 'Hyaluronidase zur Auflösung von Hyaluronsäure', 30, 150.00, 10, GETDATE(), GETDATE()),
(NEWID(), 'Mesotherapie', 'Microinjektionen für Hautverbesserung', 45, 199.00, 11, GETDATE(), GETDATE()),
(NEWID(), 'Skinbooster', 'Intensive Hautfeuchtigkeit und Straffung', 45, 199.00, 12, GETDATE(), GETDATE()),
(NEWID(), 'Profhilo', 'Bio-Remodelling für natürliche Hautverjüngung', 30, 349.00, 13, GETDATE(), GETDATE()),
(NEWID(), 'Fett-weg-Spritze', 'Lipolyse für lokale Fettreduktion', 30, 200.00, 14, GETDATE(), GETDATE()),

-- PRP / EIGENBLUT
(NEWID(), 'PRP bei Haarausfall / Eigenbluttherapie', 'Plättchenreiches Plasma gegen Haarausfall', 60, 450.00, 20, GETDATE(), GETDATE()),
(NEWID(), 'Vampire Lifting / PRP inkl. Maske 60 Min', 'PRP Gesichtsbehandlung mit revitalisierender Maske', 60, 450.00, 21, GETDATE(), GETDATE()),

-- INFUSION
(NEWID(), 'Infusionstherapie', 'Revitalisierende Vitamin-Infusion', 45, 129.00, 25, GETDATE(), GETDATE()),

-- RADIOFREQUENZ MICRONEEDLING
(NEWID(), 'Radiofrequenz Microneedling - Gesicht', 'RF Microneedling für Gesicht', 60, 299.00, 30, GETDATE(), GETDATE()),
(NEWID(), 'Radiofrequenz Microneedling - Gesicht, Hals, Dekolleté', 'RF Microneedling für Gesicht, Hals und Dekolleté', 90, 399.00, 31, GETDATE(), GETDATE()),

-- BIOPEEL
(NEWID(), 'BioPeelX Gesicht inkl. Maske 60 Min', 'Chemisches Peeling mit anschließender Maske', 60, 170.00, 35, GETDATE(), GETDATE()),

-- HYDRAFACIAL
(NEWID(), 'HydraFacial - Basic', 'Grundlegende HydraFacial Behandlung', 30, 189.00, 40, GETDATE(), GETDATE()),
(NEWID(), 'HydraFacial - MD', 'Erweiterte HydraFacial mit zusätzlichen Boostern', 45, 199.00, 41, GETDATE(), GETDATE()),
(NEWID(), 'HydraFacial - Deluxe', 'Premium HydraFacial mit allen Extras', 60, 229.00, 42, GETDATE(), GETDATE());
```

**Hinweis:** Passe die Preise und Beschreibungen nach Bedarf an!

---

## 🔄 Namespace & Projekt umbenennen (Optional)

### Option 1: Umbenennen (Sauber aber aufwändig)
```bash
# Projekt umbenennen
mv BarberDario.Api SkinbloomAesthetics.Api

# In allen .cs Dateien:
# namespace BarberDario.Api → namespace SkinbloomAesthetics.Api
```

### Option 2: Lassen wie es ist (Schneller)
- Funktioniert genauso gut
- Nur intern heißt es anders
- Nach außen sieht niemand den Code

**Empfehlung:** Erst mal lassen, später bei Bedarf umbenennen.

---

## 🚀 Deployment

### 1. Neue Datenbank auf MonsterASP

Basierend auf `MONSTERASP_SQL_DEPLOYMENT.md`:

1. Neue SQL Datenbank erstellen
2. Connection String in `appsettings.Production.json`
3. Services-Daten einfügen (SQL Script oben)

### 2. API Hosting

**Option A: MonsterASP Windows Hosting**
- ASP.NET Core 8 Support
- Bereits für BarberDario genutzt
- Neue Subdomain: `api.skinbloom-aesthetics.ch`

**Option B: Azure App Service**
- Kostenlos für kleine Apps (F1 Tier)
- Automatisches Deployment mit GitHub

**Option C: Railway / Render**
- Moderne Hosting-Plattformen
- Free Tier verfügbar

### 3. appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "PRODUCTION_DB_CONNECTION_STRING"
  },
  "Brevo": {
    "ApiKey": "PRODUCTION_BREVO_API_KEY",
    "SenderEmail": "noreply@skinbloom-aesthetics.ch",
    "SenderName": "Skinbloom Aesthetics"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://skinbloom-aesthetics.ch",
      "https://www.skinbloom-aesthetics.ch"
    ]
  }
}
```

---

## ✅ Checklist

### Datenbank:
- [ ] Neue SQL Server Datenbank erstellt
- [ ] Connection String in appsettings.json
- [ ] Migrationen gelaufen (`dotnet ef database update`)
- [ ] Services-Daten eingefügt (Beauty Behandlungen)
- [ ] Test-Buchung erstellt

### E-Mail:
- [ ] Brevo Account erstellt
- [ ] API Key generiert
- [ ] Sender-Email verifiziert
- [ ] In appsettings.json konfiguriert
- [ ] Test-E-Mail versendet

### API Konfiguration:
- [ ] CORS für neue Domain
- [ ] appsettings.Production.json aktualisiert
- [ ] Environment Variables gesetzt

### Hosting:
- [ ] API-Hosting ausgewählt
- [ ] Domain/Subdomain konfiguriert (z.B. api.skinbloom-aesthetics.ch)
- [ ] SSL Zertifikat eingerichtet
- [ ] API deployed

### Frontend-Integration:
- [ ] `.env.local` im Frontend mit neuer API-URL
- [ ] CORS funktioniert
- [ ] Services werden geladen
- [ ] Buchungsprozess getestet

---

## 🧪 Testen

### Lokales Testen:

```bash
# Backend starten
cd skinbloom-api/BarberDario.Api
dotnet run

# API sollte laufen auf: http://localhost:5067
```

**Test-Endpoints:**
- `GET http://localhost:5067/api/services` - Services abrufen
- `GET http://localhost:5067/api/availability/{serviceId}?date=2026-02-10` - Verfügbarkeit
- `POST http://localhost:5067/api/bookings` - Buchung erstellen

### Production Testen:
- Services-Endpoint funktioniert
- Buchung kann erstellt werden
- E-Mail wird versendet
- Admin-Panel funktioniert

---

## 📖 Nächste Schritte

1. **Neue Datenbank erstellen** auf MonsterASP oder anderem Provider
2. **Services-Daten** mit SQL Script einfügen
3. **Brevo Account** erstellen und konfigurieren
4. **API deployen** zu Production-Hosting
5. **Frontend** mit Production API-URL verbinden
6. **End-to-End Test** durchführen

---

## 💡 Empfehlung

**Reihenfolge:**
1. ✅ Frontend ist fertig (bereits erledigt!)
2. 🔄 Neue Datenbank + Services anlegen
3. 🔄 Brevo E-Mail konfigurieren
4. 🔄 API deployen
5. 🔄 Frontend mit Production API verbinden
6. ✅ Launch!

**Zeitaufwand:** 2-3 Stunden für komplettes Backend-Setup

---

Brauchst du Hilfe bei einem bestimmten Schritt?
