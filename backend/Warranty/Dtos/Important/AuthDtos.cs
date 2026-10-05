using System.ComponentModel.DataAnnotations;

namespace Warranty.Dtos;

public sealed class RegisterRequest
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), StringLength(72)]
    public string Password { get; init; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(72)]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshRequest
{
    [Required, StringLength(256)]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
