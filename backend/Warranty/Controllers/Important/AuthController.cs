using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext dbContext,
    TokenService tokenService) : ControllerBase
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!PasswordHashing.CanHash(request.Password))
        {
            return BadRequest(new ProblemDetails { Title = "Password cannot exceed 72 UTF-8 bytes." });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Email is already registered." });
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHashing.Hash(request.Password),
            Phone = request.Phone?.Trim(),
            Role = UserRole.Customer
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

        return Created("/api/auth/register", ToResponse(user));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (user is null || !user.IsActive || !PasswordHashing.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        return Ok(await IssueTokensAsync(user, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var suppliedTokenHash = HashRefreshToken(request.RefreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.Token == suppliedTokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt <= now)
        {
            return Unauthorized(new ProblemDetails { Title = "Refresh token is invalid or expired." });
        }

        if (!storedToken.User.IsActive)
        {
            storedToken.IsRevoked = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Unauthorized(new ProblemDetails { Title = "The account is inactive." });
        }

        storedToken.IsRevoked = true;
        var authResponse = CreateTokenResponse(storedToken.User);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            Token = HashRefreshToken(authResponse.RefreshToken),
            ExpiresAt = authResponse.RefreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(authResponse);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var response = CreateTokenResponse(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = HashRefreshToken(response.RefreshToken),
            ExpiresAt = response.RefreshTokenExpiresAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    private AuthResponse CreateTokenResponse(User user)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var rawRefreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            rawRefreshToken,
            DateTime.UtcNow.Add(RefreshTokenLifetime));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

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
