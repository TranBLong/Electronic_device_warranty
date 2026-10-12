using Warranty.Dtos;

namespace Warranty.Services.Interfaces.Important;

public interface IUserService
{
    Task<UserResponse> GetMeAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateMeAsync(
        int userId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default);
    Task DeactivateMeAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponse>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponse>> GetTechniciansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
