-- Skinbloom Aesthetics - Complete Service List (25 Services)
-- Updated: 2026-02-03
-- Includes all 25 services with correct pricing and descriptions

INSERT INTO Services (Id, Name, Description, DurationMinutes, Price, DisplayOrder, CreatedAt, UpdatedAt, IsActive)
VALUES
-- Consultation (Free)
(NEWID(), 'Kostenloses Beratungsgespräch', 'Persönliche Beratung zu allen Behandlungen – unverbindlich und kostenlos', 30, 0.00, 0, GETDATE(), GETDATE(), 1),

-- Hyaluron Treatments
(NEWID(), 'Hyaluron - Jawline', 'Konturierung der Kinnlinie mit Hyaluronsäure', 45, 249.00, 1, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Kinn-Aufbau', 'Kinn-Modellierung für harmonische Gesichtsproportionen', 45, 249.00, 2, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Lippenfalten', 'Glättung der Lippenfalten', 30, 249.00, 3, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Lippenunterspritzung', 'Volumenaufbau und Konturierung der Lippen', 45, 249.00, 4, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Marionettenfalte', 'Behandlung der Mundwinkelfalten', 30, 249.00, 5, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Wangenaufbau', 'Volumenaufbau im Wangenbereich', 45, 249.00, 6, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Nasolabialfalte', 'Glättung der Nasen-Mund-Falten', 30, 249.00, 7, GETDATE(), GETDATE(), 1),
(NEWID(), 'Hyaluron - Augenringe/Tränenfurche', 'Behandlung von Augenringen und Tränenfurchen für frischeren Blick', 30, 249.00, 8, GETDATE(), GETDATE(), 1),

-- Hylase & Injectable Treatments
(NEWID(), 'Hylase', 'Hyaluronidase zur Auflösung von Hyaluronsäure', 30, 150.00, 10, GETDATE(), GETDATE(), 1),
(NEWID(), 'Mesotherapie', 'Microinjektionen für Hautverbesserung', 45, 199.00, 11, GETDATE(), GETDATE(), 1),
(NEWID(), 'Skinbooster', 'Intensive Hautfeuchtigkeit und Straffung', 45, 199.00, 12, GETDATE(), GETDATE(), 1),
(NEWID(), 'Profhilo', 'Bio-Remodelling für natürliche Hautverjüngung', 30, 349.00, 13, GETDATE(), GETDATE(), 1),
(NEWID(), 'Fett-weg-Spritze', 'Lipolyse für lokale Fettreduktion', 30, 200.00, 14, GETDATE(), GETDATE(), 1),

-- Botox Treatments (NEW)
(NEWID(), 'Botox - Stirn (Zornesfalte)', 'Glättung der Zornesfalte und Stirnfalten mit Botulinum', 30, 199.00, 15, GETDATE(), GETDATE(), 1),
(NEWID(), 'Botox - Krähenfüße', 'Behandlung der Lachfältchen um die Augen', 20, 179.00, 16, GETDATE(), GETDATE(), 1),

-- PRP & Advanced Treatments
(NEWID(), 'PRP bei Haarausfall / Eigenbluttherapie', 'Plättchenreiches Plasma gegen Haarausfall', 60, 450.00, 20, GETDATE(), GETDATE(), 1),
(NEWID(), 'Vampire Lifting / PRP inkl. Maske 60 Min', 'PRP Gesichtsbehandlung mit revitalisierender Maske', 60, 450.00, 21, GETDATE(), GETDATE(), 1),
(NEWID(), 'Infusionstherapie', 'Revitalisierende Vitamin-Infusion', 45, 129.00, 25, GETDATE(), GETDATE(), 1),

-- Radiofrequency & Skin Treatments
(NEWID(), 'Radiofrequenz Microneedling - Gesicht', 'RF Microneedling für Gesicht', 60, 299.00, 30, GETDATE(), GETDATE(), 1),
(NEWID(), 'Radiofrequenz Microneedling - Gesicht, Hals, Dekolleté', 'RF Microneedling für Gesicht, Hals und Dekolleté', 90, 399.00, 31, GETDATE(), GETDATE(), 1),
(NEWID(), 'BioPeelX Gesicht inkl. Maske 60 Min', 'Chemisches Peeling mit anschließender Maske', 60, 170.00, 35, GETDATE(), GETDATE(), 1),

-- HydraFacial Treatments
(NEWID(), 'HydraFacial - Basic', 'Grundlegende HydraFacial Behandlung', 30, 189.00, 40, GETDATE(), GETDATE(), 1),
(NEWID(), 'HydraFacial - MD', 'Erweiterte HydraFacial mit zusätzlichen Boostern', 45, 199.00, 41, GETDATE(), GETDATE(), 1),
(NEWID(), 'HydraFacial - Deluxe', 'Premium HydraFacial mit allen Extras', 60, 229.00, 42, GETDATE(), GETDATE(), 1);

-- Total: 25 Services
-- Free: 1
-- Paid: 24
