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
        Validate(file);
        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(env.ContentRootPath, _options.RootPath);
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

    private void Validate(IFormFile file)
    {
        if (file.Length <= 0) throw new InvalidOperationException("Empty file.");
        if (file.Length > _options.MaxFileSizeBytes)
            throw new InvalidOperationException($"Max size {_options.MaxFileSizeBytes / 1024 / 1024} MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Extension '{ext}' not allowed.");
        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Content type '{file.ContentType}' not allowed.");
    }

    private static string Sanitize(string name)
    {
        var f = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars()) f = f.Replace(c, '_');
        return string.IsNullOrWhiteSpace(f) ? "file" : f;
    }
}
