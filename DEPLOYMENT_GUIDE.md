# Skinbloom API Deployment zu MonsterASP

## ✅ Vorbereitung (Fertig)

- ✅ API gebaut für Windows (10MB ZIP-Datei)
- ✅ Datenbank eingerichtet mit 22 Services
- ✅ Connection String konfiguriert
- ✅ web.config für IIS erstellt

---

## Schritt 1: ZIP hochladen zu MonsterASP

### Option A: File Manager (Empfohlen)

1. Logge dich ins **MonsterASP Control Panel** ein
2. Gehe zu **File Manager** oder **FTP Manager**
3. Navigiere zu deinem Web-Verzeichnis:
   - Typischerweise: `/httpdocs/` oder `/wwwroot/`
   - Erstelle einen Ordner: `/httpdocs/api/` oder `/api/`

4. **Upload der ZIP-Datei:**
   - Lade hoch: `skinbloom-api/BarberDario.Api/skinbloom-api.zip`
   - Entpacke die ZIP direkt im `/api/` Ordner

5. **Struktur sollte sein:**
   ```
   /httpdocs/api/
   ├── BarberDario.Api.dll
   ├── BarberDario.Api.exe
   ├── web.config
   ├── appsettings.json
   ├── appsettings.Production.json
   └── ... (alle anderen DLLs)
   ```

### Option B: FTP (FileZilla/WinSCP)

1. Öffne **FileZilla** oder **WinSCP**
2. Verbinde mit MonsterASP FTP:
   - Host: (siehe MonsterASP Control Panel)
   - User: (dein FTP-User)
   - Password: (dein FTP-Password)
   - Port: 21

3. Navigiere zu `/httpdocs/api/`
4. Upload alle Dateien aus `skinbloom-api/BarberDario.Api/publish/`

---

## Schritt 2: .NET Runtime installieren (falls nicht vorhanden)

Prüfe im MonsterASP Control Panel:
1. **Server-Einstellungen** → **ASP.NET** oder **Application Pools**
2. Stelle sicher, dass **.NET 8.0 Runtime** installiert ist

Falls nicht installiert:
- Kontaktiere MonsterASP Support
- Oder verwende **selbst-enthaltenes Deployment** (größere Dateigröße)

---

## Schritt 3: IIS/Web-Anwendung konfigurieren

### Im MonsterASP Control Panel:

1. **Websites** → Deine Domain auswählen

2. **Neue Anwendung/Verzeichnis erstellen:**
   - Name: `api`
   - Pfad: `/api/`
   - Application Pool: **.NET Core** oder **No Managed Code**

3. **Alternative: Subdomain erstellen**
   - Gehe zu **Domains** → **Subdomains**
   - Erstelle: `api.skinbloom-aesthetics.ch`
   - Document Root: `/api/`
   - SSL: **Let's Encrypt** aktivieren

4. **Permissions prüfen:**
   - IIS User (`IIS AppPool\YourAppPool`) braucht Read/Write Rechte
   - Logs-Ordner muss schreibbar sein

---

## Schritt 4: appsettings.Production.json verifizieren

Stelle sicher, dass diese Datei im `/api/` Ordner ist mit korrekten Werten:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db40004.databaseasp.net,1433; Database=db40004; User Id=db40004; Password=Ht3!%2MiFs9_; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;"
  },
  "Brevo": {
    "ApiKey": "xsmtpsib-01490a4d7efebb2bdd5a678c8c17d2f0363c3cfab8682a53bb02dc23fb8081fa-o2b9BGYJbaFKuO0F"
  },
  "Email": {
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

## Schritt 5: API testen

### Test 1: Health Check

Öffne im Browser:
```
https://yourdomain.com/api/
```

oder

```
https://api.skinbloom-aesthetics.ch/
```

**Erwartetes Ergebnis:**
- Swagger UI wird angezeigt
- Oder: API Dokumentation Seite
- Oder: HTTP 200 Status

### Test 2: Services abrufen

```bash
curl https://yourdomain.com/api/api/services

# Oder im Browser:
https://yourdomain.com/api/api/services
```

**Erwartetes Ergebnis:**
```json
[
  {
    "id": "...",
    "name": "Kostenloses Beratungsgespräch",
    "price": 0.00,
    "durationMinutes": 30
  },
  {
    "name": "Hyaluron - Jawline",
    "price": 249.00,
    "durationMinutes": 45
  },
  ...
]
```

**Sollte 22 Services zurückgeben**

### Test 3: Swagger UI

```
https://yourdomain.com/api/swagger
```

Hier kannst du alle Endpunkte interaktiv testen.

---

## Schritt 6: Frontend verbinden

### Update .env.local im Frontend:

```bash
cd gentlelink-skinbloom

# Erstelle/Update .env.local
echo "NEXT_PUBLIC_API_URL=https://api.skinbloom-aesthetics.ch/api" > .env.local
echo "NEXT_PUBLIC_CLARITY_PROJECT_ID=vbnguu902y" >> .env.local
```

### Deploy zu Vercel:

```bash
vercel --prod

# Environment Variables im Vercel Dashboard setzen:
# - NEXT_PUBLIC_API_URL=https://api.skinbloom-aesthetics.ch/api
# - NEXT_PUBLIC_CLARITY_PROJECT_ID=vbnguu902y
```

---

## Troubleshooting

### Problem 1: API lädt nicht / 500 Error

**Logs prüfen:**
```
/api/logs/stdout_*.log
```

**Häufige Ursachen:**
- .NET 8 Runtime nicht installiert
- Fehlende Permissions
- Falsche web.config
- Connection String falsch

**Lösung:**
```xml
<!-- web.config aktualisieren -->
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

### Problem 2: 404 Not Found

**Ursache:** Application nicht korrekt in IIS konfiguriert

**Lösung:**
- Prüfe ob `/api/` als Anwendung eingerichtet ist
- Application Pool sollte "No Managed Code" sein

### Problem 3: CORS Fehler

**Symptom:** Frontend kann nicht auf API zugreifen

**Lösung:**
```json
// appsettings.Production.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://skinbloom-aesthetics.ch",
      "https://www.skinbloom-aesthetics.ch",
      "https://your-vercel-domain.vercel.app"
    ]
  }
}
```

### Problem 4: Datenbank-Verbindung fehlschlägt

**Symptom:** API startet, aber Services-Endpunkt gibt Fehler

**Lösung:**
- Prüfe Connection String in `appsettings.Production.json`
- Teste Verbindung mit WebMSSQL
- Server muss `db40004.databaseasp.net,1433` sein

---

## Zusammenfassung - Quick Deployment

```bash
# 1. ZIP bereits erstellt ✅
# Pfad: skinbloom-api/BarberDario.Api/skinbloom-api.zip (10MB)

# 2. MonsterASP Control Panel:
#    - Upload ZIP zu /httpdocs/api/
#    - Entpacken
#    - IIS Anwendung erstellen
#    - .NET 8 Runtime prüfen

# 3. Subdomain (optional):
#    - api.skinbloom-aesthetics.ch → /api/
#    - SSL aktivieren (Let's Encrypt)

# 4. Test:
#    https://api.skinbloom-aesthetics.ch/api/services
#    → Sollte 22 Services zurückgeben

# 5. Frontend verbinden:
cd gentlelink-skinbloom
echo "NEXT_PUBLIC_API_URL=https://api.skinbloom-aesthetics.ch/api" > .env.local
vercel --prod
```

---

## Nächste Schritte nach Deployment

1. ✅ API deployed und läuft
2. ✅ Services-Endpunkt gibt 22 Services zurück
3. → Frontend deployen zu Vercel
4. → End-to-End Buchungsprozess testen
5. → Brevo Email-Versand testen
6. → Monitoring/Logging einrichten

---

## Support

**MonsterASP Support:**
- Für .NET Runtime Installation
- Für IIS Konfiguration
- Für SSL-Zertifikate

**Dokumentation:**
- Backend: [README.md](README.md)
- Services: [SkinbloomServices_Simple.sql](SkinbloomServices_Simple.sql)
- Troubleshooting: [DB_CONNECTION_TROUBLESHOOTING.md](DB_CONNECTION_TROUBLESHOOTING.md)
