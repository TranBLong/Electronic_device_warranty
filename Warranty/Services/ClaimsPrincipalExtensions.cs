using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Warranty.Enums;

namespace Warranty.Services;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId) =>
        int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);

    public static bool TryGetUserRole(this ClaimsPrincipal principal, out UserRole role) =>
        Enum.TryParse(principal.FindFirstValue("role"), out role) && Enum.IsDefined(role);
}
