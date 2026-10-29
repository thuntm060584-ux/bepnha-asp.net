using System;
using System.Collections.Generic;
using System.Linq;

namespace CookingCommunity.Web.Models;

public class Recipe : ISoftDeletable
{
	public bool IsDeleted { get; set; }

	public DateTime? DeletedAt { get; set; }

	public int RecipeId { get; set; }

	public int AuthorId { get; set; }

	public User Author { get; set; } = null!;

	public int CategoryId { get; set; }

	public Category Category { get; set; } = null!;

	public string Title { get; set; } = "";

	public string Summary { get; set; } = "";

	public string CoverImage { get; set; } = "";

	public int Difficulty { get; set; }

	public int CookMinutes { get; set; }

	public int Servings { get; set; }

	public string Instructions { get; set; } = "";

	public bool IsPublished { get; set; } = true;

	public string? HiddenReason { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime? UpdatedAt { get; set; }

	public List<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

	public List<Review> Reviews { get; set; } = new List<Review>();

	public double AverageRating
	{
		get
		{
			if (Reviews.Count != 0)
			{
				return Math.Round(Reviews.Average((Review x) => x.Rating), 1);
			}
			return 0.0;
		}
	}

	public string DifficultyText => Difficulty switch
	{
		1 => "Dễ", 
		2 => "Trung bình", 
		_ => "Khó", 
	};
}
