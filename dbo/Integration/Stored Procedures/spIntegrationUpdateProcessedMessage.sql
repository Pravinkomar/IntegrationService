-- =============================================
-- Author:		EB
-- Create date: 20180830
-- Description:	Updates the MessageOut Table.
-- =============================================
create PROCEDURE [Integration].[spIntegrationUpdateProcessedMessage] 
	@messageId uniqueidentifier, 
	@message text
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	declare @wo as nvarchar(50)
    UPDATE Integration.MessageOut SET Message = @message, MessageStatusId = 2, LastUpdated = GETDATE() WHERE id = @messageId
	select @wo=batchId from Integration.MessageOut where id = @messageId

	Exec dbo.spLocal_Insert_LogMessageEvent @ProcessArea='Integration', @ProcessOrder=@wo, @Source='spIntegrationUpdateProcessedMessage',@Desc='Message Updated and created', @ErrorMsg='Informational'

	INSERT INTO Integration.MessageOutHistory
	SELECT Id,BatchId, ProductionUnitId,MessageTypeId, SystemId,MessageStatusId,EntryOn,GETDATE(),ResendCounter,@message,(DATEDIFF(SECOND, EntryOn, GETDATE())),NULL FROM Integration.MessageOut where id = @messageId
END
