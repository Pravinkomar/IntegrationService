-- =============================================
-- Author:		Erik Bergbom
-- Create date: 2018-08-28
-- Description:	Get New Outgoing Messages to process
-- =============================================
CREATE PROCEDURE [dbo].[spIntegrationGetOutgoingMessages]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	--SET NOCOUNT ON;

    select mo.Id, mo.Message, mo.ResendCounter, mo.LastUpdated,mo.EntryOn, mo.BatchId, mo.ProductionUnitId,
	cs.UseWebService, cs.FolderPath, cs.WebServiceAddress, cs.SystemName,
	cmt.DataStoredProcedure, cmt.MessageTemplate, cmt.ResendCounter as DefaultResendValue,
	ms.Description
	FROM Integration.MessageOut mo
	inner join Integration.ConfigSystem cs on mo.SystemId = cs.Id and cs.isActive = 1
	inner join Integration.ConfigMessageTypes cmt on mo.MessageTypeId = cmt.Id
	inner join Integration.MessageStatuses ms on mo.MessageStatusId = ms.Id
	Where ms.Id in (4,5) -- 4 = Processing, 5 = Resending
END
