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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE TABLE [Signals] (
        [SignalId] uniqueidentifier NOT NULL,
        [IdNumberHash] nvarchar(128) NOT NULL,
        [SignalType] nvarchar(50) NOT NULL,
        [SignalCategory] nvarchar(50) NOT NULL,
        [OccurrenceCount] int NOT NULL,
        [FirstSeen] datetime2 NOT NULL,
        [LastSeen] datetime2 NOT NULL,
        [AggregateRiskScore] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Signals] PRIMARY KEY ([SignalId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE TABLE [Tenants] (
        [TenantId] uniqueidentifier NOT NULL,
        [TenantName] nvarchar(200) NOT NULL,
        [TenantCode] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [SubscriptionTier] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([TenantId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityLogs] (
        [ActivityLogId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [Endpoint] nvarchar(500) NOT NULL,
        [HttpMethod] nvarchar(10) NOT NULL,
        [StatusCode] int NOT NULL,
        [ClientIp] nvarchar(45) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([ActivityLogId]),
        CONSTRAINT [FK_ActivityLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE TABLE [HrEvents] (
        [EventId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IdNumber] nvarchar(20) NOT NULL,
        [EventType] nvarchar(50) NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [EmployerName] nvarchar(200) NOT NULL,
        [EmployeeNumber] nvarchar(50) NULL,
        [VerificationStatus] nvarchar(30) NOT NULL,
        [RiskScore] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_HrEvents] PRIMARY KEY ([EventId]),
        CONSTRAINT [FK_HrEvents_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE TABLE [MnoEvents] (
        [EventId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IdNumber] nvarchar(20) NOT NULL,
        [Msisdn] nvarchar(20) NOT NULL,
        [EventType] nvarchar(50) NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [ApplicationChannel] nvarchar(50) NOT NULL,
        [OutletOrDealer] nvarchar(200) NOT NULL,
        [DeviceImei] nvarchar(20) NULL,
        [RiskScore] int NOT NULL,
        [FlagReason] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MnoEvents] PRIMARY KEY ([EventId]),
        CONSTRAINT [FK_MnoEvents_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_CreatedAt] ON [ActivityLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_TenantId] ON [ActivityLogs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HrEvents_EventDate] ON [HrEvents] ([EventDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HrEvents_TenantId] ON [HrEvents] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MnoEvents_EventDate] ON [MnoEvents] ([EventDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MnoEvents_TenantId] ON [MnoEvents] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Signals_IdNumberHash] ON [Signals] ([IdNumberHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Signals_IdNumberHash_SignalType_SignalCategory] ON [Signals] ([IdNumberHash], [SignalType], [SignalCategory]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Signals_IsActive] ON [Signals] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_TenantCode] ON [Tenants] ([TenantCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260707104402_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260707104402_InitialCreate', N'10.0.9');
END;

COMMIT;
GO

