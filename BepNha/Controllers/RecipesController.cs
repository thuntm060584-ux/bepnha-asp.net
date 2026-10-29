using System.Data;
using System.Text.RegularExpressions;
using CookingCommunity.Web.Models;
using CookingCommunity.Web.Data;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Controllers;

public class RecipesController(CookingDbContext db, ImageStorage images, ILogger<RecipesController> logger) : Controller
{
    private int CurrentId => SessionService.UserId(User);
    [HttpGet("/"), HttpGet("/recipes"), HttpGet("/recipes/search")]
    public Task<IActionResult> Index(string? q, int? category, int? difficulty, string sort = "newest", int page = 1) => List(q, category, difficulty, sort, page);
    [Authorize(Roles = "member"), HttpGet("/recipes/mine")]
    public Task<IActionResult> Mine(int page = 1) => List(null, null, null, "newest", page, mine: true);
    [Authorize(Roles = "member"), HttpGet("/feed")]
    public Task<IActionResult> Feed(int page = 1) => List(null, null, null, "newest", page, feed: true);
    [HttpGet("/authors/{id:int}")]
    public Task<IActionResult> Author(int id, int page = 1) => List(null, null, null, "newest", page, author: id);
    private async Task<IActionResult> List(string? q, int? category, int? difficulty, string sort, int page, bool mine = false, bool feed = false, int? author = null)
    {
        var m = new RecipeListModel { Q = q?.Trim(), Category = category, Difficulty = difficulty, Sort = sort, IsMine = mine, IsFeed = feed, IsHome = Request.Path == "/", Categories = await db.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync() };
        var query = db.Recipes.AsNoTracking().Where(x => mine ? x.AuthorId == CurrentId : x.IsPublished);
        if (author.HasValue)
        {
            m.Author = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == author);
            if (m.Author is null) return NotFound();
            query = query.Where(x => x.AuthorId == author);
            m.Followers = await db.Follows.CountAsync(x => x.FollowingId == author);
            m.IsFollowing = await db.Follows.AnyAsync(x => x.FollowerId == CurrentId && x.FollowingId == author);
            m.Heading = "Gian bếp của " + m.Author.FullName;
        }
        if (feed) { query = query.Where(x => db.Follows.Any(f => f.FollowerId == CurrentId && f.FollowingId == x.AuthorId)); m.Heading = "Bảng tin đang theo dõi"; }
        if (mine) m.Heading = "Công thức của tôi";
        if (!string.IsNullOrWhiteSpace(m.Q)) query = query.Where(x => x.Title.Contains(m.Q) || x.RecipeIngredients.Any(i => i.Ingredient.IngredientName.Contains(m.Q)));
        if (category.HasValue) query = query.Where(x => x.CategoryId == category);
        if (difficulty.HasValue) query = query.Where(x => x.Difficulty == difficulty);
        m.Total = await query.CountAsync(); m.Page = Math.Clamp(page, 1, Math.Max(1, m.Pages));
        query = sort == "rating" ? query.OrderByDescending(x => x.Reviews.Average(r => (double?)r.Rating) ?? 0).ThenByDescending(x => x.RecipeId) : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.RecipeId);
        m.Recipes = await query.Include(x => x.Author).Include(x => x.Category).Include(x => x.Reviews).Skip((m.Page - 1) * 12).Take(12).ToListAsync();
        if (m.IsHome) m.Featured = await db.Recipes.AsNoTracking().Where(x => x.IsPublished && x.Reviews.Any()).OrderByDescending(x => x.Reviews.Average(r => r.Rating)).ThenByDescending(x => x.RecipeId).Include(x => x.Author).Include(x => x.Category).Include(x => x.Reviews).Take(3).ToListAsync();
        return View("Index", m);
    }
    [HttpGet("/recipes/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var r = await db.Recipes.AsNoTracking().AsSplitQuery().Include(x => x.Author).Include(x => x.Category).Include(x => x.RecipeIngredients).ThenInclude(x => x.Ingredient).Include(x => x.Reviews).ThenInclude(x => x.Reviewer).SingleOrDefaultAsync(x => x.RecipeId == id);
        if (r is null || (!r.IsPublished && r.AuthorId != CurrentId && !User.IsInRole("admin"))) return NotFound();
        return View(new RecipeDetailsModel { Recipe = r, Followers = await db.Follows.CountAsync(x => x.FollowingId == r.AuthorId), IsFollowing = await db.Follows.AnyAsync(x => x.FollowerId == CurrentId && x.FollowingId == r.AuthorId), MyReview = r.Reviews.FirstOrDefault(x => x.ReviewerId == CurrentId) });
    }
    [Authorize(Roles = "member"), HttpGet("/recipes/new")]
    public async Task<IActionResult> Create() => View("Edit", new RecipeForm { Categories = await db.Categories.ToListAsync() });
    [Authorize(Roles = "member"), HttpGet("/recipes/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await db.Recipes.Include(x => x.RecipeIngredients).ThenInclude(x => x.Ingredient).SingleOrDefaultAsync(x => x.RecipeId == id);
        if (r is null) return NotFound(); if (r.AuthorId != CurrentId) return Forbid();
        return View(new RecipeForm { Id = id, Title = r.Title, Summary = r.Summary, CategoryId = r.CategoryId, Difficulty = r.Difficulty, CookMinutes = r.CookMinutes, Servings = r.Servings, ExistingCover = r.CoverImage, Steps = r.Instructions.Split('\n').ToList(), Ingredients = r.RecipeIngredients.Select(x => new IngredientLine { Name = x.Ingredient.IngredientName, Quantity = x.Quantity }).ToList(), Categories = await db.Categories.ToListAsync() });
    }
    [Authorize(Roles = "member"), HttpPost("/recipes/new"), HttpPost("/recipes/{id:int}/edit")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Save(int? id, RecipeForm form)
    {
        // Read ownership and persisted cover from the database; never trust posted IDs or paths.
        var r = id.HasValue ? await db.Recipes.Include(x => x.RecipeIngredients).SingleOrDefaultAsync(x => x.RecipeId == id) : new Recipe { AuthorId = CurrentId };
        if (r is null) return NotFound(); if (r.AuthorId != CurrentId) return Forbid();
        form.Id = id; form.ExistingCover = r.CoverImage; form.Categories = await db.Categories.ToListAsync();
        if (!form.Categories.Any(x => x.CategoryId == form.CategoryId)) ModelState.AddModelError("CategoryId", "Danh mục không tồn tại.");
        if (form.Steps is null || form.Steps.Count == 0 || form.Steps.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 4000 || x.Contains('\n') || x.Contains('\r'))) ModelState.AddModelError("Steps", "Nhập ít nhất một bước; mỗi bước tối đa 4000 ký tự, không xuống dòng trong một bước.");
        if (form.Ingredients is null || form.Ingredients.Count == 0) ModelState.AddModelError("Ingredients", "Cần ít nhất một nguyên liệu.");
        if (form.Ingredients?.GroupBy(x => Normalize(x.Name)).Any(x => x.Count() > 1) == true) ModelState.AddModelError("Ingredients", "Nguyên liệu bị trùng. Hãy gộp định lượng trên một dòng.");
        if (!id.HasValue && form.Cover is null) ModelState.AddModelError("Cover", "Vui lòng chọn ảnh đại diện.");
        if (!ModelState.IsValid) return View("Edit", form);
        string? uploaded = null; var previous = r.CoverImage;
        try
        {
            if (form.Cover is not null) uploaded = await images.Save(form.Cover);
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await db.Categories.AnyAsync(x => x.CategoryId == form.CategoryId)) throw new InvalidDataException("Danh mục vừa bị xóa. Vui lòng chọn danh mục khác.");
            r.Title = form.Title.Trim(); r.Summary = form.Summary.Trim(); r.CategoryId = form.CategoryId; r.Difficulty = form.Difficulty; r.CookMinutes = form.CookMinutes; r.Servings = form.Servings;
            r.Instructions = string.Join('\n', form.Steps!.Select(x => x.Trim())); r.CoverImage = uploaded ?? previous;
            var existingLinks = id.HasValue ? await db.RecipeIngredients.IgnoreQueryFilters().Where(x => x.RecipeId == r.RecipeId).ToListAsync() : [];
            if (id.HasValue) { r.UpdatedAt = DateTime.UtcNow; foreach (var link in existingLinks) link.SoftDelete(); }
            else db.Recipes.Add(r);
            foreach (var line in form.Ingredients!)
            {
                var name = Normalize(line.Name); var ingredient = await db.Ingredients.SingleOrDefaultAsync(x => x.IngredientName == name);
                ingredient ??= new Ingredient { IngredientName = name };
                var link = existingLinks.FirstOrDefault(x => x.IngredientId == ingredient.IngredientId && ingredient.IngredientId != 0);
                if (link is not null) { link.Quantity = line.Quantity.Trim(); link.UndoDelete(); }
                else r.RecipeIngredients.Add(new RecipeIngredient { Ingredient = ingredient, Quantity = line.Quantity.Trim() });
            }
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is InvalidDataException or DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            images.Delete(uploaded); logger.LogWarning(ex, "Không lưu được công thức");
            ModelState.AddModelError("", ex is InvalidDataException ? ex.Message : "Không thể lưu công thức. Dữ liệu đã được hoàn tác, vui lòng thử lại.");
            return View("Edit", form);
        }
        // Retain previous stored covers too; only uncommitted failed uploads are cleaned up.
        TempData["Success"] = "Đã lưu công thức."; return RedirectToAction(nameof(Details), new { id = r.RecipeId });
    }
    [Authorize(Roles = "member"), HttpPost("/recipes/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await db.Recipes.FindAsync(id); if (r is null) return NotFound(); if (r.AuthorId != CurrentId) return Forbid();
        r.SoftDelete(); r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa công thức khỏi gian bếp. Dữ liệu và ảnh được giữ lại để quản trị viên có thể khôi phục."; return RedirectToAction(nameof(Mine));
    }
    private static string Normalize(string? value) => Regex.Replace((value ?? "").Trim(), @"\s+", " ").ToLowerInvariant();
}
