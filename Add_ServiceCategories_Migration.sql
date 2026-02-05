-- Migration: Add Service Categories System
-- Date: 2026-02-03
-- Description: Adds ServiceCategories table and updates Services table with category relationship

-- Step 1: Create ServiceCategories table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ServiceCategories')
BEGIN
    CREATE TABLE ServiceCategories (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        Icon NVARCHAR(50) NULL,
        Color NVARCHAR(20) NULL,
        DisplayOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_ServiceCategories_IsActive ON ServiceCategories(IsActive);
    CREATE INDEX IX_ServiceCategories_DisplayOrder ON ServiceCategories(DisplayOrder);

    PRINT 'ServiceCategories table created successfully';
END
ELSE
BEGIN
    PRINT 'ServiceCategories table already exists, skipping creation';
END;

-- Step 2: Add CategoryId column to Services table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Services' AND COLUMN_NAME = 'CategoryId')
BEGIN
    ALTER TABLE Services
    ADD CategoryId UNIQUEIDENTIFIER NULL;

    ALTER TABLE Services
    ADD CONSTRAINT FK_Services_ServiceCategories_CategoryId
    FOREIGN KEY (CategoryId) REFERENCES ServiceCategories(Id) ON DELETE SET NULL;

    PRINT 'CategoryId column added to Services table';
END
ELSE
BEGIN
    PRINT 'CategoryId column already exists in Services table';
END;

-- Step 3: Insert Service Categories
DECLARE @HyaluronCategoryId UNIQUEIDENTIFIER = NEWID();
DECLARE @BotoxCategoryId UNIQUEIDENTIFIER = NEWID();
DECLARE @SkinCategoryId UNIQUEIDENTIFIER = NEWID();
DECLARE @AdvancedCategoryId UNIQUEIDENTIFIER = NEWID();
DECLARE @ConsultationCategoryId UNIQUEIDENTIFIER = NEWID();

-- Insert categories if they don't exist
IF NOT EXISTS (SELECT 1 FROM ServiceCategories WHERE Name = 'Beratung')
BEGIN
    SET @ConsultationCategoryId = NEWID();
    INSERT INTO ServiceCategories (Id, Name, Description, Icon, Color, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (@ConsultationCategoryId, 'Beratung', 'Kostenlose Beratungsgespräche und Ersttermine', '💬', '#6B7280', 0, 1, GETDATE(), GETDATE());
    PRINT 'Category "Beratung" inserted';
END
ELSE
BEGIN
    SELECT @ConsultationCategoryId = Id FROM ServiceCategories WHERE Name = 'Beratung';
    PRINT 'Category "Beratung" already exists';
END;

IF NOT EXISTS (SELECT 1 FROM ServiceCategories WHERE Name = 'Hyaluron Behandlungen')
BEGIN
    SET @HyaluronCategoryId = NEWID();
    INSERT INTO ServiceCategories (Id, Name, Description, Icon, Color, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (@HyaluronCategoryId, 'Hyaluron Behandlungen', 'Faltenunterspritzung und Volumenaufbau mit Hyaluronsäure', '💉', '#000000', 1, 1, GETDATE(), GETDATE());
    PRINT 'Category "Hyaluron Behandlungen" inserted';
END
ELSE
BEGIN
    SELECT @HyaluronCategoryId = Id FROM ServiceCategories WHERE Name = 'Hyaluron Behandlungen';
    PRINT 'Category "Hyaluron Behandlungen" already exists';
END;

IF NOT EXISTS (SELECT 1 FROM ServiceCategories WHERE Name = 'Botox Behandlungen')
BEGIN
    SET @BotoxCategoryId = NEWID();
    INSERT INTO ServiceCategories (Id, Name, Description, Icon, Color, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (@BotoxCategoryId, 'Botox Behandlungen', 'Muskelentspannende Behandlungen mit Botulinum', '✨', '#1F2937', 2, 1, GETDATE(), GETDATE());
    PRINT 'Category "Botox Behandlungen" inserted';
END
ELSE
BEGIN
    SELECT @BotoxCategoryId = Id FROM ServiceCategories WHERE Name = 'Botox Behandlungen';
    PRINT 'Category "Botox Behandlungen" already exists';
END;

IF NOT EXISTS (SELECT 1 FROM ServiceCategories WHERE Name = 'Hautbehandlungen')
BEGIN
    SET @SkinCategoryId = NEWID();
    INSERT INTO ServiceCategories (Id, Name, Description, Icon, Color, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (@SkinCategoryId, 'Hautbehandlungen', 'Gesichtsbehandlungen, Peelings und Facials', '🌟', '#374151', 3, 1, GETDATE(), GETDATE());
    PRINT 'Category "Hautbehandlungen" inserted';
END
ELSE
BEGIN
    SELECT @SkinCategoryId = Id FROM ServiceCategories WHERE Name = 'Hautbehandlungen';
    PRINT 'Category "Hautbehandlungen" already exists';
END;

IF NOT EXISTS (SELECT 1 FROM ServiceCategories WHERE Name = 'Advanced Treatments')
BEGIN
    SET @AdvancedCategoryId = NEWID();
    INSERT INTO ServiceCategories (Id, Name, Description, Icon, Color, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (@AdvancedCategoryId, 'Advanced Treatments', 'PRP, Microneedling und spezielle Therapien', '🔬', '#4B5563', 4, 1, GETDATE(), GETDATE());
    PRINT 'Category "Advanced Treatments" inserted';
END
ELSE
BEGIN
    SELECT @AdvancedCategoryId = Id FROM ServiceCategories WHERE Name = 'Advanced Treatments';
    PRINT 'Category "Advanced Treatments" already exists';
END;

-- Step 4: Assign existing services to categories
PRINT 'Assigning services to categories...';

-- Consultation
UPDATE Services SET CategoryId = @ConsultationCategoryId
WHERE Name LIKE '%Beratungsgespräch%' AND CategoryId IS NULL;

-- Hyaluron treatments
UPDATE Services SET CategoryId = @HyaluronCategoryId
WHERE (Name LIKE 'Hyaluron%' OR Name LIKE '%Hylase%' OR Name LIKE '%Skinbooster%' OR Name LIKE '%Profhilo%' OR Name LIKE '%Mesotherapie%' OR Name LIKE '%Fett-weg%')
AND CategoryId IS NULL;

-- Botox treatments
UPDATE Services SET CategoryId = @BotoxCategoryId
WHERE Name LIKE 'Botox%' AND CategoryId IS NULL;

-- Advanced treatments (PRP, Infusion)
UPDATE Services SET CategoryId = @AdvancedCategoryId
WHERE (Name LIKE '%PRP%' OR Name LIKE '%Vampire%' OR Name LIKE '%Infusion%' OR Name LIKE '%Radiofrequenz%')
AND CategoryId IS NULL;

-- Skin treatments (Peeling, HydraFacial)
UPDATE Services SET CategoryId = @SkinCategoryId
WHERE (Name LIKE '%Peel%' OR Name LIKE '%HydraFacial%' OR Name LIKE '%BioPeel%')
AND CategoryId IS NULL;

-- Verify category assignments
SELECT
    c.Name AS CategoryName,
    COUNT(s.Id) AS ServiceCount,
    STRING_AGG(s.Name, ', ') AS Services
FROM ServiceCategories c
LEFT JOIN Services s ON s.CategoryId = c.Id AND s.IsActive = 1
WHERE c.IsActive = 1
GROUP BY c.Id, c.Name, c.DisplayOrder
ORDER BY c.DisplayOrder;

PRINT 'Service categories migration completed successfully!';
