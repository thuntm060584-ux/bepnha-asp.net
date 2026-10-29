-- Idempotent, additive upgrade. Keeps every existing row, key and uploaded file.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock_result int;
    EXEC @lock_result = sys.sp_getapplock @Resource=N'CookingCommunity.Schema.SoftDelete', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000;
    IF @lock_result < 0 THROW 50001, N'Không lấy được khóa nâng cấp cơ sở dữ liệu.', 1;
    IF COL_LENGTH('dbo.categories', 'is_deleted') IS NULL ALTER TABLE dbo.categories ADD is_deleted bit NOT NULL CONSTRAINT DF_categories_is_deleted DEFAULT (0);
    IF COL_LENGTH('dbo.categories', 'deleted_at') IS NULL ALTER TABLE dbo.categories ADD deleted_at datetime NULL;
    IF COL_LENGTH('dbo.recipes', 'is_deleted') IS NULL ALTER TABLE dbo.recipes ADD is_deleted bit NOT NULL CONSTRAINT DF_recipes_is_deleted DEFAULT (0);
    IF COL_LENGTH('dbo.recipes', 'deleted_at') IS NULL ALTER TABLE dbo.recipes ADD deleted_at datetime NULL;
    IF COL_LENGTH('dbo.reviews', 'is_deleted') IS NULL ALTER TABLE dbo.reviews ADD is_deleted bit NOT NULL CONSTRAINT DF_reviews_is_deleted DEFAULT (0);
    IF COL_LENGTH('dbo.reviews', 'deleted_at') IS NULL ALTER TABLE dbo.reviews ADD deleted_at datetime NULL;
    IF COL_LENGTH('dbo.recipe_ingredients', 'is_deleted') IS NULL ALTER TABLE dbo.recipe_ingredients ADD is_deleted bit NOT NULL CONSTRAINT DF_recipe_ingredients_is_deleted DEFAULT (0);
    IF COL_LENGTH('dbo.recipe_ingredients', 'deleted_at') IS NULL ALTER TABLE dbo.recipe_ingredients ADD deleted_at datetime NULL;
    IF COL_LENGTH('dbo.follows', 'is_deleted') IS NULL ALTER TABLE dbo.follows ADD is_deleted bit NOT NULL CONSTRAINT DF_follows_is_deleted DEFAULT (0);
    IF COL_LENGTH('dbo.follows', 'deleted_at') IS NULL ALTER TABLE dbo.follows ADD deleted_at datetime NULL;
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
