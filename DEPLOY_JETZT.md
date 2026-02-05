# Jetzt Deployen - Schritt für Schritt

## ✅ Was bereits fertig ist:

- ✅ SQL Server Datenbank erstellt (`db36529`)
- ✅ Connection String aktualisiert
- ✅ Code auf SQL Server migriert
- ✅ SQL Migrations Script generiert
- ✅ Backend gebaut und publisht

---

## 📝 Schritt 1: SQL Script in WebMSSQL ausführen

### 1.1 WebMSSQL öffnen
Gehe zu: **https://webmssql.monsterasp.net** (Link aus deinem Screenshot)

### 1.2 Mit Datenbank verbinden
- Server: `db36529.databaseasp.net`
- Database: `db36529`
- Login: `db36529`
- Password: `aZ+42!jARd5%`

### 1.3 SQL Script ausführen
1. Öffne die Datei: `BarberDario.Api/migrations_sql_server.sql`
2. Kopiere den **gesamten Inhalt**
3. Füge ihn in WebMSSQL ein
4. Klicke **"Execute"** oder **"Run"**

**Erwartetes Ergebnis:**
- 7 neue Tabellen werden erstellt:
  - BlockedTimeSlots
  - BusinessHours
  - Customers
  - Services
  - Settings
  - Bookings
  - EmailLogs

---

## 📤 Schritt 2: Backend zu MonsterASP hochladen

### 2.1 FileZilla öffnen

**Server-Details:**
- Host: `site48430.siteapp.net`
- Username: `site48430`
- Password: [dein MonsterASP Passwort]
- Port: 21 (FTP)

### 2.2 Alte Dateien löschen
1. Navigiere zu `/wwwroot`
2. **Lösche ALLE alten Dateien** in diesem Ordner

### 2.3 Neue Dateien hochladen
1. Auf deinem lokalen PC: Navigiere zu:
   ```
   /Users/berkcan/Dropbox/Mac (2)/Documents/Dario_Friseur Homepage/barberdario-api/BarberDario.Api/publish
   ```

2. **Wähle ALLE Dateien** im `publish` Ordner
3. Uploade sie in `/wwwroot` auf dem Server

**Wichtige Dateien, die hochgeladen werden:**
- ✅ `BarberDario.Api.dll`
- ✅ `web.config`
- ✅ `appsettings.Production.json`
- ✅ Alle anderen DLLs und Dateien

---

## 🔄 Schritt 3: Website in MonsterASP starten

1. Gehe zu: **https://admin.monsterasp.net**
2. Wähle deine Website (`barberdarioapi.runasp.net`)
3. Klicke **"Restart"** oder **"Start"**

---

## ✅ Schritt 4: API testen

### 4.1 Services abrufen
Öffne im Browser:
```
https://barberdarioapi.runasp.net/api/services
```

**Erwartetes Ergebnis:**
```json
[]
```
(Leeres Array ist OK, weil noch keine Services in der Datenbank sind)

### 4.2 Swagger testen (nur in Development)
Swagger ist aus Sicherheitsgründen nur lokal verfügbar.

### 4.3 Admin Dashboard testen
Im Frontend:
```
https://limktree-keinfriseur.vercel.app/admin/login
```
- Username: `admin`
- Password: `barber2025`

---

## 🚨 Falls Fehler auftreten:

### Fehler: "Cannot open database"
**Lösung:** SQL Script wurde nicht ausgeführt
→ Gehe zurück zu Schritt 1

### Fehler: 500.30 - App failed to start
**Lösung:**
1. Überprüfe, ob `web.config` hochgeladen wurde
2. Überprüfe, ob `BarberDario.Api.dll` in `/wwwroot` liegt
3. Checke MonsterASP Logs: Dashboard → Logs → stdout

### Fehler: "Login failed for user"
**Lösung:** Connection String ist falsch
→ Überprüfe `appsettings.Production.json`:
```json
"DefaultConnection": "Server=db36529.databaseasp.net; Database=db36529; User Id=db36529; Password=aZ+42!jARd5%; Encrypt=False; MultipleActiveResultSets=True;"
```

### API gibt 404 zurück
**Lösung:**
1. Stelle sicher, dass ALLE Dateien aus `publish` hochgeladen wurden
2. Restart Website in MonsterASP

---

## 📊 Hangfire aktivieren (Optional - später)

Hangfire ist aktuell deaktiviert. Um es zu aktivieren:

1. Öffne `Program.cs`
2. Entferne die Kommentare (`//`) von:
   - Zeilen 36-40 (Hangfire Service)
   - Zeile 42 (HangfireServer)
   - Zeilen 62-64 (Hangfire Dashboard)
   - Zeilen 79-88 (Recurring Jobs)

3. Neu builden:
   ```bash
   dotnet publish -c Release -o ./publish --self-contained false
   ```

4. Neu hochladen

---

## 📝 Nach erfolgreichem Deployment:

### Frontend API URL aktualisieren

In `limktree_keinfriseur/lib/api/client.ts`:
```typescript
const API_BASE_URL = "https://barberdarioapi.runasp.net";
```

Dann:
```bash
git add .
git commit -m "Update API URL to MonsterASP"
git push
```
→ Vercel deployed automatisch

---

## 🎉 Deployment Checklist

- [ ] SQL Script in WebMSSQL ausgeführt
- [ ] Backend-Dateien via FileZilla hochgeladen
- [ ] Website in MonsterASP gestartet
- [ ] API getestet (`/api/services` gibt 200 zurück)
- [ ] Frontend API URL aktualisiert
- [ ] Admin Login getestet
- [ ] Erste Buchung getestet

**Viel Erfolg!** 🚀

Bei Problemen: Schicke mir einen Screenshot vom Fehler + MonsterASP Logs!
