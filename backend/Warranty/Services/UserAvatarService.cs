using Microsoft.Extensions.Options;
using Warranty.Options;

namespace Warranty.Services;

public sealed class UserAvatarService(
    IOptions<UploadOptions> uploadOptions,
    IWebHostEnvironment env,
    ILogger<UserAvatarService> logger)
{
    private readonly UploadOptions _options = uploadOptions.Value;

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] ImageContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async Task<string> SaveAsync(int userId, IFormFile file, CancellationToken ct = default)
    {
        Validate(file);

        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(env.ContentRootPath, _options.RootPath);
        var folder = Path.Combine(root, "avatars", userId.ToString());
        Directory.CreateDirectory(folder);

        var safeName = Sanitize(file.FileName);
        var unique = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(folder, unique);
        var relative = Path.Combine("avatars", userId.ToString(), unique).Replace('\\', '/');

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            await file.CopyToAsync(stream, ct);

        logger.LogInformation("Avatar saved for user {UserId}: {Path}", userId, relative);
        return relative;
    }

    public void DeleteIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(env.ContentRootPath, _options.RootPath);
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private void Validate(IFormFile file)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("Empty file.");
        if (file.Length > _options.MaxFileSizeBytes)
            throw new InvalidOperationException($"Max size {_options.MaxFileSizeBytes / 1024 / 1024} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Extension '{extension}' not allowed for avatar.");
        if (!ImageContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Content type '{file.ContentType}' not allowed for avatar.");
    }

    private static string Sanitize(string name)
    {
        var safeName = Path.GetFileName(name);
        foreach (var character in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(character, '_');
        return string.IsNullOrWhiteSpace(safeName) ? "avatar.jpg" : safeName;
    }
}
