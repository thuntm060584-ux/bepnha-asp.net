using CookingCommunity.Web.Models;
using CookingCommunity.Web.Data;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Controllers;

[Authorize(Roles = "admin"), Route("admin")]
public class AdminController(CookingDbContext db) : Controller
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await db.Users.SingleAsync(x => x.UserId == SessionService.UserId(User));
        ViewBag.Email = user.Email;
        ViewBag.CreatedAt = user.CreatedAt;
        return View(new ProfileForm { FullName = user.FullName, Bio = user.Bio });
    }
    [HttpPost("profile")]
    public async Task<IActionResult> Profile(ProfileForm form)
    {
        var user = await db.Users.SingleAsync(x => x.UserId == SessionService.UserId(User));
        ViewBag.Email = user.Email;
        ViewBag.CreatedAt = user.CreatedAt;
        if (!ModelState.IsValid) return View(form);
        user.FullName = form.FullName.Trim();
        user.Bio = (form.Bio ?? "").Trim();
        await db.SaveChangesAsync();
        await SessionService.SignIn(HttpContext, user);
        TempData["Success"] = "Đã cập nhật hồ sơ quản trị viên.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet(""), HttpGet("recipes")]
    public async Task<IActionResult> Recipes(string? q, string status = "active", int page = 1)
    {
        var query = db.Recipes.IgnoreQueryFilters().AsNoTracking();
        query = status switch {
            "deleted" => query.Where(x => x.IsDeleted),
            "all" => query,
            "published" => query.Where(x => !x.IsDeleted && x.IsPublished),
            "hidden" => query.Where(x => !x.IsDeleted && !x.IsPublished),
            _ => query.Where(x => !x.IsDeleted)
        };
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Title.Contains(q) || x.Author.FullName.Contains(q));
        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / 12d)); page = Math.Clamp(page, 1, pages);
        return View(new AdminRecipesModel { Q = q, Status = status, Page = page, Pages = pages, Total = total,
            Recipes = await query.Include(x => x.Author).Include(x => x.Category).Include(x => x.Reviews.Where(r => !r.IsDeleted))
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.RecipeId).Skip((page - 1) * 12).Take(12).ToListAsync() });
    }
    [HttpGet("recipes/{id:int}")]
    public async Task<IActionResult> Recipe(int id)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters().AsNoTracking().AsSplitQuery()
            .Include(x => x.Author).Include(x => x.Category)
            .Include(x => x.RecipeIngredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Ingredient)
            .Include(x => x.Reviews).ThenInclude(x => x.Reviewer)
            .SingleOrDefaultAsync(x => x.RecipeId == id);
        return recipe is null ? NotFound() : View(recipe);
    }
    [HttpPost("recipes/{id:int}/hide")]
    public async Task<IActionResult> Hide(int id, string? reason)
    {
        var r = await db.Recipes.SingleOrDefaultAsync(x => x.RecipeId == id); if (r is null) return NotFound();
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 300) TempData["Error"] = "Lý do ẩn phải từ 5 đến 300 ký tự.";
        else { r.IsPublished = false; r.HiddenReason = reason.Trim(); r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(); TempData["Success"] = "Đã ẩn công thức."; }
        return RedirectToAction(nameof(Recipes));
    }
    [HttpPost("recipes/{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var r = await db.Recipes.SingleOrDefaultAsync(x => x.RecipeId == id); if (r is null) return NotFound();
        r.IsPublished = true; r.HiddenReason = null; r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        TempData["Success"] = "Đã hiển thị lại công thức."; return RedirectToAction(nameof(Recipes));
    }
    [HttpPost("recipes/{id:int}/delete")]
    public async Task<IActionResult> DeleteRecipe(int id)
    {
        var r = await db.Recipes.SingleOrDefaultAsync(x => x.RecipeId == id); if (r is null) return NotFound();
        r.SoftDelete(); r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        TempData["Success"] = "Đã chuyển công thức vào mục Đã xóa. Ảnh, nguyên liệu và đánh giá vẫn được giữ lại.";
        return RedirectToAction(nameof(Recipes));
    }
    [HttpPost("recipes/{id:int}/undelete")]
    public async Task<IActionResult> UndeleteRecipe(int id)
    {
        var r = await db.Recipes.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.RecipeId == id && x.IsDeleted);
        if (r is null) return NotFound();
        if (!await db.Categories.AnyAsync(x => x.CategoryId == r.CategoryId))
        { TempData["Error"] = "Khôi phục danh mục trước khi khôi phục công thức."; return RedirectToAction(nameof(Recipes), new { status = "deleted" }); }
        r.UndoDelete(); r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        TempData["Success"] = r.IsPublished ? "Đã khôi phục công thức." : "Đã khôi phục công thức; công thức vẫn giữ trạng thái đang ẩn.";
        return RedirectToAction(nameof(Recipes));
    }
    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews(string? q, string status = "active", int page = 1)
    {
        var query = db.Reviews.IgnoreQueryFilters().AsNoTracking();
        query = status switch { "deleted" => query.Where(x => x.IsDeleted), "all" => query, _ => query.Where(x => !x.IsDeleted && !x.Recipe.IsDeleted) };
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Comment.Contains(q) || x.Reviewer.FullName.Contains(q) || x.Recipe.Title.Contains(q));
        var total = await query.CountAsync(); var pages = Math.Max(1, (int)Math.Ceiling(total / 20d)); page = Math.Clamp(page, 1, pages);
        return View(new AdminReviewsModel { Q = q, Status = status, Page = page, Pages = pages, Total = total,
            Reviews = await query.Include(x => x.Recipe).Include(x => x.Reviewer).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.ReviewId).Skip((page - 1) * 20).Take(20).ToListAsync() });
    }
    [HttpPost("reviews/{id:int}/delete")]
    public async Task<IActionResult> DeleteReview(int id, string? returnUrl)
    {
        var r = await db.Reviews.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.ReviewId == id && !x.IsDeleted); if (r is null) return NotFound();
        r.SoftDelete(); await db.SaveChangesAsync(); TempData["Success"] = "Đã xóa mềm đánh giá.";
        return LocalRedirect(Url.IsLocalUrl(returnUrl) && returnUrl!.StartsWith("/admin/", StringComparison.Ordinal) ? returnUrl : "/admin/reviews");
    }
    [HttpPost("reviews/{id:int}/undelete")]
    public async Task<IActionResult> UndeleteReview(int id)
    {
        var r = await db.Reviews.IgnoreQueryFilters().Include(x => x.Recipe).SingleOrDefaultAsync(x => x.ReviewId == id && x.IsDeleted);
        if (r is null) return NotFound();
        if (r.Recipe.IsDeleted) TempData["Error"] = "Hãy khôi phục công thức trước khi khôi phục đánh giá.";
        else { r.UndoDelete(); await db.SaveChangesAsync(); TempData["Success"] = "Đã khôi phục đánh giá."; }
        return RedirectToAction(nameof(Reviews));
    }
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(string? q, string status = "active")
    {
        ViewBag.Q = q; ViewBag.Status = status;
        var query = db.Categories.IgnoreQueryFilters().AsNoTracking();
        query = status == "deleted" ? query.Where(x => x.IsDeleted) : status == "all" ? query : query.Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.CategoryName.Contains(q));
        return View(await query.OrderBy(x => x.CategoryName).ToListAsync());
    }
    [HttpPost("categories/save")]
    public async Task<IActionResult> SaveCategory(int? id, string? name)
    {
        name = System.Text.RegularExpressions.Regex.Replace((name ?? "").Trim(), @"\s+", " ");
        if (name.Length is < 1 or > 100) TempData["Error"] = "Tên danh mục phải từ 1 đến 100 ký tự.";
        else if (await db.Categories.IgnoreQueryFilters().AnyAsync(x => x.CategoryName == name && x.CategoryId != id))
            TempData["Error"] = "Tên danh mục đã tồn tại, có thể nằm trong mục Đã xóa. Hãy khôi phục danh mục đó.";
        else
        {
            var c = id.HasValue ? await db.Categories.SingleOrDefaultAsync(x => x.CategoryId == id.Value) : new Category(); if (c is null) return NotFound();
            c.CategoryName = name; if (!id.HasValue) db.Categories.Add(c);
            try { await db.SaveChangesAsync(); TempData["Success"] = "Đã lưu danh mục."; }
            catch (DbUpdateException) { TempData["Error"] = "Danh mục đã tồn tại hoặc vừa thay đổi. Hãy thử lại."; }
        }
        return RedirectToAction(nameof(Categories));
    }
    [HttpPost("categories/{id:int}/delete")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var c = await db.Categories.SingleOrDefaultAsync(x => x.CategoryId == id); if (c is null) return NotFound();
        if (await db.Recipes.IgnoreQueryFilters().AnyAsync(x => x.CategoryId == id))
            TempData["Error"] = "Không thể xóa danh mục đang được công thức tham chiếu, kể cả công thức trong mục Đã xóa.";
        else { c.SoftDelete(); await db.SaveChangesAsync(); TempData["Success"] = "Đã xóa mềm danh mục."; }
        return RedirectToAction(nameof(Categories));
    }
    [HttpPost("categories/{id:int}/undelete")]
    public async Task<IActionResult> UndeleteCategory(int id)
    {
        var c = await db.Categories.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.CategoryId == id && x.IsDeleted); if (c is null) return NotFound();
        c.UndoDelete(); await db.SaveChangesAsync(); TempData["Success"] = "Đã khôi phục danh mục."; return RedirectToAction(nameof(Categories));
    }
    [HttpGet("users")]
    public async Task<IActionResult> Users(string? q, string status = "all")
    {
        ViewBag.Q = q; ViewBag.Status = status; var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.FullName.Contains(q) || x.Email.Contains(q));
        if (status == "locked") query = query.Where(x => !x.IsActive);
        if (status == "active") query = query.Where(x => x.IsActive);
        return View(await query.OrderBy(x => x.UserId).ToListAsync());
    }
    [HttpPost("users/{id:int}/toggle")]
    public async Task<IActionResult> ToggleUser(int id)
    {
        if (id == SessionService.UserId(User)) { TempData["Error"] = "Bạn không thể khóa chính mình."; return RedirectToAction(nameof(Users)); }
        var u = await db.Users.FindAsync(id); if (u is null) return NotFound();
        u.IsActive = !u.IsActive; await db.SaveChangesAsync(); TempData["Success"] = u.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản và vô hiệu hóa phiên đăng nhập.";
        return RedirectToAction(nameof(Users));
    }
}
