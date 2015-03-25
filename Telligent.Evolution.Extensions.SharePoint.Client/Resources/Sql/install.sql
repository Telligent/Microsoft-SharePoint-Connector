/***********************************************
* Remove obsolete Tables and Stored Procedures */
-- Table: te_SharePoint_DocumentLibrary --
IF EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID(N'[dbo].[te_SharePoint_DocumentLibrary]') AND TYPE IN (N'U'))
DROP TABLE [dbo].[te_SharePoint_DocumentLibrary]
GO

-- Sproc: te_SharePoint_DocumentLibrary_GetByListId --
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_DocumentLibrary_GetByListId]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[te_SharePoint_DocumentLibrary_GetByListId]
GO

-- Sproc: te_SharePoint_DocumentLibrary_GetByItemId --
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_DocumentLibrary_GetByItemId]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[te_SharePoint_DocumentLibrary_GetByItemId]
GO

-- Sproc: te_SharePoint_DocumentLibrary_Add --
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_DocumentLibrary_Add]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[te_SharePoint_DocumentLibrary_Add]
GO

/***********************************************
*   Install new Tables and Stored Procedures   */
/***********************************************
* Table: te_SharePoint_List
***********************************************/
IF EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID(N'[dbo].[te_SharePoint_List]') AND TYPE IN (N'U'))
BEGIN
	-- add new column 'Type' if it does not exist
	IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE COLUMN_NAME='TypeId' AND TABLE_NAME='te_SharePoint_List')
	BEGIN
		ALTER TABLE [dbo].[te_SharePoint_List]
			ADD TypeId UNIQUEIDENTIFIER NOT NULL DEFAULT (CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER))
	END
	-- add new column 'ApplicationKey' if it does not exist
	IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE COLUMN_NAME='ApplicationKey' AND TABLE_NAME='te_SharePoint_List')
	BEGIN
		ALTER TABLE [dbo].[te_SharePoint_List]
			ADD ApplicationKey NVARCHAR(256) NULL
	END
END
ELSE
BEGIN
	CREATE TABLE [dbo].te_SharePoint_List
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		ApplicationId UNIQUEIDENTIFIER NOT NULL UNIQUE,
		ApplicationKey NVARCHAR(256) NULL,
		TypeId UNIQUEIDENTIFIER NOT NULL,
		GroupId INT NOT NULL,
		SPWebUrl NVARCHAR(256) NOT NULL,
		IsIndexed BIT NOT NULL DEFAULT 0,
		UpdatedDate DATETIME NOT NULL CONSTRAINT [DF_te_SharePoint_List_UpdatedDate] DEFAULT (GETDATE())
	)
END
GO

/***********************************************
* Table: te_SharePoint_Item
***********************************************/
IF EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID(N'[dbo].[te_SharePoint_Item]') AND TYPE IN (N'U'))
BEGIN
	-- add new column 'ContentKey' if it does not exist
	IF NOT EXISTS (SELECT * FROM information_schema.COLUMNS WHERE COLUMN_NAME='ContentKey' AND TABLE_NAME='te_SharePoint_Item')
	BEGIN
		ALTER TABLE [dbo].[te_SharePoint_Item]
			ADD ContentKey NVARCHAR(256) NULL
	END
END
ELSE
BEGIN
	CREATE TABLE [dbo].te_SharePoint_Item
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		ApplicationId UNIQUEIDENTIFIER NOT NULL 
			FOREIGN KEY REFERENCES [dbo].[te_SharePoint_List] (ApplicationId)
				ON UPDATE CASCADE
				ON DELETE CASCADE,
		ContentId UNIQUEIDENTIFIER NOT NULL UNIQUE,
		ContentKey NVARCHAR(256) NULL,
		ItemId INT NOT NULL,
		IsIndexed INT NOT NULL DEFAULT 0,
		UpdatedDate DATETIME NOT NULL CONSTRAINT [DF_te_SharePoint_Item_UpdatedDate] DEFAULT (GETDATE())
	)
END
GO

/***********************************************
* Table: te_SharePoint_View
***********************************************/
IF NOT EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID = OBJECT_ID(N'[dbo].[te_SharePoint_View]') AND TYPE IN (N'U'))
BEGIN
	CREATE TABLE [dbo].te_SharePoint_View
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		ListId INT NOT NULL
			FOREIGN KEY REFERENCES [dbo].[te_SharePoint_List] (Id)
				ON UPDATE CASCADE
				ON DELETE CASCADE,
		ViewId UNIQUEIDENTIFIER NOT NULL
	)
END
GO

/***********************************************
* Sproc: te_SharePoint_List_AddUpdate 
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_AddUpdate]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_AddUpdate]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_AddUpdate]
	@ApplicationId UNIQUEIDENTIFIER,
	@ApplicationKey NVARCHAR(256) = NULL,
	@TypeId UNIQUEIDENTIFIER,
	@GroupId INT,
	@SPWebUrl NVARCHAR(256),
	@ViewId UNIQUEIDENTIFIER
AS
BEGIN
	DECLARE @ListId INT 
	SET @ListId = -1

	UPDATE [dbo].[te_SharePoint_List]
	SET
		 ApplicationKey = @ApplicationKey
		,TypeId = @TypeId
		,GroupId = @GroupId
		,SPWebUrl = @SPWebUrl
		,@ListId = Id
	WHERE
		ApplicationId = @ApplicationId
	IF @@ROWCOUNT = 0
	BEGIN
		DECLARE @NotIndexed BIT
		SET @NotIndexed = 0
		INSERT INTO [dbo].[te_SharePoint_List]
			(ApplicationId, ApplicationKey, TypeId, GroupId, SPWebUrl, IsIndexed)
		VALUES
			(@ApplicationId, @ApplicationKey, @TypeId, @GroupId, @SPWebUrl, @NotIndexed)
		SET @ListId = SCOPE_IDENTITY()
	END
	UPDATE [dbo].[te_SharePoint_View]
	SET
		ViewId = @ViewId
	WHERE
		ListId = @ListId
	IF @@ROWCOUNT = 0
	BEGIN
		INSERT INTO [dbo].[te_SharePoint_View] (ListId, ViewId)
		VALUES (@ListId, @ViewId)
	END
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_AddUpdate] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_Delete 
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_Delete]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_Delete]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_Delete]
	@ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
	DELETE FROM [dbo].[te_SharePoint_List]
	WHERE ApplicationId = @ApplicationId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_Delete] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_Get 
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_Get]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_Get]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_Get]
	@ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 list.Id
		,list.ApplicationId
		,list.ApplicationKey
		,list.TypeId
		,list.SPWebUrl
		,list.GroupId
		,listView.ViewId
	FROM
		[dbo].[te_SharePoint_List] list
	LEFT JOIN
		[dbo].[te_SharePoint_View] listView
	ON
		list.Id = listView.ListId
	WHERE
		list.ApplicationId = @ApplicationId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_Get] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_GetByApplicationKey
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_GetByApplicationKey]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_GetByApplicationKey]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_GetByApplicationKey]
	@ApplicationKey NVARCHAR(256),
	@GroupId INT
AS
BEGIN
	SELECT
		 list.Id
		,list.ApplicationId
		,list.ApplicationKey
		,list.TypeId
		,list.SPWebUrl
		,list.GroupId
		,listView.ViewId
	FROM
		[dbo].[te_SharePoint_List] list
	LEFT JOIN
		[dbo].[te_SharePoint_View] listView
	ON
		list.Id = listView.ListId
	WHERE
		list.GroupId = @GroupId
		AND list.ApplicationKey = @ApplicationKey
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_GetByApplicationKey] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_ListByGroupId 
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_ListByGroupId]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_ListByGroupId]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_ListByGroupId]
	@GroupId INT
AS
BEGIN
	SELECT
		 list.Id
		,list.ApplicationId
		,list.ApplicationKey
		,list.TypeId
		,list.SPWebUrl
		,list.GroupId
		,listView.ViewId
	FROM
		[dbo].[te_SharePoint_List] list
	LEFT JOIN
		[dbo].[te_SharePoint_View] listView
	ON
		list.Id = listView.ListId
	WHERE
		list.GroupId = @GroupId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_ListByGroupId] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_ListByTypeId 
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_ListByTypeId]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_ListByTypeId]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_ListByTypeId]
	@TypeId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 list.Id
		,list.ApplicationId
		,list.ApplicationKey
		,list.TypeId
		,list.SPWebUrl
		,list.GroupId
		,listView.ViewId
	FROM
		[dbo].[te_SharePoint_List] list
	LEFT JOIN
		[dbo].[te_SharePoint_View] listView
	ON
		list.Id = listView.ListId
	WHERE
		list.TypeId = @TypeId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_ListByTypeId] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_ListByGroupIdTypeId
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_ListByGroupIdTypeId]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_ListByGroupIdTypeId]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_List_ListByGroupIdTypeId]
	@GroupId INT,
	@TypeId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 list.Id
		,list.ApplicationId
		,list.ApplicationKey
		,list.TypeId
		,list.SPWebUrl
		,list.GroupId
		,listView.ViewId
	FROM
		[dbo].[te_SharePoint_List] list
	LEFT JOIN
		[dbo].[te_SharePoint_View] listView
	ON
		list.Id = listView.ListId
	WHERE
		list.GroupId = @GroupId
		AND list.TypeId = @TypeId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_ListByGroupIdTypeId] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_List_GetToReindex
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_List_GetToReindex]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_List_GetToReindex]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_List_GetToReindex] 
	@TypeId UNIQUEIDENTIFIER,
	@PagingBegin int,
	@PagingEnd int,
	@TotalRecords int = null output
AS

BEGIN
	SET NOCOUNT ON;

	SET @TotalRecords = 0
	DECLARE @tblPageIndex TABLE
	(
		IndexId int not null PRIMARY KEY CLUSTERED,
		ApplicationId uniqueidentifier NOT NULL,
		TotalRecords int NOT NULL
	)

	INSERT INTO @tblPageIndex (IndexId, ApplicationId, TotalRecords)
	SELECT i.RowId, i.ApplicationId, i.TotalRecords
	FROM (
		SELECT  ROW_NUMBER() OVER (ORDER BY list.UpdatedDate ASC) AS RowId, 
				list.ApplicationId as ApplicationId,
				COUNT(*) OVER () AS TotalRecords
		FROM te_SharePoint_List list
		WHERE list.IsIndexed = 0 AND list.TypeId = @TypeId
		) i
	WHERE i.RowId > @PagingBegin 
		AND i.RowId <= @PagingEnd
	ORDER BY i.RowId ASC
	
	SELECT @TotalRecords = COALESCE(NULLIF((SELECT TOP 1 TotalRecords FROM @tblPageIndex), 0), 0)

	SELECT
		 list.Id
		,list.ApplicationId
		,list.ApplicationKey
		,list.TypeId
		,list.SPWebUrl
		,list.GroupId
		,listView.ViewId
	FROM
		dbo.te_SharePoint_List list
		JOIN
			@tblPageIndex tindex
		ON
			tindex.ApplicationId = list.ApplicationId
		LEFT JOIN
			[dbo].[te_SharePoint_View] listView
		ON
			list.Id = listView.ListId
	ORDER BY
		tindex.IndexId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_List_GetToReindex] TO PUBLIC

/***********************************************
* Sproc: te_SharePoint_Item_AddUpdate
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_AddUpdate]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_AddUpdate]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Item_AddUpdate]
	 @ApplicationId UNIQUEIDENTIFIER
	,@ContentKey NVARCHAR(256) = NULL
	,@ContentId UNIQUEIDENTIFIER
	,@ItemId INT
	,@IsIndexed INT = 0
AS
BEGIN
	UPDATE item
	SET
		 item.ApplicationId = @ApplicationId
		,item.ContentKey = @ContentKey
		,item.ItemId = @ItemId
	FROM
		te_SharePoint_Item item
	WHERE
		item.ContentId = @ContentId
	IF (@@ROWCOUNT = 0)
		AND (EXISTS(SELECT list.Id FROM te_SharePoint_List list WHERE list.ApplicationId = @ApplicationId))
	BEGIN
		INSERT INTO te_SharePoint_Item (ApplicationId, ContentKey, ContentId, ItemId, IsIndexed)
		VALUES (@ApplicationId, @ContentKey, @ContentId, @ItemId, @IsIndexed)
	END
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_AddUpdate] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_AddBatch
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_AddBatch]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_AddBatch]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Item_AddBatch]
	@ItemsXml XML
AS
BEGIN
	DECLARE @NotIndexed BIT
	SET @NotIndexed = 0
	INSERT INTO te_SharePoint_Item (ApplicationId, ContentKey, ContentId, ItemId, IsIndexed)
	SELECT
		 items.item.value('@applicationId', 'UNIQUEIDENTIFIER') AS ApplicationId
		,items.item.value('@contentKey', ' NVARCHAR(256)') AS ContentKey
		,items.item.value('@contentId', 'UNIQUEIDENTIFIER') AS ContentId
		,items.item.value('@id', 'INT') AS ItemId
		,items.item.value('@isIndexed', 'INT') AS IsIndexed
	FROM
		@ItemsXml.nodes('/items/item') AS items(item)
		LEFT JOIN
			te_SharePoint_Item
		ON
			items.item.value('@contentId', 'UNIQUEIDENTIFIER') = te_SharePoint_Item.ContentId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_AddBatch] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_Delete
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_Delete]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_Delete]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Item_Delete]
	@ContentId uniqueidentifier
AS
BEGIN
	DELETE FROM te_SharePoint_Item
	WHERE ContentId = @ContentId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_Delete] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_Get 
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_Get]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_Get]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Item_Get]
	@ContentId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 item.ApplicationId
		,item.ContentId
		,item.ItemId
		,item.ContentKey
		,item.UpdatedDate
	FROM
		te_SharePoint_Item item
	WHERE
		item.ContentId = @ContentId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_Get] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_GetByContentKey
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_GetByContentKey]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_GetByContentKey]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Item_GetByContentKey]
	 @ContentKey NVARCHAR(256)
	,@ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 item.ApplicationId
		,item.ContentId
		,item.ItemId
		,item.ContentKey
		,item.UpdatedDate
	FROM
		te_SharePoint_Item item
	WHERE
		item.ApplicationId = @ApplicationId
		AND item.ContentKey = @ContentKey
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_GetByContentKey] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_GetByItemId
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_GetByItemId]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_GetByItemId]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Item_GetByItemId]
	 @ItemId INT
	,@ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 item.ApplicationId
		,item.ContentId
		,item.ItemId
		,item.ContentKey
		,item.UpdatedDate
	FROM
		te_SharePoint_Item item
	WHERE
		item.ApplicationId = @ApplicationId
		AND item.ItemId = @ItemId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_GetByItemId] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_List.PRC
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_List]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].te_SharePoint_Item_List
GO
CREATE PROCEDURE [dbo].te_SharePoint_Item_List
	@ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
		 item.ApplicationId
		,item.ContentId
		,item.ItemId
		,item.ContentKey
		,item.UpdatedDate
	FROM
		te_SharePoint_Item item
	WHERE
		item.ApplicationId = @ApplicationId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_List] TO PUBLIC
GO

/***********************************************
* Sproc: te_SharePoint_Item_GetToReindex.PRC
* File Date: 11/07/2012 3:16:00 PM
***********************************************/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Item_GetToReindex]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Item_GetToReindex]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_Item_GetToReindex] 
	@EnabledListIds NVARCHAR(max),
	@PagingBegin INT,
	@PagingEnd INT,
	@TotalRecords INT = NULL OUTPUT
AS

BEGIN
	SET NOCOUNT ON;

	SET @TotalRecords = 0

	DECLARE @tblPageIndex TABLE
	(
		IndexId int not null PRIMARY KEY CLUSTERED,
		ContentId uniqueidentifier NOT NULL,
		TotalRecords int NOT NULL
	)

	INSERT INTO @tblPageIndex (IndexId, ContentId, TotalRecords)
	SELECT
		 i.RowId
		,i.ContentId
		,i.TotalRecords
	FROM (
		SELECT
			 ROW_NUMBER() OVER (ORDER BY item.UpdatedDate ASC) AS RowId
			,item.ContentId as ContentId
			,COUNT(*) OVER () AS TotalRecords
		FROM
			te_SharePoint_Item item
		WHERE
			item.IsIndexed = 0
			AND item.ApplicationId IN (SELECT Items FROM dbo.te_SharePoint_SplitString(@EnabledListIds,','))
		) i
	WHERE
		i.RowId > @PagingBegin
		AND i.RowId <= @PagingEnd
	ORDER BY
		i.RowId ASC
	
	SELECT @TotalRecords = COALESCE(NULLIF((SELECT TOP 1 TotalRecords FROM @tblPageIndex), 0), 0)
	
	SELECT
		 item.Id
		,item.ApplicationId
		,item.ContentKey
		,item.ContentId
		,item.ItemId
		,item.UpdatedDate
	FROM
		te_SharePoint_Item item
		JOIN
			@tblPageIndex tindex
		ON
			tindex.ContentId = item.ContentId
	ORDER BY tindex.IndexId
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Item_GetToReindex] TO PUBLIC

/***********************************************
* Sproc: te_SharePoint_Item_UpdateIsIndexed.PRC
* File Date: 11/06/2012 9:49:00 PM
***********************************************/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[te_SharePoint_Item_UpdateIsIndexed]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop PROCEDURE [dbo].[te_SharePoint_Item_UpdateIsIndexed]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_Item_UpdateIsIndexed]
(
	@ContentIds  NVARCHAR(MAX),
	@IsIndexed INT
)
AS
begin
	UPDATE i
	SET
		IsIndexed = @IsIndexed
		,UpdatedDate = GETDATE()
	FROM
		dbo.te_SharePoint_Item i
	WHERE
		IsIndexed <> -1
		AND ContentId IN (SELECT Items FROM dbo.te_SharePoint_SplitString(@ContentIds,','))

end
GO
GRANT EXECUTE ON [dbo].[te_SharePoint_Item_UpdateIsIndexed] TO PUBLIC

/***********************************************
* Sproc: te_SharePoint_List_UpdateIsIndexed.PRC
* File Date: 11/06/2012 9:item49:00 PM
***********************************************/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[te_SharePoint_List_UpdateIsIndexed]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop PROCEDURE [dbo].[te_SharePoint_List_UpdateIsIndexed]
GO

CREATE PROCEDURE [dbo].[te_SharePoint_List_UpdateIsIndexed]
(
	 @ApplicationIds  NVARCHAR(MAX)
	,@IsIndexed BIT
)
AS
begin

	UPDATE l
	SET
		 IsIndexed = @IsIndexed
	FROM
		dbo.te_SharePoint_List l
	WHERE
		ApplicationId IN (SELECT Items FROM dbo.te_SharePoint_SplitString(@ApplicationIds,','))

end
GO
GRANT EXECUTE ON [dbo].[te_SharePoint_List_UpdateIsIndexed] TO PUBLIC

/***********************************************
* Sproc: te_SharePoint_SplitString.FNC
* File Date: 11/06/2012 9:49:00 PM
***********************************************/

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

IF EXISTS(SELECT 1 FROM sys.all_objects WHERE name = 'te_SharePoint_SplitString' AND type = 'TF')
	DROP FUNCTION [dbo].[te_SharePoint_SplitString]
GO

CREATE FUNCTION [dbo].[te_SharePoint_SplitString](@String NVARCHAR(MAX), @Delimiter char(1))
RETURNS @Table TABLE (Items NVARCHAR(MAX))
AS
BEGIN
	DECLARE @idx INT
	DECLARE @slice NVARCHAR(MAX)

	SELECT @idx = 1
		IF LEN(@String)<1 OR @String IS NULL  RETURN

	WHILE @idx!= 0
	BEGIN
		SET @idx = CHARINDEX(@Delimiter,@String)
		IF @idx!=0
			SET @slice = LEFT(@String,@idx - 1)
		ELSE
			SET @slice = @String

		IF(LEN(@slice)>0)
			INSERT INTO @Table(Items) VALUES(@slice)

		SET @String = RIGHT(@String,LEN(@String) - @idx)
		IF LEN(@String) = 0 BREAK
	END
RETURN
END

GO


/***********************************************
* Sproc: te_SharePoint_Node_AddUpdate
***********************************************/
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[te_SharePoint_Node_AddUpdate]') AND type in (N'P', N'PC'))
	DROP PROCEDURE [dbo].[te_SharePoint_Node_AddUpdate]
GO
CREATE PROCEDURE [dbo].[te_SharePoint_Node_AddUpdate]
	 @ApplicationId UNIQUEIDENTIFIER
	,@ApplicationTypeId UNIQUEIDENTIFIER
	,@GroupApplicationId UNIQUEIDENTIFIER
AS
BEGIN

	UPDATE [dbo].[cs_Nodes]
	SET
		 ParentNodeId = @GroupApplicationId
		,ApplicationTypeId = @ApplicationTypeId
	WHERE
		NodeId = @ApplicationId

	IF @@ROWCOUNT = 0
	BEGIN
		INSERT INTO dbo.cs_Nodes (NodeId, ParentNodeId, ApplicationTypeId)
		VALUES (@ApplicationId, @GroupApplicationId, @ApplicationTypeId)
	END
END
GO

GRANT EXECUTE ON [dbo].[te_SharePoint_Node_AddUpdate] TO PUBLIC
GO