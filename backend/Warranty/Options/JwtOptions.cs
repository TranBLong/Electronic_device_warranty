using System.ComponentModel.DataAnnotations;

namespace Warranty.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = "Warranty.Api";
    [Required] public string Audience { get; set; } = "Warranty.Client";
    [Required, MinLength(32)] public string SigningKey { get; set; } = string.Empty;
    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 30;
    [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;
}
