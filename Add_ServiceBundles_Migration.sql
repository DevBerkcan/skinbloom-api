-- Migration: Add Service Bundles/Packages System
-- Date: 2026-02-03
-- Description: Adds ServiceBundles and ServiceBundleItems tables, updates Bookings table

-- Step 1: Create ServiceBundles table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ServiceBundles')
BEGIN
    CREATE TABLE ServiceBundles (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        OriginalPrice DECIMAL(10,2) NOT NULL, -- Sum of all included services
        BundlePrice DECIMAL(10,2) NOT NULL, -- Discounted package price
        DiscountPercentage DECIMAL(5,2) NOT NULL, -- Calculated discount
        TotalDurationMinutes INT NOT NULL, -- Sum of all service durations
        DisplayOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        ValidFrom DATETIME2 NULL, -- Optional: Bundle valid from date
        ValidUntil DATETIME2 NULL, -- Optional: Bundle valid until date
        TermsAndConditions NVARCHAR(2000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_ServiceBundles_IsActive ON ServiceBundles(IsActive);
    CREATE INDEX IX_ServiceBundles_DisplayOrder ON ServiceBundles(DisplayOrder);
    CREATE INDEX IX_ServiceBundles_ValidDates ON ServiceBundles(ValidFrom, ValidUntil);

    PRINT 'ServiceBundles table created successfully';
END
ELSE
BEGIN
    PRINT 'ServiceBundles table already exists, skipping creation';
END;

-- Step 2: Create ServiceBundleItems table (junction table between bundles and services)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ServiceBundleItems')
BEGIN
    CREATE TABLE ServiceBundleItems (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        BundleId UNIQUEIDENTIFIER NOT NULL,
        ServiceId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL DEFAULT 1, -- How many times this service is included
        DisplayOrder INT NOT NULL DEFAULT 0,
        Notes NVARCHAR(500) NULL,

        CONSTRAINT FK_ServiceBundleItems_ServiceBundles_BundleId
            FOREIGN KEY (BundleId) REFERENCES ServiceBundles(Id) ON DELETE CASCADE,

        CONSTRAINT FK_ServiceBundleItems_Services_ServiceId
            FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ServiceBundleItems_BundleId ON ServiceBundleItems(BundleId);
    CREATE INDEX IX_ServiceBundleItems_ServiceId ON ServiceBundleItems(ServiceId);
    CREATE INDEX IX_ServiceBundleItems_BundleDisplayOrder ON ServiceBundleItems(BundleId, DisplayOrder);

    PRINT 'ServiceBundleItems table created successfully';
END
ELSE
BEGIN
    PRINT 'ServiceBundleItems table already exists, skipping creation';
END;

-- Step 3: Update Bookings table to support bundles
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Bookings' AND COLUMN_NAME = 'BundleId')
BEGIN
    -- Add BundleId column
    ALTER TABLE Bookings
    ADD BundleId UNIQUEIDENTIFIER NULL;

    -- Make ServiceId nullable (booking can be for service OR bundle)
    ALTER TABLE Bookings
    ALTER COLUMN ServiceId UNIQUEIDENTIFIER NULL;

    -- Add foreign key constraint
    ALTER TABLE Bookings
    ADD CONSTRAINT FK_Bookings_ServiceBundles_BundleId
    FOREIGN KEY (BundleId) REFERENCES ServiceBundles(Id) ON DELETE NO ACTION;

    -- Add indexes
    CREATE INDEX IX_Bookings_BundleId ON Bookings(BundleId);

    PRINT 'BundleId column added to Bookings table';
END
ELSE
BEGIN
    PRINT 'BundleId column already exists in Bookings table';
END;

-- Step 4: Add check constraint to ensure either ServiceId or BundleId is set (not both, not neither)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Bookings_ServiceOrBundle')
BEGIN
    ALTER TABLE Bookings
    ADD CONSTRAINT CK_Bookings_ServiceOrBundle
    CHECK (
        (ServiceId IS NOT NULL AND BundleId IS NULL) OR
        (ServiceId IS NULL AND BundleId IS NOT NULL)
    );

    PRINT 'Check constraint added to ensure booking has either service or bundle';
END
ELSE
BEGIN
    PRINT 'Check constraint already exists';
END;

-- Step 5: Insert example bundles (optional - can be commented out)
PRINT 'Creating example bundles...';

-- Example Bundle 1: Anti-Aging Komplettpaket
DECLARE @AntiAgingBundleId UNIQUEIDENTIFIER = NEWID();
DECLARE @HyaluronJawlineId UNIQUEIDENTIFIER;
DECLARE @HyaluronNasolabialId UNIQUEIDENTIFIER;
DECLARE @BotoxStirnId UNIQUEIDENTIFIER;

-- Get service IDs (adjust names if needed)
SELECT @HyaluronJawlineId = Id FROM Services WHERE Name LIKE '%Jawline%' AND IsActive = 1;
SELECT @HyaluronNasolabialId = Id FROM Services WHERE Name LIKE '%Nasolabialfalte%' AND IsActive = 1;
SELECT @BotoxStirnId = Id FROM Services WHERE Name LIKE '%Botox%Stirn%' AND IsActive = 1;

IF @HyaluronJawlineId IS NOT NULL AND @HyaluronNasolabialId IS NOT NULL AND @BotoxStirnId IS NOT NULL
BEGIN
    -- Calculate prices
    DECLARE @Price1 DECIMAL(10,2), @Price2 DECIMAL(10,2), @Price3 DECIMAL(10,2);
    DECLARE @Duration1 INT, @Duration2 INT, @Duration3 INT;

    SELECT @Price1 = Price, @Duration1 = DurationMinutes FROM Services WHERE Id = @HyaluronJawlineId;
    SELECT @Price2 = Price, @Duration2 = DurationMinutes FROM Services WHERE Id = @HyaluronNasolabialId;
    SELECT @Price3 = Price, @Duration3 = DurationMinutes FROM Services WHERE Id = @BotoxStirnId;

    DECLARE @TotalPrice DECIMAL(10,2) = @Price1 + @Price2 + @Price3;
    DECLARE @BundlePrice DECIMAL(10,2) = ROUND(@TotalPrice * 0.85, 2); -- 15% discount
    DECLARE @Discount DECIMAL(5,2) = ROUND((@TotalPrice - @BundlePrice) / @TotalPrice * 100, 2);
    DECLARE @TotalDuration INT = @Duration1 + @Duration2 + @Duration3;

    INSERT INTO ServiceBundles (Id, Name, Description, OriginalPrice, BundlePrice, DiscountPercentage, TotalDurationMinutes, DisplayOrder, IsActive, ValidFrom, ValidUntil, CreatedAt, UpdatedAt)
    VALUES (
        @AntiAgingBundleId,
        'Anti-Aging Komplettpaket',
        'Perfekte Kombination aus Hyaluron und Botox für ein frisches, jugendliches Aussehen',
        @TotalPrice,
        @BundlePrice,
        @Discount,
        @TotalDuration,
        1,
        1,
        NULL,
        NULL,
        GETDATE(),
        GETDATE()
    );

    -- Add bundle items
    INSERT INTO ServiceBundleItems (Id, BundleId, ServiceId, Quantity, DisplayOrder)
    VALUES
        (NEWID(), @AntiAgingBundleId, @HyaluronJawlineId, 1, 1),
        (NEWID(), @AntiAgingBundleId, @HyaluronNasolabialId, 1, 2),
        (NEWID(), @AntiAgingBundleId, @BotoxStirnId, 1, 3);

    PRINT 'Example bundle "Anti-Aging Komplettpaket" created';
END
ELSE
BEGIN
    PRINT 'Skipped example bundle creation - required services not found';
END;

-- Verify bundles
SELECT
    b.Name AS BundleName,
    b.OriginalPrice,
    b.BundlePrice,
    b.DiscountPercentage AS Discount,
    b.TotalDurationMinutes,
    COUNT(bi.Id) AS ServiceCount,
    STRING_AGG(s.Name, ', ') AS IncludedServices
FROM ServiceBundles b
LEFT JOIN ServiceBundleItems bi ON b.Id = bi.BundleId
LEFT JOIN Services s ON bi.ServiceId = s.Id
WHERE b.IsActive = 1
GROUP BY b.Id, b.Name, b.OriginalPrice, b.BundlePrice, b.DiscountPercentage, b.TotalDurationMinutes
ORDER BY b.DisplayOrder;

PRINT 'Service bundles migration completed successfully!';
