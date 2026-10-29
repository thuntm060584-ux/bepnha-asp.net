using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CookingCommunity.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CookingCommunity.Web.Data;

public class CookingDbContext : DbContext
{
	public DbSet<User> Users => Set<User>();

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Recipe> Recipes => Set<Recipe>();

	public DbSet<Ingredient> Ingredients => Set<Ingredient>();

	public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

	public DbSet<Review> Reviews => Set<Review>();

	public DbSet<Follow> Follows => Set<Follow>();

	public CookingDbContext(DbContextOptions<CookingDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder b)
	{
		b.Entity(delegate(EntityTypeBuilder<User> e)
		{
			e.ToTable("users", delegate(TableBuilder<User> t)
			{
				t.HasCheckConstraint("CK_users_role", "role IN ('admin','member')");
			});
			e.HasKey((User x) => x.UserId);
			e.Property((User x) => x.FullName).HasMaxLength(100);
			e.Property((User x) => x.Email).HasMaxLength(254);
			e.HasIndex((User x) => x.Email).IsUnique();
			e.Property((User x) => x.PasswordHash).HasMaxLength(200);
			e.Property((User x) => x.Bio).HasMaxLength(1000);
			e.Property((User x) => x.Role).HasMaxLength(10);
			e.Property((User x) => x.ResetToken).HasMaxLength(64);
		});
		b.Entity(delegate(EntityTypeBuilder<Category> e)
		{
			e.ToTable("categories");
			e.HasKey((Category x) => x.CategoryId);
			e.Property((Category x) => x.CategoryName).HasMaxLength(100);
			e.HasIndex((Category x) => x.CategoryName).IsUnique();
		});
		b.Entity(delegate(EntityTypeBuilder<Ingredient> e)
		{
			e.ToTable("ingredients");
			e.HasKey((Ingredient x) => x.IngredientId);
			e.Property((Ingredient x) => x.IngredientName).HasMaxLength(100);
			e.HasIndex((Ingredient x) => x.IngredientName).IsUnique();
		});
		b.Entity(delegate(EntityTypeBuilder<Recipe> e)
		{
			e.ToTable("recipes", delegate(TableBuilder<Recipe> t)
			{
				t.HasCheckConstraint("CK_recipes_hidden", "is_published = 1 OR (hidden_reason IS NOT NULL AND LEN(hidden_reason) BETWEEN 5 AND 300)");
				t.HasCheckConstraint("CK_recipes_values", "difficulty BETWEEN 1 AND 3 AND cook_minutes > 0 AND servings > 0");
			});
			e.HasKey((Recipe x) => x.RecipeId);
			e.Property((Recipe x) => x.Title).HasMaxLength(200);
			e.Property((Recipe x) => x.Summary).HasMaxLength(2000);
			e.Property((Recipe x) => x.CoverImage).HasMaxLength(300);
			e.Property((Recipe x) => x.HiddenReason).HasMaxLength(300);
			e.Ignore((Recipe x) => x.AverageRating);
			e.Ignore((Recipe x) => x.DifficultyText);
			e.HasOne((Recipe x) => x.Author).WithMany().HasForeignKey((Recipe x) => x.AuthorId)
				.OnDelete(DeleteBehavior.NoAction);
			e.HasOne((Recipe x) => x.Category).WithMany().HasForeignKey((Recipe x) => x.CategoryId)
				.OnDelete(DeleteBehavior.NoAction);
			e.HasIndex((Recipe x) => new { x.IsPublished, x.CreatedAt });
		});
		b.Entity(delegate(EntityTypeBuilder<RecipeIngredient> e)
		{
			e.ToTable("recipe_ingredients");
			e.HasKey((RecipeIngredient x) => new { x.RecipeId, x.IngredientId });
			e.Property((RecipeIngredient x) => x.Quantity).HasMaxLength(100);
			e.HasOne((RecipeIngredient x) => x.Recipe).WithMany((Recipe x) => x.RecipeIngredients).HasForeignKey((RecipeIngredient x) => x.RecipeId)
				.OnDelete(DeleteBehavior.Cascade);
			e.HasOne((RecipeIngredient x) => x.Ingredient).WithMany().HasForeignKey((RecipeIngredient x) => x.IngredientId)
				.OnDelete(DeleteBehavior.NoAction);
		});
		b.Entity(delegate(EntityTypeBuilder<Review> e)
		{
			e.ToTable("reviews", delegate(TableBuilder<Review> t)
			{
				t.HasCheckConstraint("CK_reviews_rating", "rating BETWEEN 1 AND 5");
			});
			e.HasKey((Review x) => x.ReviewId);
			e.Property((Review x) => x.Comment).HasMaxLength(1000);
			e.HasIndex((Review x) => new { x.RecipeId, x.ReviewerId }).IsUnique();
			e.HasOne((Review x) => x.Recipe).WithMany((Recipe x) => x.Reviews).HasForeignKey((Review x) => x.RecipeId)
				.OnDelete(DeleteBehavior.Cascade);
			e.HasOne((Review x) => x.Reviewer).WithMany().HasForeignKey((Review x) => x.ReviewerId)
				.OnDelete(DeleteBehavior.NoAction);
		});
		b.Entity(delegate(EntityTypeBuilder<Follow> e)
		{
			e.ToTable("follows", delegate(TableBuilder<Follow> t)
			{
				t.HasCheckConstraint("CK_follows_self", "follower_id <> following_id");
			});
			e.HasKey((Follow x) => new { x.FollowerId, x.FollowingId });
			e.HasOne((Follow x) => x.Follower).WithMany().HasForeignKey((Follow x) => x.FollowerId)
				.OnDelete(DeleteBehavior.NoAction);
			e.HasOne((Follow x) => x.Following).WithMany().HasForeignKey((Follow x) => x.FollowingId)
				.OnDelete(DeleteBehavior.NoAction);
		});
		b.Entity<Category>().HasQueryFilter((Category x) => !x.IsDeleted);
		b.Entity<Recipe>().HasQueryFilter((Recipe x) => !x.IsDeleted && !x.Category.IsDeleted);
		b.Entity<Review>().HasQueryFilter((Review x) => !x.IsDeleted && !x.Recipe.IsDeleted && !x.Recipe.Category.IsDeleted);
		b.Entity<RecipeIngredient>().HasQueryFilter((RecipeIngredient x) => !x.IsDeleted && !x.Recipe.IsDeleted && !x.Recipe.Category.IsDeleted);
		b.Entity<Follow>().HasQueryFilter((Follow x) => !x.IsDeleted);
		b.Entity<Category>().Property((Category x) => x.IsDeleted).HasDefaultValue(false);
		b.Entity<Recipe>().Property((Recipe x) => x.IsDeleted).HasDefaultValue(false);
		b.Entity<Review>().Property((Review x) => x.IsDeleted).HasDefaultValue(false);
		b.Entity<RecipeIngredient>().Property((RecipeIngredient x) => x.IsDeleted).HasDefaultValue(false);
		b.Entity<Follow>().Property((Follow x) => x.IsDeleted).HasDefaultValue(false);
		foreach (IMutableEntityType entityType in b.Model.GetEntityTypes())
		{
			foreach (IMutableProperty property in entityType.GetProperties())
			{
				property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
				if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTime))
				{
					property.SetColumnType("datetime");
				}
			}
		}
	}

	private void GuardAgainstHardDeletes()
	{
		ChangeTracker.DetectChanges();
		if (ChangeTracker.Entries().Any((EntityEntry x) => x.State == EntityState.Deleted))
		{
			throw new InvalidOperationException("Không được xóa vật lý dữ liệu nghiệp vụ. Hãy sử dụng xóa mềm.");
		}
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		GuardAgainstHardDeletes();
		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
	{
		GuardAgainstHardDeletes();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}
}
