IF OBJECT_ID(N'[dbo].[Booking]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Booking];
GO

IF OBJECT_ID(N'[dbo].[Event]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Event];
GO

IF OBJECT_ID(N'[dbo].[Venue]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Venue];
GO

CREATE TABLE [dbo].[Venue]
(
    [VenueId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [VenueName] NVARCHAR(120) NOT NULL,
    [Location] NVARCHAR(180) NOT NULL,
    [Capacity] INT NOT NULL,
    [ImageUrl] NVARCHAR(260) NULL,
    [Description] NVARCHAR(1000) NULL,
    [CreatedUtc] DATETIME2 NOT NULL CONSTRAINT [DF_Venue_CreatedUtc] DEFAULT SYSUTCDATETIME()
);
GO

CREATE UNIQUE INDEX [IX_Venue_VenueName] ON [dbo].[Venue]([VenueName]);
GO

CREATE TABLE [dbo].[Event]
(
    [EventId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [EventName] NVARCHAR(140) NOT NULL,
    [EventDate] DATETIME2 NOT NULL,
    [Description] NVARCHAR(1200) NULL,
    [VenueId] INT NULL,
    [OrganizerName] NVARCHAR(140) NOT NULL,
    [RequestedEndUtc] DATETIME2 NOT NULL,
    [ExpectedGuests] INT NOT NULL,
    [Status] NVARCHAR(40) NOT NULL,
    CONSTRAINT [FK_Event_Venue_VenueId] FOREIGN KEY ([VenueId]) REFERENCES [dbo].[Venue]([VenueId])
);
GO

CREATE INDEX [IX_Event_VenueId] ON [dbo].[Event]([VenueId]);
GO

CREATE TABLE [dbo].[Booking]
(
    [BookingID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [EventID] INT NOT NULL,
    [VenueID] INT NOT NULL,
    [BookingDate] DATETIME2 NOT NULL CONSTRAINT [DF_Booking_BookingDate] DEFAULT SYSUTCDATETIME(),
    [BookingReference] NVARCHAR(40) NOT NULL,
    [StartUtc] DATETIME2 NOT NULL,
    [EndUtc] DATETIME2 NOT NULL,
    [Status] NVARCHAR(40) NOT NULL,
    CONSTRAINT [FK_Booking_Event_EventID] FOREIGN KEY ([EventID]) REFERENCES [dbo].[Event]([EventId]),
    CONSTRAINT [FK_Booking_Venue_VenueID] FOREIGN KEY ([VenueID]) REFERENCES [dbo].[Venue]([VenueId])
);
GO

CREATE UNIQUE INDEX [IX_Booking_BookingReference] ON [dbo].[Booking]([BookingReference]);
GO

CREATE INDEX [IX_Booking_EventID] ON [dbo].[Booking]([EventID]);
GO

CREATE INDEX [IX_Booking_VenueID] ON [dbo].[Booking]([VenueID]);
GO
