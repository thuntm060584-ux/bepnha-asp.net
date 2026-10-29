namespace CookingCommunity.Web.Services;

public class ImageStorage(IWebHostEnvironment environment)
{
    public const long MaxImageBytes = 5L * 1024 * 1024;

    public async Task<string> Save(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxImageBytes) throw new InvalidDataException("Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".png")) throw new InvalidDataException("Chỉ chấp nhận ảnh có phần mở rộng .jpg hoặc .png.");
        await using var source = file.OpenReadStream();
        var header = new byte[12]; var length = await source.ReadAsync(header);
        bool valid = ext switch {
            ".jpg" => length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".png" => length >= 8 && header.AsSpan(0,8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}),
            _ => false
        };
        if (!valid) throw new InvalidDataException("Nội dung tệp không phải ảnh hợp lệ với phần mở rộng đã chọn.");
        var name = Guid.NewGuid().ToString("N") + ext;
        var folder = Path.Combine(environment.WebRootPath, "uploads"); Directory.CreateDirectory(folder);
        await using var destination = File.Create(Path.Combine(folder, name));
        await destination.WriteAsync(header.AsMemory(0, length)); await source.CopyToAsync(destination);
        return "/uploads/" + name;
    }
    public void Delete(string? url)
    {
        if (url?.StartsWith("/uploads/", StringComparison.Ordinal) == true && Path.GetFileName(url) == url[9..])
        {
            var path = Path.Combine(environment.WebRootPath, "uploads", Path.GetFileName(url));
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
