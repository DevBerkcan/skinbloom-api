# SQL Server Connection Troubleshooting

## Problem
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

## Aktualisierter Connection String

Ich habe den Connection String bereits optimiert für .NET 8:

```
Server=db40004.databaseasp.net,1433;
Database=db40004;
User Id=db40004;
Password=Ht3!%2MiFs9_;
Encrypt=True;
TrustServerCertificate=True;
MultipleActiveResultSets=True;
Connection Timeout=30;
```

**Änderungen:**
- ✅ Expliziter Port `,1433` hinzugefügt
- ✅ `Encrypt=True` + `TrustServerCertificate=True` für .NET 8
- ✅ `Connection Timeout=30` für längeren Timeout

---

## Lösungsschritte

### 1. Migration erneut versuchen

```bash
cd skinbloom-api/BarberDario.Api
dotnet ef database update
```

### 2. Wenn weiterhin Fehler: Server-Adresse prüfen

MonsterASP verwendet manchmal verschiedene Server-Formate:

**Option A:** Mit Port
```
Server=db40004.databaseasp.net,1433
```

**Option B:** Ohne Port
```
Server=db40004.databaseasp.net
```

**Option C:** Mit SQL Server Instance
```
Server=db40004.databaseasp.net\SQLEXPRESS
```

**Option D:** Mit tcp: Präfix
```
Server=tcp:db40004.databaseasp.net,1433
```

### 3. MonsterASP Control Panel prüfen

1. Logge dich in MonsterASP Control Panel ein
2. Gehe zu "Databases" → "SQL Server"
3. Prüfe:
   - ✅ Ist die Datenbank aktiv?
   - ✅ Server-Adresse korrekt?
   - ✅ Remote Access aktiviert?
   - ✅ Gibt es spezielle Connection String Vorgaben?

### 4. Firewall-Test

Teste ob Port 1433 erreichbar ist:

```bash
# macOS/Linux
nc -zv db40004.databaseasp.net 1433

# oder mit telnet
telnet db40004.databaseasp.net 1433
```

**Erwartetes Ergebnis:**
- ✅ "Connection succeeded" = Server erreichbar
- ❌ "Connection refused" oder Timeout = Firewall blockiert

### 5. Alternative: Local Development

Falls Remote-Verbindung nicht funktioniert, temporär lokal entwickeln:

#### Option A: SQL Server LocalDB (Windows)
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb; Database=SkinbloomDev; Trusted_Connection=true; MultipleActiveResultSets=true"
```

#### Option B: Docker SQL Server (macOS/Linux)
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest

# Connection String
Server=localhost,1433;Database=SkinbloomDev;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
```

### 6. Test mit SQL Tools

Teste Verbindung zuerst mit GUI-Tools:

**Azure Data Studio** (kostenlos, cross-platform):
```bash
# Download von: https://aka.ms/azuredatastudio

# Verbindung testen:
Server: db40004.databaseasp.net
Authentication: SQL Login
Username: db40004
Password: Ht3!%2MiFs9_
Database: db40004
```

**SQL Server Management Studio** (Windows only)

---

## Nächste Schritte nach erfolgreicher Verbindung

### 1. Datenbank-Schema erstellen
```bash
cd skinbloom-api/BarberDario.Api
dotnet ef database update
```

### 2. Services-Daten einfügen

Führe [SkinbloomServices.sql](SkinbloomServices.sql) in der Datenbank aus:

**Option A: Via Azure Data Studio**
1. Öffne SkinbloomServices.sql
2. Verbinde mit db40004.databaseasp.net
3. Execute Query

**Option B: Via sqlcmd (Command Line)**
```bash
sqlcmd -S db40004.databaseasp.net,1433 -U db40004 -P "Ht3!%2MiFs9_" -d db40004 -i SkinbloomServices.sql
```

### 3. Verifizieren
```sql
SELECT COUNT(*) AS TotalServices FROM Services;
-- Erwartetes Ergebnis: 25

SELECT * FROM Services ORDER BY DisplayOrder;
```

### 4. API lokal testen
```bash
dotnet run

# In anderem Terminal:
curl http://localhost:5000/api/services
```

---

## Häufige MonsterASP Probleme

### Problem 1: IP-Whitelist
Manche Hosting-Provider erlauben nur Verbindungen von bestimmten IPs.

**Lösung:**
- Im MonsterASP Control Panel → "Remote Access" → Deine öffentliche IP hinzufügen

### Problem 2: SSL/TLS Versionen
Ältere SQL Server akzeptieren nur bestimmte TLS Versionen.

**Lösung:**
```
Server=...;Encrypt=False;TrustServerCertificate=False
```

### Problem 3: Datenbank nicht erstellt
Die Datenbank muss im Control Panel manuell erstellt werden.

**Lösung:**
1. MonsterASP → "Create Database"
2. Notiere genauen Datenbanknamen (case-sensitive!)

---

## Kontakt & Support

Falls nichts funktioniert:
1. MonsterASP Support kontaktieren
2. Screenshot von Connection Error senden
3. Nach exaktem Connection String Format fragen

**MonsterASP SQL Server Dokumentation:**
- Support Portal checken für aktuellste Connection String Beispiele
