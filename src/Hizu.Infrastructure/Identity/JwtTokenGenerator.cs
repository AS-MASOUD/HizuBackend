using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hizu.Application.Common.Interfaces;
using Hizu.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Hizu.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<(string Token, DateTime Expiration)> GenerateTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"] ?? "HizuSuperSecretKeyMustBeAtLeast32CharsLongForHS256!";
        var issuer = jwtSettings["Issuer"] ?? "HizuApi";
        var audience = jwtSettings["Audience"] ?? "HizuClient";
        var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var exp) ? exp : 1440; // Default 24h

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiration = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Task.FromResult((tokenHandler.WriteToken(token), expiration));
    }
}
