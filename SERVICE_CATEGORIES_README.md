# Service Categories System

## Übersicht

Das Service-Kategorien-System ermöglicht die Organisation von Behandlungen in logische Gruppen für bessere Übersichtlichkeit und Navigation.

## Datenbank-Struktur

### ServiceCategories Tabelle

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| Id | UNIQUEIDENTIFIER | Primary Key |
| Name | NVARCHAR(100) | Kategorie-Name |
| Description | NVARCHAR(500) | Beschreibung der Kategorie |
| Icon | NVARCHAR(50) | Emoji oder Icon-Name |
| Color | NVARCHAR(20) | Hex-Farbe für UI |
| DisplayOrder | INT | Sortierreihenfolge |
| IsActive | BIT | Aktiv-Status |
| CreatedAt | DATETIME2 | Erstellungsdatum |
| UpdatedAt | DATETIME2 | Letzte Aktualisierung |

### Services Tabelle (Erweitert)

- **Neues Feld**: `CategoryId UNIQUEIDENTIFIER` (Nullable, Foreign Key zu ServiceCategories)
- **Beziehung**: `ON DELETE SET NULL` (Services bleiben erhalten wenn Kategorie gelöscht wird)

## Standard-Kategorien

### 1. Beratung (Icon: 💬)
- Kostenlose Beratungsgespräche
- **Services**: Kostenloses Beratungsgespräch

### 2. Hyaluron Behandlungen (Icon: 💉)
- Faltenunterspritzung und Volumenaufbau
- **Services**:
  - Hyaluron - Jawline
  - Hyaluron - Kinn-Aufbau
  - Hyaluron - Lippenfalten
  - Hyaluron - Lippenunterspritzung
  - Hyaluron - Marionettenfalte
  - Hyaluron - Wangenaufbau
  - Hyaluron - Nasolabialfalte
  - Hyaluron - Augenringe/Tränenfurche
  - Hylase
  - Mesotherapie
  - Skinbooster
  - Profhilo
  - Fett-weg-Spritze

### 3. Botox Behandlungen (Icon: ✨)
- Muskelentspannende Behandlungen
- **Services**:
  - Botox - Stirn (Zornesfalte)
  - Botox - Krähenfüße

### 4. Hautbehandlungen (Icon: 🌟)
- Gesichtsbehandlungen, Peelings und Facials
- **Services**:
  - BioPeelX Gesicht inkl. Maske
  - HydraFacial - Basic
  - HydraFacial - MD
  - HydraFacial - Deluxe

### 5. Advanced Treatments (Icon: 🔬)
- PRP, Microneedling und spezielle Therapien
- **Services**:
  - PRP bei Haarausfall / Eigenbluttherapie
  - Vampire Lifting / PRP inkl. Maske
  - Infusionstherapie
  - Radiofrequenz Microneedling - Gesicht
  - Radiofrequenz Microneedling - Gesicht, Hals, Dekolleté

## API Endpoints

### GET /api/servicecategories
Alle aktiven Kategorien mit Service-Anzahl abrufen

**Response**:
```json
[
  {
    "id": "guid",
    "name": "Hyaluron Behandlungen",
    "description": "Faltenunterspritzung...",
    "icon": "💉",
    "color": "#000000",
    "displayOrder": 1,
    "serviceCount": 13
  }
]
```

### GET /api/servicecategories/{id}
Einzelne Kategorie abrufen

### GET /api/servicecategories/{id}/services
Alle Services einer Kategorie abrufen

**Response**:
```json
[
  {
    "id": "guid",
    "name": "Hyaluron - Jawline",
    "description": "Konturierung der Kinnlinie...",
    "durationMinutes": 45,
    "price": 249.00,
    "displayOrder": 1,
    "categoryId": "guid",
    "categoryName": "Hyaluron Behandlungen"
  }
]
```

### POST /api/servicecategories (Admin)
Neue Kategorie erstellen

**Request Body**:
```json
{
  "name": "Neue Kategorie",
  "description": "Beschreibung",
  "icon": "🎯",
  "color": "#000000",
  "displayOrder": 10
}
```

### PUT /api/servicecategories/{id} (Admin)
Kategorie aktualisieren

**Request Body** (alle Felder optional):
```json
{
  "name": "Aktualisierter Name",
  "description": "Neue Beschreibung",
  "icon": "✨",
  "color": "#1F2937",
  "displayOrder": 5,
  "isActive": true
}
```

### DELETE /api/servicecategories/{id} (Admin)
Kategorie löschen (Soft Delete)

**Wichtig**: Nur möglich wenn keine aktiven Services in der Kategorie sind.

## Services API (Erweitert)

### GET /api/services
Alle Services enthalten jetzt auch:
- `categoryId`: GUID der Kategorie (nullable)
- `categoryName`: Name der Kategorie (nullable)

## Installation

### 1. Migration ausführen

Öffne WebMSSQL und führe folgendes SQL-Script aus:
```sql
-- Siehe: Add_ServiceCategories_Migration.sql
```

### 2. Services aktualisieren (optional)

Falls du die 3 fehlenden Services noch hinzufügen möchtest:
```sql
-- Siehe: Add_3_Missing_Services.sql
```

### 3. Kategorien prüfen

```sql
SELECT
    c.Name AS CategoryName,
    COUNT(s.Id) AS ServiceCount
FROM ServiceCategories c
LEFT JOIN Services s ON s.CategoryId = c.Id AND s.IsActive = 1
WHERE c.IsActive = 1
GROUP BY c.Name
ORDER BY c.DisplayOrder;
```

**Erwartetes Ergebnis**:
| CategoryName | ServiceCount |
|--------------|--------------|
| Beratung | 1 |
| Hyaluron Behandlungen | 13 |
| Botox Behandlungen | 2 |
| Hautbehandlungen | 4 |
| Advanced Treatments | 5 |

## Frontend Integration

### Services nach Kategorie gruppiert anzeigen

```typescript
const categories = await fetch('/api/servicecategories').then(r => r.json());

for (const category of categories) {
  const services = await fetch(`/api/servicecategories/${category.id}/services`)
    .then(r => r.json());

  // Render category with services
  renderCategory(category, services);
}
```

### Alle Services mit Kategorie-Info

```typescript
const services = await fetch('/api/services').then(r => r.json());

// Services sind schon mit categoryId und categoryName angereichert
services.forEach(service => {
  console.log(`${service.name} → ${service.categoryName || 'Unkategorisiert'}`);
});
```

## Vorteile

✅ **Bessere Organisation**: Services sind logisch gruppiert
✅ **Flexible Navigation**: Kunden können nach Kategorie filtern
✅ **Skalierbar**: Neue Kategorien können einfach hinzugefügt werden
✅ **SEO-freundlich**: Kategorien können für bessere Auffindbarkeit genutzt werden
✅ **Optionale Nutzung**: Services ohne Kategorie bleiben gültig (CategoryId ist nullable)

## Nächste Schritte

Nach der Installation kannst du:
1. Kategorien im Admin-Bereich verwalten
2. Services über die Admin-API Kategorien zuweisen
3. Frontend-UI für Kategorie-Navigation implementieren
4. Service-Bundles pro Kategorie erstellen (siehe SERVICE_BUNDLES_README.md)
