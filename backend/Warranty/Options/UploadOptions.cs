using System.ComponentModel.DataAnnotations;

namespace Warranty.Options;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    [Required] public string RootPath { get; set; } = "App_Data/uploads";
    [Range(1024, 104_857_600)] public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"];
    public string[] AllowedExtensions { get; set; } =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"];
}
