CREATE TABLE [Integration].[MessageStatuses] (
    [id]          INT           IDENTITY (1, 1) NOT NULL,
    [description] NVARCHAR (50) NULL,
    CONSTRAINT [PK_MessageStatuses] PRIMARY KEY CLUSTERED ([id] ASC)
);
