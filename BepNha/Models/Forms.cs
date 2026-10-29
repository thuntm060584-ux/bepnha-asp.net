using System.ComponentModel.DataAnnotations;
using CookingCommunity.Web.Models;

namespace CookingCommunity.Web.Models;

public class PasswordRuleAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is string p && p.Length >= 8 && System.Text.Encoding.UTF8.GetByteCount(p) <= 72 && p.Any(char.IsLetter) && p.Any(char.IsDigit);
    public PasswordRuleAttribute() : base("Mật khẩu cần ít nhất 8 ký tự, có chữ và số, tối đa 72 byte UTF-8.") { }
}
public class LoginForm
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class RegisterForm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập trường này."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(254, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập trường này."), PasswordRule] public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; set; } = "";
}
public class ForgotForm
{
    [Required(ErrorMessage = "Vui lòng nhập trường này."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(254, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Email { get; set; } = "";
    public string? ResetLink { get; set; }
}
public class ResetForm
{
    [Required(ErrorMessage = "Vui lòng nhập trường này.")] public string Token { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập trường này."), PasswordRule] public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")] public string ConfirmPassword { get; set; } = "";
}
public class ProfileForm
{
    [Required(ErrorMessage = "Vui lòng nhập trường này."), StringLength(100, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string FullName { get; set; } = "";
    [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Bio { get; set; } = "";
}
public class ChangePasswordForm
{
    [Required(ErrorMessage = "Vui lòng nhập trường này.")] public string OldPassword { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập trường này."), PasswordRule] public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")] public string ConfirmPassword { get; set; } = "";
}
public class IngredientLine
{
    [Required(ErrorMessage = "Nhập tên nguyên liệu."), StringLength(100, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Nhập định lượng."), StringLength(100, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Quantity { get; set; } = "";
}
public class RecipeForm
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập trường này."), StringLength(200, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Title { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập trường này."), StringLength(2000, ErrorMessage = "Nội dung không được vượt quá {1} ký tự.")] public string Summary { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng chọn danh mục."), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")] public int CategoryId { get; set; }
    [Range(1, 3, ErrorMessage = "Độ khó phải từ 1 đến 3.")] public int Difficulty { get; set; } = 1;
    [Range(1, 10080, ErrorMessage = "Thời gian nấu phải từ 1 đến 10080 phút.")] public int CookMinutes { get; set; } = 30;
    [Range(1, 1000, ErrorMessage = "Khẩu phần phải từ 1 đến 1000 người.")] public int Servings { get; set; } = 2;
    public IFormFile? Cover { get; set; }
    public string? ExistingCover { get; set; }
    [MinLength(1, ErrorMessage = "Cần ít nhất một dòng."), MaxLength(100, ErrorMessage = "Tối đa 100 dòng.")] public List<IngredientLine> Ingredients { get; set; } = [new()];
    [MinLength(1, ErrorMessage = "Cần ít nhất một dòng."), MaxLength(100, ErrorMessage = "Tối đa 100 dòng.")] public List<string> Steps { get; set; } = [""];
    public List<Category> Categories { get; set; } = [];
}
public class RecipeListModel
{
    public List<Recipe> Recipes { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public string? Q { get; set; }
    public int? Category { get; set; }
    public int? Difficulty { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int Total { get; set; }
    public int Pages => (int)Math.Ceiling(Total / 12d);
    public string Heading { get; set; } = "Khám phá công thức";
    public bool IsHome { get; set; }
    public bool IsMine { get; set; }
    public bool IsFeed { get; set; }
    public List<Recipe> Featured { get; set; } = [];
    public User? Author { get; set; }
    public int Followers { get; set; }
    public bool IsFollowing { get; set; }
}
public class RecipeDetailsModel
{
    public Recipe Recipe { get; set; } = null!;
    public int Followers { get; set; }
    public bool IsFollowing { get; set; }
    public Review? MyReview { get; set; }
}

