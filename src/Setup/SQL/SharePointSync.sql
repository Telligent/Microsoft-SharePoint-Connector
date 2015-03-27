-- Table: te_SharePoint_ProfileSync --
IF EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID(N'[dbo].[te_SharePoint_ProfileSync]') AND TYPE IN (N'U'))
BEGIN
	-- add column if it does not exist
	IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE COLUMN_NAME='SyncStatus' AND TABLE_NAME='te_SharePoint_ProfileSync')
	BEGIN
		Print 'Updating...te_SharePoint_ProfileSync'
		ALTER TABLE [dbo].[te_SharePoint_ProfileSync]
			ADD SyncStatus INT NOT NULL DEFAULT 1
	END
END
ELSE
BEGIN
	Print 'Creating...te_SharePoint_ProfileSync'
	CREATE TABLE [dbo].[te_SharePoint_ProfileSync]
	(
		ProviderId INT NOT NULL PRIMARY KEY,
		LastRunTime DATETIME NOT NULL,
		SyncStatus INT NOT NULL
	)
END
GO

-- Sproc: te_SharePoint_ProfileSync_GetLastRunTime --
Print 'Creating...te_SharePoint_ProfileSync_GetLastRunTime'

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_ProfileSync_GetLastRunTime]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[te_SharePoint_ProfileSync_GetLastRunTime]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_ProfileSync_GetLastRunTime]
	@ProviderId INT,
	@LastRunTime DATETIME OUT,
	@SyncStatus INT OUT
AS
BEGIN
	SELECT @LastRunTime = LastRunTime, @SyncStatus = SyncStatus
	FROM [dbo].[te_SharePoint_ProfileSync]
	WHERE ProviderId = @ProviderId
END
GO

-- Sproc: te_SharePoint_ProfileSync_ResetLastRunTime --
Print 'Creating...te_SharePoint_ProfileSync_ResetLastRunTime'

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_ProfileSync_ResetLastRunTime]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[te_SharePoint_ProfileSync_ResetLastRunTime]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_ProfileSync_ResetLastRunTime]
	@ProviderId INT,
	@LastRunTime DATETIME OUT,
	@SyncStatus INT
AS
BEGIN
	SET @LastRunTime = GETUTCDATE()
	UPDATE te_SharePoint_ProfileSync
	SET LastRunTime = @LastRunTime,
		SyncStatus  = @SyncStatus
	WHERE ProviderId = @ProviderId
	IF @@ROWCOUNT = 0
		INSERT INTO [dbo].[te_SharePoint_ProfileSync] (ProviderId, LastRunTime, SyncStatus) 
		VALUES (@ProviderId, @LastRunTime, @SyncStatus)
END
GO

-- Sproc: te_SharePoint_ProfileSync_SetLastRunStatus --
Print 'Creating...te_SharePoint_ProfileSync_SetLastRunStatus'

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_ProfileSync_SetLastRunStatus]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[te_SharePoint_ProfileSync_SetLastRunStatus]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_ProfileSync_SetLastRunStatus]
	@ProviderId INT,
	@SyncStatus INT
AS
BEGIN
	UPDATE te_SharePoint_ProfileSync
	SET SyncStatus = @SyncStatus
	WHERE ProviderId = @ProviderId
	IF @@ROWCOUNT = 0
	BEGIN
		DECLARE @LastRunTime DATETIME
		SET @LastRunTime = GETUTCDATE()
		INSERT INTO [dbo].[te_SharePoint_ProfileSync] (ProviderId, LastRunTime, SyncStatus) 
		VALUES (@ProviderId, @LastRunTime, @SyncStatus)
	END
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_ProfileSync_GetLastRunTime] TO PUBLIC
GRANT EXECUTE ON [dbo].[te_SharePoint_ProfileSync_ResetLastRunTime] TO PUBLIC
GRANT EXECUTE ON [dbo].[te_SharePoint_ProfileSync_SetLastRunStatus] TO PUBLIC

GO
