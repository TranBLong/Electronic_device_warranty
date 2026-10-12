using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class UserService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository) : IUserService
{
    public async Task<UserResponse> GetMeAsync(int userId, CancellationToken cancellationToken = default) =>
        ToResponse(await GetUserAsync(userId, cancellationToken));

    public async Task<UserResponse> UpdateMeAsync(
        int userId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken, tracking: true);
        var email = NormalizeEmail(request.Email);
        if (await userRepository.EmailExistsAsync(email, userId, cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        await SaveUserAsync("Email is already registered.", cancellationToken);
        return ToResponse(user);
    }

    public async Task DeactivateMeAsync(int userId, CancellationToken cancellationToken = default) =>
        await DeactivateUserAsync(userId, cancellationToken);

    public async Task<IReadOnlyList<UserResponse>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        ToResponses(await userRepository.GetActiveByRoleAsync(UserRole.Customer, cancellationToken));

    public async Task<IReadOnlyList<UserResponse>> GetTechniciansAsync(
        CancellationToken cancellationToken = default) =>
        ToResponses(await userRepository.GetActiveByRoleAsync(UserRole.Technician, cancellationToken));

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ToResponses(await userRepository.GetAllAsync(cancellationToken));

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ToResponse(await GetUserAsync(id, cancellationToken));

    public async Task<UserResponse> CreateAsync(
        AdminCreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Role is null || !Enum.IsDefined(request.Role.Value))
        {
            throw new BusinessRuleException("Role is invalid.");
        }

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
            Role = request.Role.Value,
            IsActive = request.IsActive
        };
        await userRepository.AddAsync(user, cancellationToken);
        await SaveUserAsync("Email is already registered.", cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Role is null || !Enum.IsDefined(request.Role.Value))
        {
            throw new BusinessRuleException("Role is invalid.");
        }

        var user = await GetUserAsync(id, cancellationToken, tracking: true);
        var changePassword = !string.IsNullOrWhiteSpace(request.NewPassword);
        if (changePassword)
        {
            if (!PasswordHashing.CanHash(request.NewPassword!))
            {
                throw new BusinessRuleException("Password must be between 8 and 72 UTF-8 bytes.");
            }
        }

        var email = NormalizeEmail(request.Email);
        if (await userRepository.EmailExistsAsync(email, id, cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        var credentialsChanged = user.Role != request.Role.Value || user.IsActive != request.IsActive || changePassword;
        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        user.Role = request.Role.Value;
        user.IsActive = request.IsActive;
        if (changePassword)
        {
            user.PasswordHash = PasswordHashing.Hash(request.NewPassword!);
        }

        if (credentialsChanged)
        {
            await refreshTokenRepository.MarkActiveAsRevokedAsync(user.Id, cancellationToken);
        }

        await SaveUserAsync("Email is already registered.", cancellationToken);
        return ToResponse(user);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default) =>
        await DeactivateUserAsync(id, cancellationToken);

    private async Task DeactivateUserAsync(int id, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(id, cancellationToken, tracking: true);
        user.IsActive = false;
        await refreshTokenRepository.MarkActiveAsRevokedAsync(id, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserAsync(
        int id,
        CancellationToken cancellationToken,
        bool tracking = false) =>
        await userRepository.GetByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("User was not found.");

    private async Task SaveUserAsync(string conflictMessage, CancellationToken cancellationToken)
    {
        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConflictException(conflictMessage);
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static UserResponse ToResponse(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt);

    private static IReadOnlyList<UserResponse> ToResponses(IEnumerable<User> users) =>
        users.Select(ToResponse).ToList();
}
