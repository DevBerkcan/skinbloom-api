# Test-Endpoints für Email & Dummy-Daten

## ✅ Was ich gemacht habe:

1. **Test-Controller erstellt** mit 3 Endpoints:
   - `/api/test/seed-today` - Erstellt Dummy-Buchungen für heute
   - `/api/test/send-test-email` - Sendet Test-Email
   - `/api/test/clear-test-bookings` - Löscht Test-Buchungen

2. **Frontend gefixt** - Timeline-Seite funktioniert jetzt
3. **Beide deployed** - Backend gebaut, Frontend gepusht

---

## 📝 Jetzt musst du:

### 1. Backend via FileZilla hochladen

1. Öffne **FileZilla**
2. Verbinde zu `site48430.siteapp.net`
3. Navigiere zu `/wwwroot`
4. **Lösche alle alten Dateien**
5. Lade alle Dateien aus `publish/` Ordner hoch:
   ```
   /Users/berkcan/Dropbox/Mac (2)/Documents/Dario_Friseur Homepage/barberdario-api/BarberDario.Api/publish
   ```

6. Gehe zu **MonsterASP** → **Restart** Website

### 2. Warte auf Vercel Deployment (2-3 Minuten)

Checke https://vercel.com/dashboard → Deployments

---

## 🧪 Dann teste die Endpoints:

### Test 1: Dummy-Daten für heute erstellen

Öffne im Browser oder Postman:

**POST** `https://barberdarioapi.runasp.net/api/test/seed-today`

Das erstellt 7 Buchungen für heute:
- 09:00 - 09:30
- 10:00 - 10:30
- 11:30 - 12:00
- 14:00 - 14:30
- 15:30 - 16:00
- 17:00 - 17:30
- 18:30 - 19:00

**Erwartete Antwort:**
```json
{
  "message": "Created 7 test bookings for today",
  "date": "2025-12-27",
  "bookings": [...]
}
```

### Test 2: Timeline anschauen

1. Gehe zu https://limktree-keinfriseur.vercel.app/admin/login
2. Login: `admin` / `barber2025`
3. Klicke auf **"Heute Timeline"** (sollte jetzt im Menü sein!)
4. Du solltest sehen:
   - ✅ 7 Termine von 8-20 Uhr
   - ✅ Rote Linie bei aktueller Uhrzeit
   - ✅ Aktueller Termin rot markiert
   - ✅ Alle Kundendaten

### Test 3: Email testen

**Postman oder curl:**

```bash
curl -X POST https://barberdarioapi.runasp.net/api/test/send-test-email \
  -H "Content-Type: application/json" \
  -d '{
    "email": "deine-email@example.com",
    "firstName": "Test",
    "lastName": "User"
  }'
```

**Oder im Browser (JSON direkt in Body):**

POST: `https://barberdarioapi.runasp.net/api/test/send-test-email`

Body:
```json
{
  "email": "berkcan98@live.de",
  "firstName": "Berk",
  "lastName": "Ates"
}
```

**Erwartete Antwort (Erfolg):**
```json
{
  "message": "Test email sent successfully",
  "recipient": "berkcan98@live.de",
  "type": "Booking Confirmation"
}
```

**Erwartete Antwort (Fehler - wenn SMTP blockiert):**
```json
{
  "message": "Failed to send email",
  "error": "Unable to connect to SMTP server...",
  "innerError": "..."
}
```

**Checke:**
1. Dein Email-Postfach (inkl. Spam!)
2. Brevo Dashboard: https://app.brevo.com/logs
3. MonsterASP Logs: Dashboard → Logs → Suche nach "Email" oder "SMTP"

### Test 4: Test-Daten löschen

Wenn du fertig bist:

**DELETE** `https://barberdarioapi.runasp.net/api/test/clear-test-bookings`

Das löscht alle Test-Buchungen.

---

## 🚨 Troubleshooting

### Timeline zeigt "Heute Timeline" Button nicht
- ❌ Vercel Deployment läuft noch
- ✅ Warte 2-3 Minuten und lade Seite neu (Strg+Shift+R)

### Email-Test gibt Fehler
**Mögliche Ursachen:**
1. **Port 587 blockiert** (MonsterASP)
   - Kontaktiere MonsterASP Support
   - Oder verwende alternativen SMTP-Provider

2. **Brevo Credentials falsch**
   - Checke `appsettings.Production.json`

3. **Daily Limit erreicht**
   - Checke Brevo Dashboard

### Dummy-Daten werden nicht angezeigt
- Backend nicht hochgeladen?
- Website nicht neu gestartet?
- Checke MonsterASP Logs

---

## ✅ Erwartetes Ergebnis:

Nach allen Tests solltest du haben:
- ✅ Timeline-Seite mit "Heute Timeline" Button
- ✅ 7 Test-Termine sichtbar in Timeline
- ✅ Rote Linie zeigt aktuelle Uhrzeit
- ✅ Email-Test zeigt ob SMTP funktioniert
- ⚠️ Email kommt an (wenn SMTP nicht blockiert)

**Schicke mir Screenshots von:**
1. Timeline mit den Terminen
2. Email-Test Response
3. Email im Postfach (falls angekommen)

Dann können wir weitermachen! 🚀
