using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using CookingCommunity.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CookingCommunity.Web.Data;

public static class DemoSeeder
{
	public static async Task Seed(CookingDbContext db)
	{
		if (await db.Users.AnyAsync())
		{
			return;
		}
		await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
		string passwordHash = BCrypt.Net.BCrypt.HashPassword("BepViet123!", 12);
		User[] users = new User[5]
		{
			new User
			{
				FullName = "Quản trị FoodKing",
				Email = "adminbepnha@gmail.com",
				PasswordHash = passwordHash,
				Role = "admin"
			},
			new User
			{
				FullName = "Minh Anh",
				Email = "minhanh@foodking.local",
				PasswordHash = passwordHash,
				Bio = "Yêu những bữa cơm nhà và thích thử công thức mới mỗi ngày."
			},
			new User
			{
				FullName = "Hoàng Nam",
				Email = "hoangnam@foodking.local",
				PasswordHash = passwordHash,
				Bio = "Mình chia sẻ những món nướng và món Âu dễ làm trong căn bếp gia đình."
			},
			new User
			{
				FullName = "Thu Hà",
				Email = "thuha@foodking.local",
				PasswordHash = passwordHash,
				Bio = "Nấu ngon, ăn lành. Cùng mình khám phá món chay và salad nhé!"
			},
			new User
			{
				FullName = "Gia Bảo",
				Email = "giabao@foodking.local",
				PasswordHash = passwordHash,
				Bio = "Cuối tuần vào bếp, mang niềm vui đến bàn ăn."
			}
		};
		Category[] categories = new string[5] { "Món chính", "Món ăn nhanh", "Món chay & salad", "Món nướng", "Bánh & tráng miệng" }.Select((string x) => new Category
		{
			CategoryName = x
		}).ToArray();
		db.Users.AddRange(users);
		db.Categories.AddRange(categories);
		await db.SaveChangesAsync();
		Dictionary<string, Ingredient> catalog = new Dictionary<string, Ingredient>();
		Recipe[] recipes = new Recipe[10]
		{
			Make(0, "Gà rán giòn thơm tại nhà", "Lớp vỏ vàng giòn, thịt gà mềm ngọt. Món ngon cho buổi sum họp cuối tuần, dùng kèm salad và tương ớt.", 1, 2, 45, 4, "ga-ran-gion.png", new string[6] { "đùi gà|800 g", "bột mì|150 g", "bột bắp|50 g", "sữa tươi|200 ml", "muối|1 thìa cà phê", "dầu ăn|700 ml" }, new string[4] { "Thấm khô gà, khía nhẹ phần thịt dày. Ướp với sữa tươi và muối trong ngăn mát 30 phút.", "Trộn bột mì và bột bắp. Lăn từng miếng gà qua hỗn hợp bột, nhúng lại sữa rồi áo bột lần hai.", "Đun dầu đến khoảng 170°C. Chiên gà từng mẻ 12–15 phút, giữ lửa vừa để vỏ không cháy.", "Kiểm tra phần dày nhất đạt ít nhất 74°C. Để ráo trên giá và dùng khi còn nóng." }),
			Make(1, "Burger bò phô mai", "Bánh burger bò tự làm với thịt mọng nước, phô mai tan chảy và rau tươi. Bữa trưa nhanh gọn mà vẫn đủ chất.", 1, 1, 25, 2, "burger-bo-pho-mai.png", new string[6] { "thịt bò xay|300 g", "bánh burger|2 chiếc", "phô mai|2 lát", "xà lách|4 lá", "cà chua|1 quả", "muối|1/2 thìa cà phê" }, new string[4] { "Chia thịt bò thành hai phần, nắn dẹt và rắc muối đều hai mặt.", "Áp chảo mỗi mặt khoảng 4–5 phút, đến khi tâm thịt đạt 71°C. Đặt phô mai lên thịt ở phút cuối.", "Nướng nhẹ mặt trong bánh. Rửa rau và thái cà chua thành lát.", "Xếp rau, cà chua và thịt vào bánh. Dùng ngay khi nóng." }),
			Make(2, "Pizza rau củ phô mai", "Đế bánh thơm, viền giòn cùng ớt chuông, nấm và phô mai. Một lựa chọn không thịt cho buổi tối cùng bạn bè.", 2, 2, 50, 3, "pizza-rau-cu-pho-mai.png", new string[7] { "bột mì|250 g", "men nở|4 g", "nước ấm|150 ml", "phô mai mozzarella|150 g", "ớt chuông|1 quả", "nấm|100 g", "sốt cà chua|80 g" }, new string[4] { "Trộn bột, men và nước ấm, nhồi 10 phút rồi ủ khoảng 45 phút đến khi bột nở gần gấp đôi.", "Làm nóng lò ở 230°C. Cán bột thành đế mỏng, đặt lên khay lót giấy nướng.", "Phết sốt, rắc phô mai, xếp nấm và ớt chuông thái mỏng lên mặt.", "Nướng 12–15 phút đến khi viền bánh vàng và phô mai tan. Cắt miếng và thưởng thức." }),
			Make(3, "Khoai tây chiên giòn", "Khoai vàng giòn bên ngoài, bùi mềm bên trong. Mẹo chiên hai lần giúp khoai giữ độ giòn lâu hơn.", 1, 1, 35, 3, "khoai-tay-chien.png", new string[3] { "khoai tây|600 g", "dầu ăn|500 ml", "muối|1/2 thìa cà phê" }, new string[4] { "Gọt vỏ khoai, cắt thanh đều nhau. Ngâm nước lạnh 15 phút rồi rửa sạch tinh bột.", "Luộc sơ khoai trong 3 phút, vớt ra và thấm thật khô.", "Chiên lần đầu ở 150°C khoảng 5 phút. Vớt ra để nguội 10 phút.", "Chiên lần hai ở 180°C khoảng 3 phút cho vàng giòn. Để ráo dầu, rắc muối và dùng ngay." }),
			Make(4, "Gà nướng tỏi mật ong", "Thịt gà ướp mật ong và tỏi, nướng vàng thơm. Nước sốt ngọt dịu rất hợp dùng với cơm nóng.", 3, 2, 60, 4, "ga-nuong-toi-mat-ong.png", new string[5] { "đùi gà|1 kg", "mật ong|2 thìa canh", "tỏi|5 tép", "nước tương|2 thìa canh", "dầu ăn|1 thìa canh" }, new string[4] { "Lau khô gà. Trộn mật ong, tỏi băm, nước tương và dầu ăn thành sốt ướp.", "Xoa sốt khắp gà, ướp trong tủ lạnh ít nhất 30 phút.", "Làm nóng lò 190°C. Xếp gà lên khay, nướng 35–45 phút, trở mặt giữa chừng.", "Kiểm tra thịt chín với nhiệt độ tâm ít nhất 74°C. Để nghỉ 5 phút trước khi cắt." }),
			Make(5, "Cơm gà gia vị", "Hạt cơm tơi thơm quyện gia vị và thịt gà đậm đà. Nấu trong một nồi, tiện cho bữa tối gia đình.", 0, 2, 50, 4, "com-ga-gia-vi.png", new string[6] { "gạo|400 g", "thịt gà|600 g", "hành tây|1 củ", "bột cà ri|2 thìa cà phê", "sữa chua không đường|100 g", "muối|1 thìa cà phê" }, new string[4] { "Vo gạo, ngâm 15 phút. Ướp gà với sữa chua, cà ri và muối.", "Phi hành tây trong nồi với ít dầu, thêm gà và đảo đến khi săn.", "Cho gạo vào nồi, thêm khoảng 600 ml nước nóng. Đậy nắp nấu lửa nhỏ 20 phút.", "Kiểm tra gạo mềm và gà chín hoàn toàn. Tắt bếp, ủ 10 phút rồi xới nhẹ." }),
			Make(6, "Bánh mì bò áp chảo", "Bánh mì kẹp bò thơm tiêu cùng rau tươi, thích hợp cho bữa sáng nhiều năng lượng.", 0, 1, 20, 2, "banh-mi-bo-ap-chao.png", new string[5] { "thịt bò|250 g", "bánh mì|2 ổ", "dưa leo|1 quả", "tỏi|2 tép", "nước tương|1 thìa canh" }, new string[4] { "Thái bò mỏng ngang thớ, ướp tỏi băm và nước tương trong 10 phút.", "Làm nóng chảo, thêm ít dầu. Xào bò nhanh trên lửa lớn đến khi chín.", "Nướng giòn bánh mì, rửa dưa leo và thái lát.", "Rạch bánh, kẹp bò và dưa leo. Dùng ngay để bánh giữ độ giòn." }),
			Make(7, "Gà viên giòn cho cả nhà", "Những viên gà nhỏ xinh với lớp áo giòn. Có thể chuẩn bị sẵn, bảo quản ngăn mát và chiên ngay trước bữa ăn.", 1, 1, 30, 3, "ga-vien-gion.png", new string[6] { "thịt gà xay|400 g", "trứng gà|1 quả", "bột mì|80 g", "bột chiên xù|100 g", "muối|1/2 thìa cà phê", "dầu ăn|400 ml" }, new string[4] { "Trộn thịt gà với muối, nắn thành từng viên nhỏ đều nhau.", "Đánh tan trứng. Lăn gà qua bột mì, trứng rồi bột chiên xù.", "Chiên trong dầu 170°C khoảng 6–8 phút, đến khi tâm viên gà đạt 74°C.", "Để ráo trên giấy thấm dầu, ăn kèm rau và sốt tùy thích." }),
			Make(8, "Pizza gà nướng", "Tận dụng gà nướng cho chiếc pizza đậm vị, thêm hành tây và phô mai để bữa tối thêm thú vị.", 4, 3, 65, 4, "pizza-ga-nuong.png", new string[7] { "bột mì|300 g", "men nở|5 g", "nước ấm|180 ml", "gà nướng chín|200 g", "phô mai mozzarella|180 g", "sốt cà chua|100 g", "hành tây|1/2 củ" }, new string[4] { "Nhồi bột mì, men và nước 10 phút. Đậy kín, ủ 45–60 phút đến khi nở gấp đôi.", "Làm nóng lò 230°C. Xé nhỏ thịt gà, thái mỏng hành tây.", "Cán đế bánh, phết sốt cà chua. Phủ gà, hành và phô mai.", "Nướng khoảng 15 phút đến khi đế chín giòn. Để nguội 2 phút rồi cắt miếng." }),
			Make(9, "Burger nấm thanh nhẹ", "Nấm áp chảo thơm ngọt thay cho thịt, kết hợp rau xanh và cà chua trong chiếc bánh burger mềm.", 2, 1, 20, 2, "burger-nam.png", new string[6] { "nấm đùi gà|300 g", "bánh burger|2 chiếc", "xà lách|4 lá", "cà chua|1 quả", "nước tương|1 thìa canh", "dầu ăn|1 thìa canh" }, new string[4] { "Lau sạch nấm, thái lát dày. Ướp với nước tương trong 5 phút.", "Làm nóng dầu trong chảo, áp chảo nấm mỗi mặt 3–4 phút đến khi vàng và chín.", "Rửa rau, thái cà chua và làm nóng bánh burger.", "Xếp nấm và rau vào bánh, thêm sốt theo khẩu vị. Thưởng thức ngay." })
		};
		db.Recipes.AddRange(recipes);
		await db.SaveChangesAsync();
		Recipe[] array = recipes;
		foreach (Recipe r in array)
		{
			foreach (User item in (from x in users.Skip(1)
				where x.UserId != r.AuthorId
				select x).Take(2))
			{
				db.Reviews.Add(new Review
				{
					RecipeId = r.RecipeId,
					ReviewerId = item.UserId,
					Rating = (r.RecipeId + item.UserId) % 2 + 4,
					Comment = "Công thức dễ theo dõi, gia đình mình rất thích. Cảm ơn bạn đã chia sẻ!"
				});
			}
		}
		for (int num2 = 1; num2 <= 4; num2++)
		{
			db.Follows.Add(new Follow
			{
				FollowerId = users[num2].UserId,
				FollowingId = users[(num2 == 4) ? 1 : (num2 + 1)].UserId
			});
		}
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
		Recipe Make(int n, string title, string summary, int cat, int difficulty, int minutes, int servings, string image, string[] ingredients, string[] steps)
		{
			Recipe recipe = new Recipe
			{
				AuthorId = users[1 + n % 4].UserId,
				CategoryId = categories[cat].CategoryId,
				Title = title,
				Summary = summary,
				Difficulty = difficulty,
				CookMinutes = minutes,
				Servings = servings,
				CoverImage = "/assets/img/recipes/" + image,
				Instructions = string.Join('\n', steps),
				CreatedAt = DateTime.UtcNow.AddDays(-12 + n)
			};
			for (int i = 0; i < ingredients.Length; i++)
			{
				string[] array2 = ingredients[i].Split('|');
				if (!catalog.TryGetValue(array2[0], out Ingredient? value) || value is null)
				{
					value = new Ingredient
					{
						IngredientName = array2[0]
					};
					catalog.Add(array2[0], value);
				}
				recipe.RecipeIngredients.Add(new RecipeIngredient
				{
					Ingredient = value,
					Quantity = array2[1]
				});
			}
			return recipe;
		}
	}
}
