using Hizu.Domain.Entities;

namespace Hizu.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    Task<(string Token, DateTime Expiration)> GenerateTokenAsync(ApplicationUser user, IList<string> roles);
}
