CREATE TABLE [Integration].[ConfigSystem] (
    [id]                INT            IDENTITY (1, 1) NOT NULL,
    [systemName]        NVARCHAR (50)  NULL,
    [WebServiceAddress] NVARCHAR (500) NULL,
    [WebServiceAPIPath] NVARCHAR (500) NULL,
    [isActive]          TINYINT        NOT NULL,
    [FolderPath]        NVARCHAR (500) NULL,
    [UseWebService]     TINYINT        NOT NULL,
    CONSTRAINT [PK_ConfigSystem] PRIMARY KEY CLUSTERED ([id] ASC)
);
