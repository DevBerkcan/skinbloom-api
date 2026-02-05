# ✅ Datenbank ist fertig! Jetzt nur noch hochladen

## 📤 Schritt 1: FileZilla öffnen

Falls nicht installiert: https://filezilla-project.org

## 📤 Schritt 2: Zu MonsterASP verbinden

**Server-Verbindung:**
- Host: `site48430.siteapp.net`
- Benutzername: `site48430`
- Passwort: [Dein MonsterASP Passwort]
- Port: `21` (oder leer lassen für Standard)

Klicke **"Verbinden"**

## 🗑️ Schritt 3: Alte Dateien löschen

1. Im **rechten Fenster** (Server) navigiere zu: `/wwwroot`
2. Wähle **ALLE Dateien** in diesem Ordner aus
3. **Rechtsklick → Löschen**

## 📤 Schritt 4: Neue Dateien hochladen

1. Im **linken Fenster** (Dein Computer) navigiere zu:
   ```
   /Users/berkcan/Dropbox/Mac (2)/Documents/Dario_Friseur Homepage/barberdario-api/BarberDario.Api/publish
   ```

2. Wähle **ALLE Dateien und Ordner** im `publish` Ordner aus

3. **Rechtsklick → Upload**
   (oder ziehe sie einfach nach rechts ins `/wwwroot` Verzeichnis)

4. **Warte**, bis alle Dateien hochgeladen sind (kann 2-5 Minuten dauern)

## 🔄 Schritt 5: Website neu starten

1. Gehe zu: https://admin.monsterasp.net
2. Logge dich ein
3. Finde deine Website in der Liste
4. Klicke auf **"Restart"** oder **"Start"**

## ✅ Schritt 6: Testen

Öffne im Browser:
```
https://barberdarioapi.runasp.net/api/services
```

**Erwartetes Ergebnis:**
```json
[]
```
(Ein leeres Array - das ist OK! Bedeutet die API funktioniert!)

---

## 🚨 Falls Fehler:

### FileZilla verbindet nicht
- Überprüfe Benutzername/Passwort
- Versuche Port 22 (SFTP) statt 21 (FTP)

### 500.30 Fehler beim API-Aufruf
- Stelle sicher, dass `BarberDario.Api.dll` in `/wwwroot` liegt
- Stelle sicher, dass `web.config` hochgeladen wurde
- Restart Website nochmal

### API gibt 404
- Alle Dateien hochgeladen?
- Im richtigen Ordner (`/wwwroot`)?
- Website neu gestartet?

---

**Bei Erfolg schicke mir einen Screenshot von:**
1. FileZilla nach dem Upload (zeigt die Dateien in `/wwwroot`)
2. Browser mit `https://barberdarioapi.runasp.net/api/services`

Dann können wir weitermachen! 🚀
