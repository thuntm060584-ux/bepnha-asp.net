CREATE TABLE [categories] (
    [category_id] int NOT NULL IDENTITY,
    [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [deleted_at] datetime NULL,
    [category_name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_categories] PRIMARY KEY ([category_id])
);
GO


CREATE TABLE [ingredients] (
    [ingredient_id] int NOT NULL IDENTITY,
    [ingredient_name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_ingredients] PRIMARY KEY ([ingredient_id])
);
GO


CREATE TABLE [users] (
    [user_id] int NOT NULL IDENTITY,
    [full_name] nvarchar(100) NOT NULL,
    [email] nvarchar(254) NOT NULL,
    [password_hash] nvarchar(200) NOT NULL,
    [bio] nvarchar(1000) NOT NULL,
    [role] nvarchar(10) NOT NULL,
    [is_active] bit NOT NULL,
    [reset_token] nvarchar(64) NULL,
    [created_at] datetime NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([user_id]),
    CONSTRAINT [CK_users_role] CHECK (role IN ('admin','member'))
);
GO


CREATE TABLE [follows] (
    [follower_id] int NOT NULL,
    [following_id] int NOT NULL,
    [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [deleted_at] datetime NULL,
    [followed_at] datetime NOT NULL,
    CONSTRAINT [PK_follows] PRIMARY KEY ([follower_id], [following_id]),
    CONSTRAINT [CK_follows_self] CHECK (follower_id <> following_id),
    CONSTRAINT [FK_follows_users_follower_id] FOREIGN KEY ([follower_id]) REFERENCES [users] ([user_id]),
    CONSTRAINT [FK_follows_users_following_id] FOREIGN KEY ([following_id]) REFERENCES [users] ([user_id])
);
GO


CREATE TABLE [recipes] (
    [recipe_id] int NOT NULL IDENTITY,
    [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [deleted_at] datetime NULL,
    [author_id] int NOT NULL,
    [category_id] int NOT NULL,
    [title] nvarchar(200) NOT NULL,
    [summary] nvarchar(2000) NOT NULL,
    [cover_image] nvarchar(300) NOT NULL,
    [difficulty] int NOT NULL,
    [cook_minutes] int NOT NULL,
    [servings] int NOT NULL,
    [instructions] nvarchar(max) NOT NULL,
    [is_published] bit NOT NULL,
    [hidden_reason] nvarchar(300) NULL,
    [created_at] datetime NOT NULL,
    [updated_at] datetime NULL,
    CONSTRAINT [PK_recipes] PRIMARY KEY ([recipe_id]),
    CONSTRAINT [CK_recipes_hidden] CHECK (is_published = 1 OR (hidden_reason IS NOT NULL AND LEN(hidden_reason) BETWEEN 5 AND 300)),
    CONSTRAINT [CK_recipes_values] CHECK (difficulty BETWEEN 1 AND 3 AND cook_minutes > 0 AND servings > 0),
    CONSTRAINT [FK_recipes_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [categories] ([category_id]),
    CONSTRAINT [FK_recipes_users_author_id] FOREIGN KEY ([author_id]) REFERENCES [users] ([user_id])
);
GO


CREATE TABLE [recipe_ingredients] (
    [recipe_id] int NOT NULL,
    [ingredient_id] int NOT NULL,
    [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [deleted_at] datetime NULL,
    [quantity] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_recipe_ingredients] PRIMARY KEY ([recipe_id], [ingredient_id]),
    CONSTRAINT [FK_recipe_ingredients_ingredients_ingredient_id] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([ingredient_id]),
    CONSTRAINT [FK_recipe_ingredients_recipes_recipe_id] FOREIGN KEY ([recipe_id]) REFERENCES [recipes] ([recipe_id]) ON DELETE CASCADE
);
GO


CREATE TABLE [reviews] (
    [review_id] int NOT NULL IDENTITY,
    [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [deleted_at] datetime NULL,
    [recipe_id] int NOT NULL,
    [reviewer_id] int NOT NULL,
    [rating] int NOT NULL,
    [comment] nvarchar(1000) NOT NULL,
    [created_at] datetime NOT NULL,
    CONSTRAINT [PK_reviews] PRIMARY KEY ([review_id]),
    CONSTRAINT [CK_reviews_rating] CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT [FK_reviews_recipes_recipe_id] FOREIGN KEY ([recipe_id]) REFERENCES [recipes] ([recipe_id]) ON DELETE CASCADE,
    CONSTRAINT [FK_reviews_users_reviewer_id] FOREIGN KEY ([reviewer_id]) REFERENCES [users] ([user_id])
);
GO


CREATE UNIQUE INDEX [IX_categories_category_name] ON [categories] ([category_name]);
GO


CREATE INDEX [IX_follows_following_id] ON [follows] ([following_id]);
GO


CREATE UNIQUE INDEX [IX_ingredients_ingredient_name] ON [ingredients] ([ingredient_name]);
GO


CREATE INDEX [IX_recipe_ingredients_ingredient_id] ON [recipe_ingredients] ([ingredient_id]);
GO


CREATE INDEX [IX_recipes_author_id] ON [recipes] ([author_id]);
GO


CREATE INDEX [IX_recipes_category_id] ON [recipes] ([category_id]);
GO


CREATE INDEX [IX_recipes_is_published_created_at] ON [recipes] ([is_published], [created_at]);
GO


CREATE UNIQUE INDEX [IX_reviews_recipe_id_reviewer_id] ON [reviews] ([recipe_id], [reviewer_id]);
GO


CREATE INDEX [IX_reviews_reviewer_id] ON [reviews] ([reviewer_id]);
GO


CREATE UNIQUE INDEX [IX_users_email] ON [users] ([email]);
GO


