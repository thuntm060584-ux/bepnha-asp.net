using CookingCommunity.Web.Data;
using CookingCommunity.Web.Models;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Controllers;

[Authorize, Route("account")]
public class AccountController(CookingDbContext db) : Controller
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var u = await db.Users.SingleAsync(x => x.UserId == SessionService.UserId(User));
        return View(new ProfileForm { FullName = u.FullName, Bio = u.Bio });
    }
    [HttpPost("profile")]
    public async Task<IActionResult> Profile(ProfileForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var u = await db.Users.SingleAsync(x => x.UserId == SessionService.UserId(User));
        u.FullName = form.FullName.Trim(); u.Bio = (form.Bio ?? "").Trim();
        await db.SaveChangesAsync(); await SessionService.SignIn(HttpContext, u);
        TempData["Success"] = "Đã cập nhật hồ sơ."; return RedirectToAction(nameof(Profile));
    }
    [HttpGet("password")] public IActionResult Password() => View(new ChangePasswordForm());
    [HttpPost("password")]
    public async Task<IActionResult> Password(ChangePasswordForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var u = await db.Users.SingleAsync(x => x.UserId == SessionService.UserId(User));
        if (System.Text.Encoding.UTF8.GetByteCount(form.OldPassword) > 72 || !BCrypt.Net.BCrypt.Verify(form.OldPassword, u.PasswordHash))
        { ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng."); return View(form); }
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(form.Password, 12); u.ResetToken = null;
        await db.SaveChangesAsync(); await SessionService.SignIn(HttpContext, u);
        TempData["Success"] = "Đổi mật khẩu thành công."; return RedirectToAction(nameof(Profile));
    }
}
