using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace KambaDiami.Api.Services;

/// <summary>
/// Limites por endereço IP nos endpoints públicos de autenticação, para travar ataques
/// de força bruta e o abuso do envio de emails. São largos de propósito: as redes móveis
/// angolanas partilham muitas vezes o mesmo IP público entre muitos clientes. A protecção
/// de cada conta vem do bloqueio após 5 tentativas falhadas (ver DependencyInjection).
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "auth-login";
    public const string Register = "auth-register";
    public const string PasswordReset = "auth-password-reset";
    public const string Locations = "locations-create";

    public static IServiceCollection AddKambaDiamiRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Demasiadas tentativas seguidas. Aguarda alguns minutos e tenta de novo." }, ct);
            };

            options.AddPolicy(Login, context => FixedWindowPerIp(context, permits: 30, TimeSpan.FromMinutes(1)));
            options.AddPolicy(Register, context => FixedWindowPerIp(context, permits: 20, TimeSpan.FromMinutes(10)));
            options.AddPolicy(PasswordReset, context => FixedWindowPerIp(context, permits: 10, TimeSpan.FromMinutes(15)));
            options.AddPolicy(Locations, context => FixedWindowPerIp(context, permits: 10, TimeSpan.FromMinutes(10)));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindowPerIp(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0
            });
}
