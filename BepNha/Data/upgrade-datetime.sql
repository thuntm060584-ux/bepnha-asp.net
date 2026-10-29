-- Chuyển các cột ngày giờ sang datetime, giữ dữ liệu và tính cho phép NULL.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock_result int;
    EXEC @lock_result = sys.sp_getapplock @Resource=N'CookingCommunity.Schema.DateTime', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000;
    IF @lock_result < 0 THROW 50002, N'Không lấy được khóa nâng cấp kiểu ngày giờ.', 1;

    DECLARE @columns TABLE (table_name sysname, column_name sysname);
    INSERT INTO @columns VALUES
        ('categories', 'deleted_at'), ('users', 'created_at'),
        ('follows', 'deleted_at'), ('follows', 'followed_at'),
        ('recipes', 'deleted_at'), ('recipes', 'created_at'), ('recipes', 'updated_at'),
        ('recipe_ingredients', 'deleted_at'), ('reviews', 'deleted_at'), ('reviews', 'created_at');

    DECLARE @table sysname, @column sysname, @nullable bit, @sql nvarchar(max), @recreate_index bit = 0;
    DECLARE date_columns CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.name, c.name, c.is_nullable
        FROM @columns expected
        JOIN sys.tables t ON t.name = expected.table_name AND t.schema_id = SCHEMA_ID('dbo')
        JOIN sys.columns c ON c.object_id = t.object_id AND c.name = expected.column_name
        WHERE c.system_type_id <> TYPE_ID('datetime');
    OPEN date_columns;
    FETCH NEXT FROM date_columns INTO @table, @column, @nullable;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Dừng và rollback nếu có ngày ngoài phạm vi datetime; không tự sửa dữ liệu.
        SET @sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@table)
            + N' WHERE ' + QUOTENAME(@column) + N' IS NOT NULL AND TRY_CONVERT(datetime, '
            + QUOTENAME(@column) + N') IS NULL) THROW 50003, N''Có ngày ngoài phạm vi datetime tại '
            + @table + N'.' + @column + N'.'', 1;';
        EXEC sys.sp_executesql @sql;

        IF @table = 'recipes' AND @column = 'created_at'
           AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.recipes') AND name = 'IX_recipes_is_published_created_at')
        BEGIN
            DROP INDEX IX_recipes_is_published_created_at ON dbo.recipes;
            SET @recreate_index = 1;
        END;
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@table) + N' ALTER COLUMN '
            + QUOTENAME(@column) + N' datetime ' + CASE WHEN @nullable = 1 THEN N'NULL;' ELSE N'NOT NULL;' END;
        EXEC sys.sp_executesql @sql;
        FETCH NEXT FROM date_columns INTO @table, @column, @nullable;
    END;
    CLOSE date_columns;
    DEALLOCATE date_columns;

    IF @recreate_index = 1
        CREATE INDEX IX_recipes_is_published_created_at ON dbo.recipes (is_published, created_at);
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
