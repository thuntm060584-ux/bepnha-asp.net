using System.ComponentModel.DataAnnotations;
using CookingCommunity.Web.Models;
using CookingCommunity.Web.Data;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Controllers;

[Authorize(Roles = "member"), Route("reviews")]
public class ReviewsController(CookingDbContext db) : Controller
{
    [HttpPost("")]
    public async Task<IActionResult> Save(int recipeId, [Range(1,5)] int rating, [StringLength(1000)] string? comment)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Chọn 1–5 sao; nhận xét tối đa 1000 ký tự." });
        var recipe = await db.Recipes.FindAsync(recipeId); if (recipe is null || !recipe.IsPublished) return NotFound();
        var userId = SessionService.UserId(User); if (recipe.AuthorId == userId) return BadRequest(new { error = "Không thể tự đánh giá công thức của mình." });
        var review = await db.Reviews.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.RecipeId == recipeId && x.ReviewerId == userId);
        if (review is null) { review = new Review { RecipeId = recipeId, ReviewerId = userId }; db.Reviews.Add(review); }
        review.UndoDelete(); review.Rating = rating; review.Comment = (comment ?? "").Trim();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Đánh giá vừa thay đổi. Vui lòng tải lại trang." }); }
        return await Result(recipeId);
    }
    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await db.Reviews.FindAsync(id); if (review is null) return NotFound();
        if (review.ReviewerId != SessionService.UserId(User)) return Forbid();
        review.SoftDelete(); await db.SaveChangesAsync(); return await Result(review.RecipeId);
    }
    private async Task<IActionResult> Result(int id)
    {
        var reviews = db.Reviews.Where(x => x.RecipeId == id);
        return Json(new { average = Math.Round(await reviews.AverageAsync(x => (double?)x.Rating) ?? 0, 1), count = await reviews.CountAsync(), redirect = "/recipes/" + id });
    }
}
