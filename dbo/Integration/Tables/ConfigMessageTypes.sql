CREATE TABLE [Integration].[ConfigMessageTypes] (
    [id]                  INT            IDENTITY (1, 1) NOT NULL,
    [MessageName]         NVARCHAR (50)  NULL,
    [MessageTemplate]     TEXT           NOT NULL,
    [Description]         NVARCHAR (250) NULL,
    [DataStoredProcedure] NVARCHAR (500) NOT NULL,
    [ResendCounter]       INT            NOT NULL,
    CONSTRAINT [PK_ConfigMessageTypes] PRIMARY KEY CLUSTERED ([id] ASC)
);
