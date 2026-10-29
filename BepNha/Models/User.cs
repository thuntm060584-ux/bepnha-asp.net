using System;

namespace CookingCommunity.Web.Models;

public class User
{
	public int UserId { get; set; }

	public string FullName { get; set; } = "";

	public string Email { get; set; } = "";

	public string PasswordHash { get; set; } = "";

	public string Bio { get; set; } = "";

	public string Role { get; set; } = "member";

	public bool IsActive { get; set; } = true;

	public string? ResetToken { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
