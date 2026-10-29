using System.Security.Cryptography;
using System.Text;
using CookingCommunity.Web.Models;
using CookingCommunity.Web.Data;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Controllers;

[Route("auth")]
public class AuthController(CookingDbContext db) : Controller
{
    [HttpGet("register")] public IActionResult Register() => View(new RegisterForm());
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        form.Email = form.Email.Trim().ToLowerInvariant(); form.FullName = form.FullName.Trim();
        if (await db.Users.AnyAsync(x => x.Email == form.Email)) ModelState.AddModelError(nameof(form.Email), "Email đã được sử dụng.");
        if (!ModelState.IsValid) return View(form);
        db.Users.Add(new User { FullName = form.FullName, Email = form.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(form.Password, 12) });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("Email", "Email đã được sử dụng. Vui lòng thử lại."); return View(form); }
        TempData["Success"] = "Đăng ký thành công. Hãy đăng nhập để chia sẻ món ngon.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            Response.Cookies.Delete("CookingCommunity.ReturnUrl");
            if (Url.IsLocalUrl(returnUrl))
                Response.Cookies.Append("CookingCommunity.ReturnUrl", returnUrl, new CookieOptions
                {
                    HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps, MaxAge = TimeSpan.FromMinutes(5)
                });
            return Redirect("/auth/login");
        }
        if (string.IsNullOrWhiteSpace(returnUrl) && Request.Cookies.TryGetValue("CookingCommunity.ReturnUrl", out var pendingReturnUrl)
            && Url.IsLocalUrl(pendingReturnUrl)) returnUrl = pendingReturnUrl;
        Response.Cookies.Delete("CookingCommunity.ReturnUrl");
        return View(new LoginForm { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == form.Email.Trim().ToLowerInvariant());
        if (user is null || Encoding.UTF8.GetByteCount(form.Password) > 72 || !BCrypt.Net.BCrypt.Verify(form.Password, user.PasswordHash))
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        else if (!user.IsActive) ModelState.AddModelError("", "Tài khoản đã bị khóa.");
        if (!ModelState.IsValid) return View(form);
        await SessionService.SignIn(HttpContext, user!);
        if (user!.Role == "admin") return Redirect("/admin/recipes");
        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/");
    }
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return Redirect("/"); }
    [HttpGet("forgot-password")] public IActionResult Forgot() => View(new ForgotForm());
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(ForgotForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == form.Email.Trim().ToLowerInvariant());
        if (user is null) { ModelState.AddModelError("Email", "Email không tồn tại."); return View(form); }
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        // Token không tự hết hạn theo thời gian; yêu cầu mới thay token cũ và token chỉ dùng một lần.
        user.ResetToken = HashToken(token);
        await db.SaveChangesAsync();
        form.ResetLink = Url.Action(nameof(Reset), "Auth", new { token });
        return View(form);
    }
    [HttpGet("reset-password")]
    public async Task<IActionResult> Reset(string? token)
    {
        if (!await ValidToken(token)) ModelState.AddModelError("", "Liên kết không hợp lệ hoặc đã được sử dụng. Vui lòng yêu cầu lại.");
        return View(new ResetForm { Token = token ?? "" });
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetForm form)
    {
        if (!await ValidToken(form.Token)) ModelState.AddModelError("", "Liên kết không hợp lệ hoặc đã được sử dụng. Vui lòng yêu cầu lại.");
        if (!ModelState.IsValid) return View(form);
        // Atomic compare-and-clear prevents concurrent requests from reusing a token.
        var hash = HashToken(form.Token); var password = BCrypt.Net.BCrypt.HashPassword(form.Password, 12);
        var changed = await db.Users.Where(x => x.ResetToken == hash)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PasswordHash, password).SetProperty(x => x.ResetToken, (string?)null));
        if (changed != 1) { ModelState.AddModelError("", "Liên kết đã được sử dụng hoặc không còn hợp lệ."); return View(form); }
        TempData["Success"] = "Đã đặt lại mật khẩu. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }
    private Task<bool> ValidToken(string? token) => token is { Length: 64 }
        ? db.Users.AnyAsync(x => x.ResetToken == HashToken(token)) : Task.FromResult(false);
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
