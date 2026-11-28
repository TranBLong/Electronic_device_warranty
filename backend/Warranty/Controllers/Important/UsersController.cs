using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(AppDbContext dbContext) : ControllerBase
{
    /// <summary>Gets the current user's profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.AsNoTracking().SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        return Ok(ToResponse(user));
    }

    /// <summary>Updates the current user's profile.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = NormalizeEmail(request.Email);
        user.Phone = request.Phone?.Trim();
        user.Address = request.Address?.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails { Title = "Email is already registered." });
        }

        return Ok(ToResponse(user));
    }

    /// <summary>Deactivates the current user's account.</summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateMe(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        user.IsActive = false;
        await RevokeRefreshTokensAsync(userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Lists active customer accounts.</summary>
    [Authorize(Policy = Policies.Staff)]
    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetCustomers(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Customer && user.IsActive)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
        return Ok(users.Select(ToResponse).ToList());
    }

    /// <summary>Lists active technician accounts.</summary>
    [Authorize(Policy = Policies.Staff)]
    [HttpGet("technicians")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetTechnicians(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Technician && user.IsActive)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
        return Ok(users.Select(ToResponse).ToList());
    }

    /// <summary>Lists user accounts.</summary>
    [Authorize(Policy = Policies.CanManageUsers)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        [FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        var q = dbContext.Users.AsNoTracking().AsQueryable();
        if (query.NormalizedKeyword is { } term)
        {
            q = q.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.Phone != null && u.Phone.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var roleFilter))
            q = q.Where(u => u.Role == roleFilter);

        var sortKey = (query.SortBy ?? "fullname").ToLowerInvariant();
        var desc = query.IsDescending;
        q = sortKey switch
        {
            "email" => desc ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email),
            "role" => desc ? q.OrderByDescending(u => u.Role).ThenBy(u => u.FullName) : q.OrderBy(u => u.Role).ThenBy(u => u.FullName),
            "created" or "createdat" => desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt),
            "id" => desc ? q.OrderByDescending(u => u.Id) : q.OrderBy(u => u.Id),
            _ => desc ? q.OrderByDescending(u => u.FullName) : q.OrderBy(u => u.FullName)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .ToListAsync(cancellationToken);
        var responses = items.Select(ToResponse).ToList();
        return Ok(PagedResult<UserResponse>.Create(responses, query.PageNumber, query.Take, total));
    }

    /// <summary>Gets a user account by ID.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    /// <summary>Creates a user account.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        AdminCreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!PasswordHashing.CanHash(request.Password))
        {
            return BadRequest(new ProblemDetails { Title = "Password cannot exceed 72 UTF-8 bytes." });
        }

        if (request.Role is null || !Enum.IsDefined(request.Role.Value))
        {
            return BadRequest(new ProblemDetails { Title = "Role is invalid." });
        }

        var email = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Email is already registered." });
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHashing.Hash(request.Password),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Role = request.Role.Value,
            IsActive = request.IsActive
        };

        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails { Title = "Email is already registered." });
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToResponse(user));
    }

    /// <summary>Updates a user account.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Role is null || !Enum.IsDefined(request.Role.Value))
        {
            return BadRequest(new ProblemDetails { Title = "Role is invalid." });
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword) &&
            !PasswordHashing.CanHash(request.NewPassword))
        {
            return BadRequest(new ProblemDetails { Title = "Password cannot exceed 72 UTF-8 bytes." });
        }

        var email = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(candidate => candidate.Id != id && candidate.Email == email, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Email is already registered." });
        }

        var credentialsChanged = user.Role != request.Role.Value || user.IsActive != request.IsActive;
        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        user.Address = request.Address?.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;
        user.Role = request.Role.Value;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = PasswordHashing.Hash(request.NewPassword);
            credentialsChanged = true;
        }

        if (credentialsChanged)
        {
            await RevokeRefreshTokensAsync(user.Id, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails { Title = "Email is already registered." });
        }

        return Ok(ToResponse(user));
    }

    /// <summary>Deactivates a user account.</summary>
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = false;
        await RevokeRefreshTokensAsync(id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task RevokeRefreshTokensAsync(int userId, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.IsRevoked, true), cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    /// <summary>Changes the current user's password.</summary>
    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new ProblemDetails { Title = "New password must be at least 6 characters." });

        var user = await dbContext.Users.SingleAsync(u => u.Id == userId, cancellationToken);
        if (!PasswordHashing.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new ProblemDetails { Title = "Current password is incorrect." });

        user.PasswordHash = PasswordHashing.Hash(request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Uploads the current user's avatar.</summary>
    [HttpPost("me/avatar")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UserResponse>> UploadMyAvatar(
        IFormFile file,
        [FromServices] UserAvatarService avatarService,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No file uploaded." });

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
            return NotFound();

        try
        {
            var oldAvatarPath = user.AvatarPath;
            var relativePath = await avatarService.SaveAsync(userId, file, cancellationToken);
            user.AvatarPath = relativePath;
            await dbContext.SaveChangesAsync(cancellationToken);
            avatarService.DeleteIfExists(oldAvatarPath);
            return Ok(ToResponse(user));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = exception.Message });
        }
    }

    /// <summary>Removes the current user's avatar.</summary>
    [HttpDelete("me/avatar")]
    public async Task<ActionResult<UserResponse>> DeleteMyAvatar(
        [FromServices] UserAvatarService avatarService,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
            return NotFound();

        avatarService.DeleteIfExists(user.AvatarPath);
        user.AvatarPath = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(user));
    }

    private UserResponse ToResponse(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Phone,
        user.Address,
        user.DateOfBirth,
        user.Gender,
        user.Role,
        user.IsActive,
        user.CreatedAt,
        ResolveAvatarUrl(user.AvatarPath));

    private string ResolveAvatarUrl(string? avatarPath)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return string.IsNullOrWhiteSpace(avatarPath)
            ? $"{baseUrl}/default-avatar.svg"
            : $"{baseUrl}/uploads/{avatarPath.TrimStart('/')}";
    }
}
