using System;

namespace CookingCommunity.Web.Models;

public class RecipeIngredient : ISoftDeletable
{
	public bool IsDeleted { get; set; }

	public DateTime? DeletedAt { get; set; }

	public int RecipeId { get; set; }

	public Recipe Recipe { get; set; } = null!;

	public int IngredientId { get; set; }

	public Ingredient Ingredient { get; set; } = null!;

	public string Quantity { get; set; } = "";
}
