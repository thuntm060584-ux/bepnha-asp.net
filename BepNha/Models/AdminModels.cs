using CookingCommunity.Web.Models;
namespace CookingCommunity.Web.Models;
public class AdminRecipesModel
{
    public List<Recipe> Recipes { get; set; } = [];
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; }
    public int Pages { get; set; }
    public int Total { get; set; }
}
public class AdminReviewsModel
{
    public List<Review> Reviews { get; set; } = [];
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; }
    public int Pages { get; set; }
    public int Total { get; set; }
}
