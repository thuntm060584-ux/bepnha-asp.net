using System;

namespace CookingCommunity.Web.Models;

public class Review : ISoftDeletable
{
	public bool IsDeleted { get; set; }

	public DateTime? DeletedAt { get; set; }

	public int ReviewId { get; set; }

	public int RecipeId { get; set; }

	public Recipe Recipe { get; set; } = null!;

	public int ReviewerId { get; set; }

	public User Reviewer { get; set; } = null!;

	public int Rating { get; set; }

	public string Comment { get; set; } = "";

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
