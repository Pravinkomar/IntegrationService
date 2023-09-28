CREATE TABLE [dbo].[Local_LogMessageEvent] (
    [MessageEvent_ID]  INT            IDENTITY (1, 1) NOT FOR REPLICATION NOT NULL,
    [Timestamp]        DATETIME       NULL,
    [ProcessArea]      VARCHAR (255)  NULL,
    [WorkOrder]        VARCHAR (255)  NULL,
    [Source]           VARCHAR (255)  NULL,
    [Description]      VARCHAR (2048) NULL,
    [MessageEventType] VARCHAR (255)  NULL
);
