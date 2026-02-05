-- Add 3 Missing Services to existing Skinbloom Database
-- Run this to add the missing services (22 → 25)
-- Date: 2026-02-03

INSERT INTO Services (Id, Name, Description, DurationMinutes, Price, DisplayOrder, CreatedAt, UpdatedAt, IsActive)
VALUES
-- Hyaluron - Augenringe (Missing service #1)
(NEWID(), 'Hyaluron - Augenringe/Tränenfurche', 'Behandlung von Augenringen und Tränenfurchen für frischeren Blick', 30, 249.00, 8, GETDATE(), GETDATE(), 1),

-- Botox - Stirn (Missing service #2)
(NEWID(), 'Botox - Stirn (Zornesfalte)', 'Glättung der Zornesfalte und Stirnfalten mit Botulinum', 30, 199.00, 15, GETDATE(), GETDATE(), 1),

-- Botox - Krähenfüße (Missing service #3)
(NEWID(), 'Botox - Krähenfüße', 'Behandlung der Lachfältchen um die Augen', 20, 179.00, 16, GETDATE(), GETDATE(), 1);

-- Verify total count (should be 25 now)
SELECT COUNT(*) AS TotalServices FROM Services WHERE IsActive = 1;
