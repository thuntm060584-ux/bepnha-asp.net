using Microsoft.AspNetCore.Mvc;
namespace CookingCommunity.Web.Controllers;
public class ErrorsController : Controller
{
    [Route("/errors/{code:int}")]
    public IActionResult Error(int code) { Response.StatusCode = code; return View("~/Views/Shared/Status.cshtml", code); }
}
