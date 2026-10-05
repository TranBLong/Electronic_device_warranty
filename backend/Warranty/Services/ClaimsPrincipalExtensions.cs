using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Warranty.Services;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId) =>
        int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);
}
