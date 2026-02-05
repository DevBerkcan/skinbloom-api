IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [BlockedTimeSlots] (
    [Id] uniqueidentifier NOT NULL,
    [BlockDate] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Reason] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BlockedTimeSlots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BusinessHours] (
    [Id] uniqueidentifier NOT NULL,
    [DayOfWeek] int NOT NULL,
    [OpenTime] time NOT NULL,
    [CloseTime] time NOT NULL,
    [IsOpen] bit NOT NULL,
    [BreakStartTime] time NULL,
    [BreakEndTime] time NULL,
    CONSTRAINT [PK_BusinessHours] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [LastVisit] datetime2 NULL,
    [TotalBookings] int NOT NULL,
    [NoShowCount] int NOT NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Services] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [DurationMinutes] int NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Settings] (
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(max) NOT NULL,
    [Description] nvarchar(500) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([Key])
);
GO

CREATE TABLE [Bookings] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [ServiceId] uniqueidentifier NOT NULL,
    [BookingDate] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ConfirmationSentAt] datetime2 NULL,
    [ReminderSentAt] datetime2 NULL,
    [CustomerNotes] nvarchar(max) NULL,
    [AdminNotes] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CancelledAt] datetime2 NULL,
    [CancellationReason] nvarchar(max) NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookings_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [EmailLogs] (
    [Id] uniqueidentifier NOT NULL,
    [BookingId] uniqueidentifier NULL,
    [EmailType] nvarchar(50) NOT NULL,
    [RecipientEmail] nvarchar(255) NOT NULL,
    [Subject] nvarchar(500) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [SentAt] datetime2 NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailLogs_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE SET NULL
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BreakEndTime', N'BreakStartTime', N'CloseTime', N'DayOfWeek', N'IsOpen', N'OpenTime') AND [object_id] = OBJECT_ID(N'[BusinessHours]'))
    SET IDENTITY_INSERT [BusinessHours] ON;
INSERT INTO [BusinessHours] ([Id], [BreakEndTime], [BreakStartTime], [CloseTime], [DayOfWeek], [IsOpen], [OpenTime])
VALUES ('13a514e0-4b48-439f-b2f4-7c83bf06c40d', NULL, NULL, '20:00:00', 4, CAST(1 AS bit), '09:00:00'),
('56e2422b-b8ca-4d0f-9afc-b4ea46140673', NULL, NULL, '18:00:00', 2, CAST(1 AS bit), '09:00:00'),
('dbe7723d-35cc-485f-9e6e-04e3a0639f94', NULL, NULL, '00:00:00', 0, CAST(0 AS bit), '00:00:00'),
('e0b75c2e-3d58-4f0c-82bd-24db70b471bd', NULL, NULL, '18:00:00', 1, CAST(1 AS bit), '09:00:00'),
('e54cf1ea-6cef-40a5-9da1-82f5509b61db', NULL, NULL, '16:00:00', 6, CAST(1 AS bit), '09:00:00'),
('ef78c874-bdc8-44f3-bb38-d5566581d2c9', NULL, NULL, '18:00:00', 3, CAST(1 AS bit), '09:00:00'),
('fa184a8e-ed14-4edc-91bc-7455c0c9a29f', NULL, NULL, '18:00:00', 5, CAST(1 AS bit), '09:00:00');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BreakEndTime', N'BreakStartTime', N'CloseTime', N'DayOfWeek', N'IsOpen', N'OpenTime') AND [object_id] = OBJECT_ID(N'[BusinessHours]'))
    SET IDENTITY_INSERT [BusinessHours] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Description', N'DisplayOrder', N'DurationMinutes', N'IsActive', N'Name', N'Price', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Services]'))
    SET IDENTITY_INSERT [Services] ON;
INSERT INTO [Services] ([Id], [CreatedAt], [Description], [DisplayOrder], [DurationMinutes], [IsActive], [Name], [Price], [UpdatedAt])
VALUES ('11111111-1111-1111-1111-111111111111', '2025-12-27T20:27:53.6878130Z', N'Klassischer Herrenhaarschnitt mit Styling', 1, 30, CAST(1 AS bit), N'Herrenschnitt', 35.0, '2025-12-27T20:27:53.6878130Z'),
('22222222-2222-2222-2222-222222222222', '2025-12-27T20:27:53.6878150Z', N'Professionelles Bart-Trimming und Konturenschneiden', 2, 20, CAST(1 AS bit), N'Bart Trimmen', 20.0, '2025-12-27T20:27:53.6878150Z'),
('33333333-3333-3333-3333-333333333333', '2025-12-27T20:27:53.6878160Z', N'Haarschnitt + Bart Trimmen', 3, 50, CAST(1 AS bit), N'Komplettpaket', 50.0, '2025-12-27T20:27:53.6878160Z'),
('44444444-4444-4444-4444-444444444444', '2025-12-27T20:27:53.6878160Z', N'Haarschnitt für Kinder bis 12 Jahre', 4, 30, CAST(1 AS bit), N'Kinder Haarschnitt', 25.0, '2025-12-27T20:27:53.6878160Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Description', N'DisplayOrder', N'DurationMinutes', N'IsActive', N'Name', N'Price', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Services]'))
    SET IDENTITY_INSERT [Services] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Key', N'Description', N'UpdatedAt', N'Value') AND [object_id] = OBJECT_ID(N'[Settings]'))
    SET IDENTITY_INSERT [Settings] ON;
INSERT INTO [Settings] ([Key], [Description], [UpdatedAt], [Value])
VALUES (N'ADMIN_EMAIL', N'Admin Email-Adresse', '2025-12-27T20:27:53.6878320Z', N'dario@barberdario.com'),
(N'BOOKING_INTERVAL_MINUTES', N'Zeitintervall für Buchungen in Minuten', '2025-12-27T20:27:53.6878320Z', N'15'),
(N'BUSINESS_ADDRESS', N'Geschäftsadresse', '2025-12-27T20:27:53.6878320Z', N'Berliner Allee 43, 40212 Düsseldorf'),
(N'BUSINESS_NAME', N'Name des Geschäfts', '2025-12-27T20:27:53.6878320Z', N'Barber Dario'),
(N'MAX_ADVANCE_BOOKING_DAYS', N'Wie viele Tage im Voraus kann gebucht werden', '2025-12-27T20:27:53.6878320Z', N'60'),
(N'MIN_ADVANCE_BOOKING_HOURS', N'Mindestvorlauf für Buchungen in Stunden', '2025-12-27T20:27:53.6878320Z', N'24'),
(N'REMINDER_HOURS_BEFORE', N'Wann vor dem Termin wird die Erinnerung gesendet (Stunden)', '2025-12-27T20:27:53.6878320Z', N'24'),
(N'TIMEZONE', N'Zeitzone des Geschäfts', '2025-12-27T20:27:53.6878320Z', N'Europe/Berlin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Key', N'Description', N'UpdatedAt', N'Value') AND [object_id] = OBJECT_ID(N'[Settings]'))
    SET IDENTITY_INSERT [Settings] OFF;
GO

CREATE INDEX [IX_BlockedTimeSlots_BlockDate] ON [BlockedTimeSlots] ([BlockDate]);
GO

CREATE INDEX [IX_Bookings_BookingDate] ON [Bookings] ([BookingDate]);
GO

CREATE INDEX [IX_Bookings_BookingDate_Status] ON [Bookings] ([BookingDate], [Status]);
GO

CREATE INDEX [IX_Bookings_CustomerId] ON [Bookings] ([CustomerId]);
GO

CREATE INDEX [IX_Bookings_ServiceId] ON [Bookings] ([ServiceId]);
GO

CREATE INDEX [IX_Bookings_Status] ON [Bookings] ([Status]);
GO

CREATE UNIQUE INDEX [IX_BusinessHours_DayOfWeek] ON [BusinessHours] ([DayOfWeek]);
GO

CREATE UNIQUE INDEX [IX_Customers_Email] ON [Customers] ([Email]);
GO

CREATE INDEX [IX_Customers_Phone] ON [Customers] ([Phone]);
GO

CREATE INDEX [IX_EmailLogs_BookingId] ON [EmailLogs] ([BookingId]);
GO

CREATE INDEX [IX_EmailLogs_EmailType] ON [EmailLogs] ([EmailType]);
GO

CREATE INDEX [IX_Services_IsActive] ON [Services] ([IsActive]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251227202753_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DELETE FROM [BusinessHours]
WHERE [Id] = '13a514e0-4b48-439f-b2f4-7c83bf06c40d';
SELECT @@ROWCOUNT;

GO

DELETE FROM [BusinessHours]
WHERE [Id] = '56e2422b-b8ca-4d0f-9afc-b4ea46140673';
SELECT @@ROWCOUNT;

GO

DELETE FROM [BusinessHours]
WHERE [Id] = 'dbe7723d-35cc-485f-9e6e-04e3a0639f94';
SELECT @@ROWCOUNT;

GO

DELETE FROM [BusinessHours]
WHERE [Id] = 'e0b75c2e-3d58-4f0c-82bd-24db70b471bd';
SELECT @@ROWCOUNT;

GO

DELETE FROM [BusinessHours]
WHERE [Id] = 'e54cf1ea-6cef-40a5-9da1-82f5509b61db';
SELECT @@ROWCOUNT;

GO

DELETE FROM [BusinessHours]
WHERE [Id] = 'ef78c874-bdc8-44f3-bb38-d5566581d2c9';
SELECT @@ROWCOUNT;

GO

DELETE FROM [BusinessHours]
WHERE [Id] = 'fa184a8e-ed14-4edc-91bc-7455c0c9a29f';
SELECT @@ROWCOUNT;

GO

ALTER TABLE [Bookings] ADD [ReferrerUrl] nvarchar(max) NULL;
GO

ALTER TABLE [Bookings] ADD [UtmCampaign] nvarchar(max) NULL;
GO

ALTER TABLE [Bookings] ADD [UtmContent] nvarchar(max) NULL;
GO

ALTER TABLE [Bookings] ADD [UtmMedium] nvarchar(max) NULL;
GO

ALTER TABLE [Bookings] ADD [UtmSource] nvarchar(max) NULL;
GO

ALTER TABLE [Bookings] ADD [UtmTerm] nvarchar(max) NULL;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BreakEndTime', N'BreakStartTime', N'CloseTime', N'DayOfWeek', N'IsOpen', N'OpenTime') AND [object_id] = OBJECT_ID(N'[BusinessHours]'))
    SET IDENTITY_INSERT [BusinessHours] ON;
INSERT INTO [BusinessHours] ([Id], [BreakEndTime], [BreakStartTime], [CloseTime], [DayOfWeek], [IsOpen], [OpenTime])
VALUES ('43cf2ba7-97ce-401c-a44d-4a4e978dacf1', NULL, NULL, '18:00:00', 3, CAST(1 AS bit), '09:00:00'),
('5c96e3ad-8ee6-4044-8b49-9afae24e1118', NULL, NULL, '16:00:00', 6, CAST(1 AS bit), '09:00:00'),
('77a854e4-861c-474d-88b1-da40bb0c151b', NULL, NULL, '00:00:00', 0, CAST(0 AS bit), '00:00:00'),
('d5396705-fde0-47c1-997f-3a410940fa70', NULL, NULL, '20:00:00', 4, CAST(1 AS bit), '09:00:00'),
('d9f4da3e-9548-455c-9f54-9620cd9355ca', NULL, NULL, '18:00:00', 2, CAST(1 AS bit), '09:00:00'),
('e0c4734d-0c8c-49ed-8fa3-ad254849a5e4', NULL, NULL, '18:00:00', 1, CAST(1 AS bit), '09:00:00'),
('f2a8b7c9-e383-4d6b-aeca-f385544a0a30', NULL, NULL, '18:00:00', 5, CAST(1 AS bit), '09:00:00');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BreakEndTime', N'BreakStartTime', N'CloseTime', N'DayOfWeek', N'IsOpen', N'OpenTime') AND [object_id] = OBJECT_ID(N'[BusinessHours]'))
    SET IDENTITY_INSERT [BusinessHours] OFF;
GO

UPDATE [Services] SET [CreatedAt] = '2025-12-27T21:50:57.5577300Z', [UpdatedAt] = '2025-12-27T21:50:57.5577300Z'
WHERE [Id] = '11111111-1111-1111-1111-111111111111';
SELECT @@ROWCOUNT;

GO

UPDATE [Services] SET [CreatedAt] = '2025-12-27T21:50:57.5577330Z', [UpdatedAt] = '2025-12-27T21:50:57.5577330Z'
WHERE [Id] = '22222222-2222-2222-2222-222222222222';
SELECT @@ROWCOUNT;

GO

UPDATE [Services] SET [CreatedAt] = '2025-12-27T21:50:57.5577330Z', [UpdatedAt] = '2025-12-27T21:50:57.5577330Z'
WHERE [Id] = '33333333-3333-3333-3333-333333333333';
SELECT @@ROWCOUNT;

GO

UPDATE [Services] SET [CreatedAt] = '2025-12-27T21:50:57.5577340Z', [UpdatedAt] = '2025-12-27T21:50:57.5577340Z'
WHERE [Id] = '44444444-4444-4444-4444-444444444444';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577520Z'
WHERE [Key] = N'ADMIN_EMAIL';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577520Z'
WHERE [Key] = N'BOOKING_INTERVAL_MINUTES';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577530Z'
WHERE [Key] = N'BUSINESS_ADDRESS';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577530Z'
WHERE [Key] = N'BUSINESS_NAME';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577520Z'
WHERE [Key] = N'MAX_ADVANCE_BOOKING_DAYS';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577520Z'
WHERE [Key] = N'MIN_ADVANCE_BOOKING_HOURS';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577520Z'
WHERE [Key] = N'REMINDER_HOURS_BEFORE';
SELECT @@ROWCOUNT;

GO

UPDATE [Settings] SET [UpdatedAt] = '2025-12-27T21:50:57.5577530Z'
WHERE [Key] = N'TIMEZONE';
SELECT @@ROWCOUNT;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251227215057_AddBookingTracking', N'8.0.11');
GO

COMMIT;
GO

