using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Warranty.Models;

namespace Warranty.Services;

public sealed class TokenService
{
    private readonly byte[] _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public TokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 UTF-8 bytes.");
        }

        _signingKey = Encoding.UTF8.GetBytes(key);
        _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(_signingKey), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            ],
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
