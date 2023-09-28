
-- =============================================
-- Author:		EB
-- Create date: 20180904
-- Description:	Cleans the MessageTables.
-- =============================================
CREATE PROCEDURE Integration.spCleanMessageTables
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Delete from parameters
	DELETE FROM Integration.MessageOutParameters where messageoutId in (
		SELECT Id from MessageOut where LastUpdated < DATEADD(MONTH, -1, GETDATE()) 
		and MessageStatusId = (SELECT TOP 1 Id from Integration.MessageStatuses where description = 'Message Sent (OK)'))
	-- Delete from MessageOut
	DELETE FROM Integration.MessageOut where LastUpdated < DATEADD(MONTH, -1, GETDATE()) 
		and MessageStatusId = (SELECT TOP 1 Id from Integration.MessageStatuses where description = 'Message Sent (OK)')
	-- Delete from MessageOutHistory
	DELETE FROM Integration.MessageOutHistory where LastUpdated < DATEADD(MONTH, -7, GETDATE()) 
		and MessageStatusId = (SELECT TOP 1 Id from Integration.MessageStatuses where description = 'Message Sent (OK)')

	DELETE FROM Integration.MessageIn where LastUpdated < DATEADD(MONTH, -1, GETDATE()) 
		and MessageStatusId = (SELECT TOP 1 Id from Integration.MessageStatuses where description = 'Message Sent (OK)')
END
