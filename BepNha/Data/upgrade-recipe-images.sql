-- Thay ảnh có sẵn bằng ảnh riêng theo đúng món; giữ ảnh do người dùng tự đổi.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
UPDATE r SET cover_image = N'/assets/img/recipes/' + images.new_file
FROM dbo.recipes r
JOIN (VALUES
    (N'Gà rán giòn thơm tại nhà', N'fried-chicken.png', N'ga-ran-gion.png'),
    (N'Burger bò phô mai', N'burger.png', N'burger-bo-pho-mai.png'),
    (N'Pizza rau củ phô mai', N'big-pizza.png', N'pizza-rau-cu-pho-mai.png'),
    (N'Khoai tây chiên giòn', N'french-fry.png', N'khoai-tay-chien.png'),
    (N'Gà nướng tỏi mật ong', N'grilled.png', N'ga-nuong-toi-mat-ong.png'),
    (N'Cơm gà gia vị', N'chicken-biryani.png', N'com-ga-gia-vi.png'),
    (N'Bánh mì bò áp chảo', N'beef-ruti.png', N'banh-mi-bo-ap-chao.png'),
    (N'Gà viên giòn cho cả nhà', N'chicken-2.png', N'ga-vien-gion.png'),
    (N'Pizza gà nướng', N'big-pizza.png', N'pizza-ga-nuong.png'),
    (N'Burger nấm thanh nhẹ', N'fresh-burger.png', N'burger-nam.png')
) images(title, old_file, new_file)
ON r.title = images.title AND r.cover_image = N'/assets/img/food/' + images.old_file;
COMMIT;
