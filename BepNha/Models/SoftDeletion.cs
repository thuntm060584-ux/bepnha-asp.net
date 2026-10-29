using System;

namespace CookingCommunity.Web.Models;

public static class SoftDeletion
{
	public static void SoftDelete(this ISoftDeletable entity)
	{
		entity.IsDeleted = true;
		entity.DeletedAt = DateTime.UtcNow;
	}

	public static void UndoDelete(this ISoftDeletable entity)
	{
		entity.IsDeleted = false;
		entity.DeletedAt = null;
	}
}
