using KambaDiami.Application.Abstractions;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Infrastructure.Persistence;
using KambaDiami.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace KambaDiami.Infrastructure.Identity;

public class UserSessionService : IUserSessionService
{
    // O estado da conta fica em cache durante pouco tempo para não ir à base de dados
    // em todos os pedidos. Suspender uma conta chama Invalidate, por isso tem efeito imediato.
    private static readonly TimeSpan StateCacheDuration = TimeSpan.FromSeconds(60);

    private readonly KambaDiamiDbContext _db;
    private readonly IMemoryCache _cache;

    public UserSessionService(KambaDiamiDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private sealed record UserState(string? SecurityStamp, AccountStatus Status);

    private static string StateKey(Guid userId) => $"kd:user-state:{userId:N}";
    private static string ActivityKey(Guid userId, DateOnly day) => $"kd:active:{userId:N}:{day:yyyyMMdd}";

    public async Task<bool> IsSessionValidAsync(Guid userId, string? securityStamp, CancellationToken ct = default)
    {
        if (!_cache.TryGetValue(StateKey(userId), out UserState? state))
        {
            state = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserState(u.SecurityStamp, u.Status))
                .FirstOrDefaultAsync(ct);

            if (state is null) return false;
            _cache.Set(StateKey(userId), state, StateCacheDuration);
        }

        return state!.Status != AccountStatus.Suspenso
               && !string.IsNullOrEmpty(securityStamp)
               && string.Equals(state.SecurityStamp, securityStamp, StringComparison.Ordinal);
    }

    public async Task TrackActivityAsync(Guid userId, CancellationToken ct = default)
    {
        var today = AngolaTime.Today;
        var key = ActivityKey(userId, today);
        if (_cache.TryGetValue(key, out _)) return;

        var exists = await _db.UserDailyActivities.AnyAsync(a => a.UserId == userId && a.Day == today, ct);
        if (!exists)
        {
            _db.UserDailyActivities.Add(new UserDailyActivity { UserId = userId, Day = today });
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Dois pedidos em simultâneo já criaram o registo do dia.
            }
        }

        _cache.Set(key, true, TimeSpan.FromHours(26));
    }

    public void Invalidate(Guid userId) => _cache.Remove(StateKey(userId));
}
