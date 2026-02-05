-- Migration: Add Newsletter & Waitlist Systems
-- Date: 2026-02-03
-- Description: Adds Newsletter, NewsletterRecipients, and Waitlist tables
--              Updates Customers table with newsletter subscription fields

-- Step 1: Update Customers table with newsletter fields
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'NewsletterSubscribed')
BEGIN
    ALTER TABLE Customers
    ADD NewsletterSubscribed BIT NOT NULL DEFAULT 0;

    ALTER TABLE Customers
    ADD NewsletterSubscribedAt DATETIME2 NULL;

    ALTER TABLE Customers
    ADD NewsletterUnsubscribedAt DATETIME2 NULL;

    ALTER TABLE Customers
    ADD UnsubscribeToken NVARCHAR(100) NULL;

    CREATE INDEX IX_Customers_NewsletterSubscribed ON Customers(NewsletterSubscribed);
    CREATE UNIQUE INDEX IX_Customers_UnsubscribeToken ON Customers(UnsubscribeToken) WHERE UnsubscribeToken IS NOT NULL;

    PRINT 'Newsletter fields added to Customers table';
END
ELSE
BEGIN
    PRINT 'Newsletter fields already exist in Customers table';
END;

-- Step 2: Create Newsletters table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Newsletters')
BEGIN
    CREATE TABLE Newsletters (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Subject NVARCHAR(500) NOT NULL,
        HtmlContent NVARCHAR(MAX) NOT NULL,
        PreviewText NVARCHAR(500) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        ScheduledFor DATETIME2 NULL,
        SentAt DATETIME2 NULL,
        RecipientCount INT NOT NULL DEFAULT 0,
        OpenedCount INT NOT NULL DEFAULT 0,
        ClickedCount INT NOT NULL DEFAULT 0,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_Newsletters_Status ON Newsletters(Status);
    CREATE INDEX IX_Newsletters_ScheduledFor ON Newsletters(ScheduledFor);
    CREATE INDEX IX_Newsletters_SentAt ON Newsletters(SentAt);

    PRINT 'Newsletters table created successfully';
END
ELSE
BEGIN
    PRINT 'Newsletters table already exists';
END;

-- Step 3: Create NewsletterRecipients table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NewsletterRecipients')
BEGIN
    CREATE TABLE NewsletterRecipients (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        NewsletterId UNIQUEIDENTIFIER NOT NULL,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        Sent BIT NOT NULL DEFAULT 0,
        SentAt DATETIME2 NULL,
        Opened BIT NOT NULL DEFAULT 0,
        OpenedAt DATETIME2 NULL,
        Clicked BIT NOT NULL DEFAULT 0,
        ClickedAt DATETIME2 NULL,
        Failed BIT NOT NULL DEFAULT 0,
        ErrorMessage NVARCHAR(1000) NULL,

        CONSTRAINT FK_NewsletterRecipients_Newsletters_NewsletterId
            FOREIGN KEY (NewsletterId) REFERENCES Newsletters(Id) ON DELETE CASCADE,

        CONSTRAINT FK_NewsletterRecipients_Customers_CustomerId
            FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_NewsletterRecipients_NewsletterId ON NewsletterRecipients(NewsletterId);
    CREATE INDEX IX_NewsletterRecipients_CustomerId ON NewsletterRecipients(CustomerId);
    CREATE INDEX IX_NewsletterRecipients_Sent ON NewsletterRecipients(Sent);
    CREATE INDEX IX_NewsletterRecipients_Opened ON NewsletterRecipients(Opened);

    PRINT 'NewsletterRecipients table created successfully';
END
ELSE
BEGIN
    PRINT 'NewsletterRecipients table already exists';
END;

-- Step 4: Create Waitlists table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Waitlists')
BEGIN
    CREATE TABLE Waitlists (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        ServiceId UNIQUEIDENTIFIER NULL,
        BundleId UNIQUEIDENTIFIER NULL,
        PreferredDate DATE NULL,
        PreferredTimeFrom TIME NULL,
        PreferredTimeTo TIME NULL,
        Notes NVARCHAR(500) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        NotifiedAt DATETIME2 NULL,
        ConvertedAt DATETIME2 NULL,
        ConvertedToBookingId UNIQUEIDENTIFIER NULL,
        ExpiredAt DATETIME2 NULL,

        CONSTRAINT FK_Waitlists_Customers_CustomerId
            FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,

        CONSTRAINT FK_Waitlists_Services_ServiceId
            FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE NO ACTION,

        CONSTRAINT FK_Waitlists_ServiceBundles_BundleId
            FOREIGN KEY (BundleId) REFERENCES ServiceBundles(Id) ON DELETE NO ACTION,

        CONSTRAINT FK_Waitlists_Bookings_ConvertedToBookingId
            FOREIGN KEY (ConvertedToBookingId) REFERENCES Bookings(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_Waitlists_CustomerId ON Waitlists(CustomerId);
    CREATE INDEX IX_Waitlists_ServiceId ON Waitlists(ServiceId);
    CREATE INDEX IX_Waitlists_BundleId ON Waitlists(BundleId);
    CREATE INDEX IX_Waitlists_Status ON Waitlists(Status);
    CREATE INDEX IX_Waitlists_PreferredDate ON Waitlists(PreferredDate);
    CREATE INDEX IX_Waitlists_CreatedAt ON Waitlists(CreatedAt);

    PRINT 'Waitlists table created successfully';
END
ELSE
BEGIN
    PRINT 'Waitlists table already exists';
END;

-- Step 5: Generate unsubscribe tokens for existing customers
UPDATE Customers
SET UnsubscribeToken = CONVERT(NVARCHAR(100), NEWID())
WHERE UnsubscribeToken IS NULL;

PRINT 'Unsubscribe tokens generated for existing customers';

-- Verify tables
SELECT
    'Customers' AS TableName,
    COUNT(*) AS RecordCount,
    SUM(CASE WHEN NewsletterSubscribed = 1 THEN 1 ELSE 0 END) AS NewsletterSubscribers
FROM Customers
UNION ALL
SELECT 'Newsletters', COUNT(*), 0 FROM Newsletters
UNION ALL
SELECT 'NewsletterRecipients', COUNT(*), 0 FROM NewsletterRecipients
UNION ALL
SELECT 'Waitlists', COUNT(*), 0 FROM Waitlists;

PRINT 'Newsletter & Waitlist migration completed successfully!';
