using System.Security.Claims;
using CookingCommunity.Web.Models;
using CookingCommunity.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CookingCommunity.Web.Services;

public class SessionService(CookingDbContext db)
{
    public static int UserId(ClaimsPrincipal principal) => int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public static Task SignIn(HttpContext context, User user) => context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.Role, user.Role), new Claim("credential", Stamp(user.PasswordHash)) }, CookieAuthenticationDefaults.AuthenticationScheme)));
    public static string Stamp(string hash) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hash)));
    public async Task Validate(CookieValidatePrincipalContext context)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == UserId(context.Principal!));
        if (user is null || !user.IsActive || context.Principal!.FindFirstValue("credential") != Stamp(user.PasswordHash) || !context.Principal!.IsInRole(user.Role))
        {
            context.RejectPrincipal(); await context.HttpContext.SignOutAsync();
        }
    }
}
