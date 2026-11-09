using KambaDiami.Domain.Entities;

namespace KambaDiami.Application.Abstractions;

public record GeneratedToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    Task<GeneratedToken> GenerateTokenAsync(ApplicationUser user);
}
