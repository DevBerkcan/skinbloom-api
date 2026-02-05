-- Skinbloom Aesthetics Services Migration Script
-- Dieses Script löscht alte Barbershop Services und fügt Beauty Salon Behandlungen ein

-- Alte Services löschen
DELETE FROM Services;

-- Neue Beauty Services einfügen
-- BERATUNG (KOSTENLOS)
INSERT INTO Services (Id, Name, Description, DurationMinutes, Price, DisplayOrder, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'Kostenloses Beratungsgespräch', 'Persönliche Beratung zu allen Behandlungen – unverbindlich und kostenlos', 30, 0.00, 0, GETDATE(), GETDATE()),

-- HYALURON BEHANDLUNGEN
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

-- Prüfen ob Services erfolgreich eingefügt wurden
SELECT COUNT(*) AS TotalServices FROM Services;
SELECT * FROM Services ORDER BY DisplayOrder;
