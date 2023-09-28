
CREATE PROCEDURE [dbo].[spLocal_Insert_LogMessageEvent]
	-- Add the parameters for the stored procedure here
@ProcessArea varchar(250) = '',
@ProcessOrder varchar(250) = '', 
@Source varchar(250) = '', 
@Desc varchar(2048) = '',
@ErrorMsg varchar(255) = 'Informational'
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    -- Insert statements for procedure here
	DELETE FROM Local_LogMessageEvent where [Timestamp] < DATEADD(MONTH, -2, GETDATE())

INSERT INTO Local_LogMessageEvent([Timestamp], ProcessArea, WorkOrder ,[Source] ,[Description] ,MessageEventType)
         Values(getdate(), @ProcessArea, @ProcessOrder, @Source , @Desc,@ErrorMsg);
  --  GETDATE(), 'JDE', @ProcessOrder, 'spLocal_Export_JDE_CSV', 'Success Export JDE','Informational'   
--delete from          Local_LogMessageEvent
--where Timestamp <  DATEADD(day, -3, GETDATE())

END


CREATE TABLE [Integration].[MessageIn](
	[Id] [uniqueidentifier] NOT NULL,
	[BatchId] [nvarchar](200) NOT NULL,
	[ProductionUnitId] [nvarchar](200) ,
	[MessageTypeId] [int] NOT NULL,
	[SystemId] [int] NOT NULL,
	[MessageStatusId] [int] NOT NULL,
	[EntryOn] [datetime] NOT NULL,
	[LastUpdated] [datetime] NOT NULL,
	[ResendCounter] [int] NOT NULL,
	[Message] [text] NOT NULL,
 CONSTRAINT [PK_MessageIn] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

