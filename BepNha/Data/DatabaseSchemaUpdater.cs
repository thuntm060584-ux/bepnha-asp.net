using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Data;

public static class DatabaseSchemaUpdater
{
	public static async Task Apply(CookingDbContext db)
	{
		foreach (var resource in new[] { "CookingCommunity.UpgradeSoftDelete.sql", "CookingCommunity.UpgradeDateTime.sql", "CookingCommunity.UpgradeRecipeImages.sql" })
		{
			await using Stream stream = typeof(DatabaseSchemaUpdater).Assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"Thiếu script nâng cấp cơ sở dữ liệu: {resource}");
			using StreamReader reader = new StreamReader(stream);
			await db.Database.ExecuteSqlRawAsync(await reader.ReadToEndAsync());
		}
	}
}
