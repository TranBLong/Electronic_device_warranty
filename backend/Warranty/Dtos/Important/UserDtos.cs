using System.ComponentModel.DataAnnotations;
using Warranty.Enums;

namespace Warranty.Dtos;

public sealed class AdminCreateUserRequest
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), StringLength(72)]
    public string Password { get; init; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; init; }

    [StringLength(300)]
    public string? Address { get; init; }

    public DateTime? DateOfBirth { get; init; }

    [EnumDataType(typeof(Gender))]
    public Gender? Gender { get; init; }

    [Required, EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class AdminUpdateUserRequest
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; init; }

    [StringLength(300)]
    public string? Address { get; init; }

    public DateTime? DateOfBirth { get; init; }

    [EnumDataType(typeof(Gender))]
    public Gender? Gender { get; init; }

    [Required, EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; init; }

    public bool IsActive { get; init; } = true;

    [MinLength(8), StringLength(72)]
    public string? NewPassword { get; init; }
}

public sealed class UpdateMyProfileRequest
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; init; }

    [StringLength(300)]
    public string? Address { get; init; }

    public DateTime? DateOfBirth { get; init; }

    [EnumDataType(typeof(Gender))]
    public Gender? Gender { get; init; }
}

public sealed record UserResponse(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    Gender? Gender,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    string AvatarUrl);


public sealed class ChangePasswordRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;
}
