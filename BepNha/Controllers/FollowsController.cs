using CookingCommunity.Web.Models;
using CookingCommunity.Web.Data;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Controllers;

[Authorize(Roles = "member"), Route("follows")]
public class FollowsController(CookingDbContext db) : Controller
{
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(int followingId)
    {
        var id = SessionService.UserId(User);
        if (id == followingId) return BadRequest(new { error = "Không thể tự theo dõi chính mình." });
        if (!await db.Users.AnyAsync(x => x.UserId == followingId && x.IsActive && x.Role == "member")) return NotFound();
        var follow = await db.Follows.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.FollowerId == id && x.FollowingId == followingId); bool following = follow is null || follow.IsDeleted;
        if (follow is null) db.Follows.Add(new Follow { FollowerId = id, FollowingId = followingId });
        else if (following) { follow.UndoDelete(); follow.FollowedAt = DateTime.UtcNow; }
        else follow.SoftDelete();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Trạng thái vừa thay đổi. Vui lòng tải lại trang." }); }
        return Json(new { following, count = await db.Follows.CountAsync(x => x.FollowingId == followingId) });
    }
}
