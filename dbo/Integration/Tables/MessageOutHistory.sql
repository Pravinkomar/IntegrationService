CREATE TABLE [Integration].[MessageOutHistory] (
    [Id]               UNIQUEIDENTIFIER NOT NULL,
    [BatchId]          NVARCHAR (200)   NULL,
    [ProductionUnitId] NVARCHAR (200)   NULL,
    [MessageTypeId]    INT              NOT NULL,
    [SystemId]         INT              NOT NULL,
    [MessageStatusId]  INT              NOT NULL,
    [EntryOn]          DATETIME         NOT NULL,
    [LastUpdated]      DATETIME         NOT NULL,
    [ResendCounter]    INT              NOT NULL,
    [Message]          TEXT             NOT NULL,
    [ProcessingTime]   FLOAT (53)       NULL,
    [Exception]        TEXT             NULL
);
