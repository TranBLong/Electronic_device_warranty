using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    TokenService tokenService) : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!PasswordHashing.CanHash(request.Password))
        {
            throw new BusinessRuleException("Password must be between 8 and 72 UTF-8 bytes.");
        }
        var email = NormalizeEmail(request.Email);
        if (await userRepository.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHashing.Hash(request.Password),
            Phone = request.Phone?.Trim(),
            Role = UserRole.Customer
        };
        await userRepository.AddAsync(user, cancellationToken);
        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConflictException("Email is already registered.");
        }

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !user.IsActive || !PasswordHashing.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var response = CreateTokenResponse(user);
        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = HashRefreshToken(response.RefreshToken),
            ExpiresAt = response.RefreshTokenExpiresAt
        }, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var suppliedTokenHash = HashRefreshToken(request.RefreshToken);
        var response = await refreshTokenRepository.RunInSerializableTransactionAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var storedToken = await refreshTokenRepository.GetByHashAsync(suppliedTokenHash, cancellationToken);
            if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt <= now)
            {
                return null;
            }

            if (!storedToken.User.IsActive)
            {
                storedToken.IsRevoked = true;
                await refreshTokenRepository.SaveChangesAsync(cancellationToken);
                return null;
            }

            storedToken.IsRevoked = true;
            var issued = CreateTokenResponse(storedToken.User);
            await refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = storedToken.UserId,
                Token = HashRefreshToken(issued.RefreshToken),
                ExpiresAt = issued.RefreshTokenExpiresAt
            }, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);
            return issued;
        }, cancellationToken);

        return response ?? throw new UnauthorizedException("Refresh token is invalid or expired.");
    }

    private AuthResponse CreateTokenResponse(User user)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            DateTime.UtcNow.Add(RefreshTokenLifetime));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static UserResponse ToResponse(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt);
}
