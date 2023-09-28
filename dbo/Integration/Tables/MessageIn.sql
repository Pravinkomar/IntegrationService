CREATE TABLE [Integration].[MessageIn] (
    [Id]               UNIQUEIDENTIFIER CONSTRAINT [DF_MessageIn_Id] DEFAULT (newsequentialid()) NOT NULL,
    [BatchId]          NVARCHAR (200)   NOT NULL,
    [ProductionUnitId] NVARCHAR (200)   NULL,
    [MessageTypeId]    INT              NOT NULL,
    [SystemId]         INT              NOT NULL,
    [MessageStatusId]  INT              NOT NULL,
    [EntryOn]          DATETIME         NOT NULL,
    [LastUpdated]      DATETIME         NOT NULL,
    [ResendCounter]    INT              NOT NULL,
    [Message]          TEXT             NOT NULL,
    CONSTRAINT [PK_MessageIn] PRIMARY KEY CLUSTERED ([Id] ASC)
);

