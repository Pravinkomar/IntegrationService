
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [Integration].[spGetItemTransfer]
	@messageId uniqueidentifier
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    select ParameterName as Name, ParameterValue as Value
	from Integration.MessageOutParameters  where MessageOutId=@messageId

END
