using System;

namespace CookingCommunity.Web.Models;

public class Follow : ISoftDeletable
{
	public bool IsDeleted { get; set; }

	public DateTime? DeletedAt { get; set; }

	public int FollowerId { get; set; }

	public User Follower { get; set; } = null!;

	public int FollowingId { get; set; }

	public User Following { get; set; } = null!;

	public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}
