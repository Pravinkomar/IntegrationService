CREATE TABLE [Integration].[MessageOut] (
    [Id]               UNIQUEIDENTIFIER CONSTRAINT [DF_MessageOut_Id] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [BatchId]          NVARCHAR (200)   NOT NULL,
    [ProductionUnitId] NVARCHAR (200)   NOT NULL,
    [MessageTypeId]    INT              NOT NULL,
    [SystemId]         INT              NOT NULL,
    [MessageStatusId]  INT              NOT NULL,
    [EntryOn]          DATETIME         NOT NULL,
    [LastUpdated]      DATETIME         NOT NULL,
    [ResendCounter]    INT              NOT NULL,
    [Message]          TEXT             NOT NULL,
    CONSTRAINT [PK_MessageOut] PRIMARY KEY CLUSTERED ([Id] ASC)
);
