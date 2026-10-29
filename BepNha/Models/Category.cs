using System;

namespace CookingCommunity.Web.Models;

public class Category : ISoftDeletable
{
	public bool IsDeleted { get; set; }

	public DateTime? DeletedAt { get; set; }

	public int CategoryId { get; set; }

	public string CategoryName { get; set; } = "";
}
