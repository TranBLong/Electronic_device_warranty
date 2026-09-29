using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetCustomers(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Customer && user.IsActive)
            .OrderBy(user => user.FullName)
            .Select(user => new UserResponse(
                user.Id, user.FullName, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [Authorize(Roles = "Admin,Manager,Receptionist")]
    [HttpGet("technicians")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetTechnicians(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Technician && user.IsActive)
            .OrderBy(user => user.FullName)
            .Select(user => new UserResponse(
                user.Id, user.FullName, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .OrderBy(user => user.Id)
            .Select(user => new UserResponse(
                user.Id, user.FullName, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

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

    private static UserResponse ToResponse(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Phone,
        user.Role,
        user.IsActive,
        user.CreatedAt);
}
