# Docker SQL Server Setup (Lokale Entwicklung)

Da die MonsterASP Datenbank nicht von außen erreichbar ist, verwenden wir Docker für die lokale Entwicklung.

## Voraussetzungen

- Docker Desktop installiert ([Download](https://www.docker.com/products/docker-desktop))

## Setup-Schritte

### 1. SQL Server Container starten

```bash
cd skinbloom-api

# Container starten
docker-compose up -d

# Status prüfen (warte bis "healthy")
docker-compose ps
```

**Erwartete Ausgabe:**
```
NAME                  IMAGE                                        STATUS
skinbloom-sqlserver   mcr.microsoft.com/mssql/server:2022-latest   Up 10 seconds (healthy)
```

### 2. Datenbank-Migrationen ausführen

```bash
cd BarberDario.Api

# Migrationen anwenden (erstellt Tabellen)
dotnet ef database update
```

**Erwartete Ausgabe:**
```
Build succeeded.
Applying migration '20241201_Initial'.
Done.
```

### 3. Services-Daten einfügen

**Option A: Via Docker Exec**
```bash
docker exec -it skinbloom-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "Skinbloom2024!" \
  -d SkinbloomDev \
  -i /workspaces/SkinbloomServices.sql
```

**Option B: Manuell (einfacher)**
1. Installiere Azure Data Studio: https://aka.ms/azuredatastudio
2. Verbinde mit:
   - Server: `localhost`
   - Authentication: SQL Login
   - Username: `sa`
   - Password: `Skinbloom2024!`
   - Database: `SkinbloomDev`
3. Öffne `SkinbloomServices.sql` und führe es aus

**Option C: Via .NET CLI (schnellste Methode)**

Erstelle ein Migrations-Script das die Services automatisch einfügt:

```bash
cd BarberDario.Api

# Services via Seed-Data einfügen
dotnet run -- seed-services
```

### 4. API starten

```bash
cd BarberDario.Api

# API starten (Development-Modus)
dotnet run
```

Die API läuft nun auf:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`

### 5. Testen

```bash
# Services abrufen
curl http://localhost:5000/api/services

# Erwartete Ausgabe: JSON mit 25 Services
```

---

## Docker Commands

### Container Management

```bash
# Container starten
docker-compose up -d

# Container stoppen
docker-compose down

# Logs ansehen
docker-compose logs -f sqlserver

# Container neu starten
docker-compose restart

# Container Status prüfen
docker-compose ps
```

### Datenbank Management

```bash
# In SQL Server Shell
docker exec -it skinbloom-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Skinbloom2024!"

# Datenbanken auflisten
SELECT name FROM sys.databases;
GO

# Services zählen
USE SkinbloomDev;
SELECT COUNT(*) FROM Services;
GO

# Exit
exit
```

### Datenbank zurücksetzen

```bash
# Container + Volumes löschen (Datenbank wird gelöscht!)
docker-compose down -v

# Neu starten
docker-compose up -d

# Migrationen neu ausführen
cd BarberDario.Api
dotnet ef database update
```

---

## Connection Strings

### Development (Docker - localhost)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433; Database=SkinbloomDev; User Id=sa; Password=Skinbloom2024!; TrustServerCertificate=True; MultipleActiveResultSets=True;"
  }
}
```

### Production (MonsterASP - deployment only)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db40004.databaseasp.net,1433; Database=db40004; User Id=db40004; Password=Ht3!%2MiFs9_; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;"
  }
}
```

**Wichtig:**
- Development verwendet `appsettings.Development.json` (localhost Docker)
- Production verwendet `appsettings.Production.json` (MonsterASP)

---

## Troubleshooting

### Problem: Container startet nicht

```bash
# Logs prüfen
docker-compose logs sqlserver

# Häufigste Ursache: Port 1433 bereits belegt
lsof -i :1433

# Anderen Port verwenden (docker-compose.yml ändern)
ports:
  - "1434:1433"  # Host:Container
```

### Problem: Migration schlägt fehl

```bash
# Prüfe ob Container healthy ist
docker-compose ps

# Falls nicht healthy, warte länger oder starte neu
docker-compose restart
```

### Problem: Services werden nicht eingefügt

```bash
# Prüfe ob Tabelle existiert
docker exec -it skinbloom-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "Skinbloom2024!" \
  -Q "USE SkinbloomDev; SELECT name FROM sys.tables;"

# Falls "Services" existiert, füge Daten manuell ein
```

---

## Deployment zu Production

Wenn lokal alles funktioniert:

### 1. Backend auf MonsterASP deployen

Die API muss auf MonsterASP gehostet werden, damit sie die Datenbank erreichen kann:

```bash
# Projekt builden für Windows
dotnet publish -c Release -r win-x64 --self-contained

# Upload zu MonsterASP via FTP/File Manager
# Pfad: /BarberDario.Api/bin/Release/net8.0/win-x64/publish/
```

### 2. Production Datenbank initialisieren

Über MonsterASP-Server (Plesk/Control Panel):

1. Remote Desktop / SSH zur MonsterASP VM
2. Oder: Upload von Pre-Deployed DLLs und führe Migrationen remote aus

**Alternative:** Generiere SQL-Script aus Migrationen:

```bash
# Migrations-Script generieren
dotnet ef migrations script -o migration.sql

# Upload migration.sql zu MonsterASP
# Führe aus via SQL Server Management Studio oder Control Panel
```

### 3. Services einfügen

Upload `SkinbloomServices.sql` und führe aus über:
- MonsterASP Database Manager
- SQL Server Management Studio (remote)
- Azure Data Studio (remote)

---

## Zusammenfassung

**Lokale Entwicklung:**
1. `docker-compose up -d` - SQL Server starten
2. `dotnet ef database update` - Schema erstellen
3. Services manuell einfügen (Azure Data Studio)
4. `dotnet run` - API starten
5. Frontend verbinden mit `http://localhost:5000/api`

**Production:**
- API wird auf MonsterASP deployed
- Verwendet `appsettings.Production.json`
- Datenbank `db40004` auf `db40004.databaseasp.net`
- Frontend verbindet mit `https://api.skinbloom-aesthetics.ch`
