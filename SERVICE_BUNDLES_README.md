# Service Bundles / Packages System

## Übersicht

Das Service-Bundles-System ermöglicht die Erstellung von Behandlungspaketen mit Sonderpreisen. Kunden können mehrere Services als Bundle buchen und profitieren von attraktiven Rabatten.

## Features

✅ **Flexible Pakete**: Kombiniere beliebig viele Services
✅ **Automatische Preisberechnung**: Original- und Bundle-Preis, Rabatt in %
✅ **Mengenangaben**: Services können mehrfach im Paket enthalten sein (z.B. 2x Hyaluron)
✅ **Zeitliche Gültigkeit**: Optional ValidFrom/ValidUntil für saisonale Angebote
✅ **AGB**: Optionale Terms & Conditions pro Bundle
✅ **Buchungsintegration**: Bundles können wie normale Services gebucht werden

## Datenbank-Struktur

### ServiceBundles Tabelle

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| Id | UNIQUEIDENTIFIER | Primary Key |
| Name | NVARCHAR(200) | Bundle-Name |
| Description | NVARCHAR(1000) | Beschreibung |
| OriginalPrice | DECIMAL(10,2) | Summe aller Services (automatisch) |
| BundlePrice | DECIMAL(10,2) | Reduzierter Paketpreis |
| DiscountPercentage | DECIMAL(5,2) | Rabatt in % (automatisch) |
| TotalDurationMinutes | INT | Gesamtdauer (automatisch) |
| DisplayOrder | INT | Sortierreihenfolge |
| IsActive | BIT | Aktiv-Status |
| ValidFrom | DATETIME2 | Gültig ab (optional) |
| ValidUntil | DATETIME2 | Gültig bis (optional) |
| TermsAndConditions | NVARCHAR(2000) | AGB (optional) |
| CreatedAt | DATETIME2 | Erstellungsdatum |
| UpdatedAt | DATETIME2 | Letzte Aktualisierung |

### ServiceBundleItems Tabelle

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| Id | UNIQUEIDENTIFIER | Primary Key |
| BundleId | UNIQUEIDENTIFIER | FK zu ServiceBundles |
| ServiceId | UNIQUEIDENTIFIER | FK zu Services |
| Quantity | INT | Anzahl (z.B. 2 = 2x dieser Service) |
| DisplayOrder | INT | Reihenfolge im Bundle |
| Notes | NVARCHAR(500) | Optionale Notizen |

### Bookings Tabelle (Erweitert)

- **Neues Feld**: `BundleId UNIQUEIDENTIFIER` (Nullable)
- **Geändert**: `ServiceId UNIQUEIDENTIFIER` → jetzt Nullable
- **Check Constraint**: Entweder ServiceId ODER BundleId muss gesetzt sein (nicht beide, nicht keiner)

## Beispiel-Pakete

### Anti-Aging Komplettpaket
**Enthaltene Services**:
- Hyaluron - Jawline (CHF 249.00)
- Hyaluron - Nasolabialfalte (CHF 249.00)
- Botox - Stirn (CHF 199.00)

**Original-Preis**: CHF 697.00
**Bundle-Preis**: CHF 592.45 (15% Rabatt)
**Ersparnis**: CHF 104.55
**Dauer**: ~105 Minuten

### Beauty Starter Paket
**Enthaltene Services**:
- Kostenloses Beratungsgespräch (CHF 0.00)
- HydraFacial - Basic (CHF 189.00)
- Skinbooster (CHF 199.00)

**Original-Preis**: CHF 388.00
**Bundle-Preis**: CHF 329.80 (15% Rabatt)
**Ersparnis**: CHF 58.20
**Dauer**: ~105 Minuten

## API Endpoints

### GET /api/servicebundles
Alle aktiven Bundles abrufen

**Query Parameters**:
- `includeExpired` (bool): Standard `false` - nur gültige Bundles

**Response**:
```json
[
  {
    "id": "guid",
    "name": "Anti-Aging Komplettpaket",
    "description": "Perfekte Kombination...",
    "originalPrice": 697.00,
    "bundlePrice": 592.45,
    "discountPercentage": 15.00,
    "savings": 104.55,
    "totalDurationMinutes": 105,
    "displayOrder": 1,
    "validFrom": null,
    "validUntil": null,
    "isCurrentlyValid": true,
    "items": [
      {
        "serviceId": "guid",
        "serviceName": "Hyaluron - Jawline",
        "serviceDescription": "Konturierung...",
        "serviceDurationMinutes": 45,
        "servicePrice": 249.00,
        "quantity": 1,
        "displayOrder": 1,
        "notes": null
      }
    ]
  }
]
```

### GET /api/servicebundles/{id}
Einzelnes Bundle abrufen

### POST /api/servicebundles (Admin)
Neues Bundle erstellen

**Request Body**:
```json
{
  "name": "Beauty Deluxe Paket",
  "description": "Verwöhnprogramm für besondere Anlässe",
  "bundlePrice": 499.00,
  "displayOrder": 10,
  "validFrom": "2026-02-01T00:00:00Z",
  "validUntil": "2026-12-31T23:59:59Z",
  "termsAndConditions": "Paket muss innerhalb 90 Tagen eingelöst werden",
  "items": [
    {
      "serviceId": "guid-service-1",
      "quantity": 1,
      "displayOrder": 1,
      "notes": null
    },
    {
      "serviceId": "guid-service-2",
      "quantity": 2,
      "displayOrder": 2,
      "notes": "2x Behandlung im Abstand von 4 Wochen"
    }
  ]
}
```

**Automatisch berechnet**:
- `originalPrice`: Summe aller Service-Preise × Quantity
- `discountPercentage`: (originalPrice - bundlePrice) / originalPrice × 100
- `totalDurationMinutes`: Summe aller Service-Dauern × Quantity

### PUT /api/servicebundles/{id} (Admin)
Bundle aktualisieren

**Request Body** (alle Felder optional):
```json
{
  "name": "Neuer Name",
  "description": "Neue Beschreibung",
  "bundlePrice": 549.00,
  "displayOrder": 5,
  "validFrom": "2026-03-01T00:00:00Z",
  "validUntil": "2026-11-30T23:59:59Z",
  "termsAndConditions": "Aktualisierte AGB",
  "isActive": true,
  "items": [
    // Wenn items angegeben, werden ALLE Items ersetzt
    {
      "serviceId": "guid",
      "quantity": 1,
      "displayOrder": 1,
      "notes": null
    }
  ]
}
```

**Wichtig**:
- Wenn `items` angegeben ist, werden alle bisherigen Items gelöscht und durch die neuen ersetzt
- Weglassen von `items` behält die bestehenden Items bei

### DELETE /api/servicebundles/{id} (Admin)
Bundle löschen (Soft Delete)

**Wichtig**: Nur möglich wenn keine aktiven zukünftigen Buchungen existieren.

## Buchung von Bundles

### Buchung erstellen

```json
POST /api/bookings
{
  "customerId": "guid",
  "bundleId": "guid-of-bundle", // ENTWEDER bundleId
  "serviceId": null,             // ODER serviceId (nicht beide!)
  "bookingDate": "2026-02-15",
  "startTime": "14:00",
  "customerNotes": "Freue mich auf das Paket!"
}
```

### Unterschied Service vs. Bundle Buchung

**Service-Buchung**:
- `serviceId`: gesetzt
- `bundleId`: null
- Dauer: Service.DurationMinutes
- Preis: Service.Price

**Bundle-Buchung**:
- `serviceId`: null
- `bundleId`: gesetzt
- Dauer: Bundle.TotalDurationMinutes
- Preis: Bundle.BundlePrice

## Installation

### 1. Migration ausführen

Öffne WebMSSQL und führe folgendes SQL-Script aus:
```sql
-- Siehe: Add_ServiceBundles_Migration.sql
```

Das Script erstellt:
- `ServiceBundles` Tabelle
- `ServiceBundleItems` Tabelle
- Aktualisiert `Bookings` Tabelle
- Fügt Check Constraint hinzu
- Erstellt Beispiel-Bundle (optional)

### 2. Bundles prüfen

```sql
SELECT
    b.Name,
    b.OriginalPrice,
    b.BundlePrice,
    b.DiscountPercentage,
    COUNT(bi.Id) AS ServiceCount
FROM ServiceBundles b
LEFT JOIN ServiceBundleItems bi ON b.Id = bi.BundleId
WHERE b.IsActive = 1
GROUP BY b.Id, b.Name, b.OriginalPrice, b.BundlePrice, b.DiscountPercentage;
```

### 3. Bundle-Items anzeigen

```sql
SELECT
    b.Name AS BundleName,
    s.Name AS ServiceName,
    bi.Quantity,
    s.Price AS ServicePrice,
    (s.Price * bi.Quantity) AS TotalPrice,
    s.DurationMinutes AS ServiceDuration,
    (s.DurationMinutes * bi.Quantity) AS TotalDuration
FROM ServiceBundles b
JOIN ServiceBundleItems bi ON b.Id = bi.BundleId
JOIN Services s ON bi.ServiceId = s.Id
WHERE b.IsActive = 1
ORDER BY b.DisplayOrder, bi.DisplayOrder;
```

## Frontend Integration

### Bundles anzeigen

```typescript
const bundles = await fetch('/api/servicebundles').then(r => r.json());

bundles.forEach(bundle => {
  console.log(`${bundle.name} - CHF ${bundle.bundlePrice}`);
  console.log(`Spare ${bundle.discountPercentage}% (CHF ${bundle.savings})`);
  console.log(`Enthält ${bundle.items.length} Services:`);

  bundle.items.forEach(item => {
    console.log(`  - ${item.quantity}x ${item.serviceName}`);
  });
});
```

### Bundle buchen

```typescript
const booking = {
  customerId: userId,
  bundleId: selectedBundle.id,
  serviceId: null, // Wichtig: null bei Bundle-Buchung
  bookingDate: '2026-02-15',
  startTime: '14:00',
  customerNotes: 'Freue mich auf das Paket!'
};

const response = await fetch('/api/bookings', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(booking)
});
```

### Bundles & Services kombiniert anzeigen

```typescript
// Alle Services
const services = await fetch('/api/services').then(r => r.json());

// Alle Bundles
const bundles = await fetch('/api/servicebundles').then(r => r.json());

// Kombinierte Liste mit Typ
const offerings = [
  ...services.map(s => ({ ...s, type: 'service' })),
  ...bundles.map(b => ({ ...b, type: 'bundle' }))
].sort((a, b) => a.displayOrder - b.displayOrder);
```

## Best Practices

### 1. Rabattgestaltung

**Empfohlene Rabatte**:
- 3 Services: 10-15% Rabatt
- 4-5 Services: 15-20% Rabatt
- 6+ Services: 20-25% Rabatt

**Psychologischer Preis**:
```typescript
// Statt CHF 592.45 → CHF 595.00 (Endziffer 5 oder 9)
const bundlePrice = Math.ceil(calculatedPrice / 5) * 5 - 0.01;
```

### 2. Bundle-Namen

**Gut**: Anti-Aging Komplettpaket, Beauty Starter, Wellness Deluxe
**Schlecht**: Bundle #1, Paket A, Angebot 2026

### 3. Beschreibungen

**Gut**: "Perfekte Kombination aus Hyaluron und Botox für ein frisches, jugendliches Aussehen. Ideal für Erstkundinnen"

**Schlecht**: "3 Services zusammen"

### 4. Gültigkeitsdauer

```sql
-- Weihnachts-Aktion
ValidFrom = '2026-11-15',
ValidUntil = '2026-12-31'

-- Frühlings-Angebot
ValidFrom = '2026-03-01',
ValidUntil = '2026-05-31'

-- Dauerhaft
ValidFrom = NULL,
ValidUntil = NULL
```

### 5. Terms & Conditions

Beispiele:
- "Paket muss innerhalb 90 Tagen eingelöst werden"
- "Services können nicht einzeln gebucht werden"
- "Nicht mit anderen Rabatten kombinierbar"
- "Nur für Neukunden"

## Vorteile

✅ **Umsatzsteigerung**: Höhere durchschnittliche Buchungswerte
✅ **Kundenbindung**: Pakete fördern Wiederbesuche
✅ **Planbarkeit**: Bessere Auslastung durch Mehrfachbuchungen
✅ **Marketing**: Attraktive Angebote für Kampagnen
✅ **Flexibilität**: Saisonale oder Event-basierte Bundles

## Nächste Schritte

Nach der Installation kannst du:
1. Bundles im Admin-Bereich erstellen
2. Frontend-UI für Bundle-Auswahl implementieren
3. Bundle-Empfehlungen basierend auf Kundenprofilen
4. Bundle-Performance Tracking (Conversion Rate, Beliebtheit)
5. Automatische Email-Kampagnen für neue Bundles

## Beispiel: Gutschein-Bundle

```json
{
  "name": "Valentinstag Geschenkpaket",
  "description": "Das perfekte Geschenk zum Valentinstag - 2 Treatments nach Wahl",
  "bundlePrice": 449.00,
  "validFrom": "2026-02-01T00:00:00Z",
  "validUntil": "2026-02-20T23:59:59Z",
  "termsAndConditions": "Gilt für alle Services bis CHF 249. Nicht einlösbar für PRP-Behandlungen. Gültig bis 31.05.2026",
  "items": [
    {
      "serviceId": "<Beratungsgespräch>",
      "quantity": 1,
      "displayOrder": 1,
      "notes": "Kostenlose Erstberatung inklusive"
    },
    {
      "serviceId": "<Hyaluron-Jawline>",
      "quantity": 1,
      "displayOrder": 2
    },
    {
      "serviceId": "<HydraFacial-Deluxe>",
      "quantity": 1,
      "displayOrder": 3
    }
  ]
}
```
