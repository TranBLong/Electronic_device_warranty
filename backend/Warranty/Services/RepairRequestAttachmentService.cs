using Microsoft.Extensions.Options;
using Warranty.Data;
using Warranty.Models;
using Warranty.Options;

namespace Warranty.Services;

public sealed class RepairRequestAttachmentService(
    AppDbContext dbContext,
    IOptions<UploadOptions> uploadOptions,
    IWebHostEnvironment env,
    ILogger<RepairRequestAttachmentService> logger)
{
    private readonly UploadOptions _options = uploadOptions.Value;

    public async Task<RepairRequestAttachment> SaveAsync(
        int repairRequestId, int uploadedById, IFormFile file, CancellationToken ct = default)
    {
        await ValidateAsync(file, ct);
        var root = GetRootPath();
        var folder = Path.Combine(root, "repair-requests", repairRequestId.ToString());
        Directory.CreateDirectory(folder);

        var safeName = Sanitize(file.FileName);
        var unique = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(folder, unique);
        var relative = Path.Combine("repair-requests", repairRequestId.ToString(), unique).Replace('\\', '/');

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            await file.CopyToAsync(stream, ct);

        var attachment = new RepairRequestAttachment
        {
            RepairRequestId = repairRequestId,
            UploadedById = uploadedById,
            FileName = safeName,
            FilePath = relative,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };
        dbContext.RepairRequestAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Attachment {Name} saved for RR {Id}", safeName, repairRequestId);
        return attachment;
    }

    public string GetPhysicalPath(string relativePath)
    {
        var root = GetRootPath();
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, fullPath);
        if (relative == "." || relative == ".." ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("Attachment path is outside the upload directory.");
        }

        return fullPath;
    }

    private string GetRootPath() => Path.GetFullPath(Path.IsPathRooted(_options.RootPath)
        ? _options.RootPath
        : Path.Combine(env.ContentRootPath, _options.RootPath));

    private async Task ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0) throw new FileValidationException("Empty file.");
        if (file.Length > _options.MaxFileSizeBytes)
            throw new FileValidationException($"Max size {_options.MaxFileSizeBytes / 1024 / 1024} MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new FileValidationException($"Extension '{ext}' not allowed.");
        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new FileValidationException($"Content type '{file.ContentType}' not allowed.");

        var expectedContentType = GetExpectedContentType(ext);
        if (expectedContentType is not null &&
            !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileValidationException($"Content type does not match extension '{ext}'.");
        }

        var signature = GetExpectedSignature(ext);
        if (signature is null) return;

        await using var stream = file.OpenReadStream();
        var header = new byte[signature.Length];
        var bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        if (bytesRead != header.Length || !header.AsSpan().SequenceEqual(signature))
            throw new FileValidationException($"File content does not match extension '{ext}'.");
    }

    private static byte[]? GetExpectedSignature(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => [0xFF, 0xD8, 0xFF],
        ".png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        ".pdf" => [0x25, 0x50, 0x44, 0x46, 0x2D],
        _ => null
    };

    private static string? GetExpectedContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => null
    };

    private static string Sanitize(string name)
    {
        var f = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars()) f = f.Replace(c, '_');
        return string.IsNullOrWhiteSpace(f) ? "file" : f;
    }
}
