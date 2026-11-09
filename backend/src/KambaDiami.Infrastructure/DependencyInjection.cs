using System.Security.Claims;
using System.Text;
using FluentValidation;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Validators;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Email;
using KambaDiami.Infrastructure.Files;
using KambaDiami.Infrastructure.Identity;
using KambaDiami.Infrastructure.Locations;
using KambaDiami.Infrastructure.Pdf;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KambaDiami.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<KambaDiamiDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = PasswordRules.MinimumLength;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<KambaDiamiDbContext>()
            .AddSignInManager()
            .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        // As ligações de recuperação de palavra-passe valem 2 horas.
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(2));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Falta a secção de configuração 'Jwt'.");
        if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret tem de ter pelo menos 32 caracteres.");
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        // Um token continua assinado e válido até expirar; aqui confirma-se que a conta
                        // não foi suspensa e que a palavra-passe não mudou desde que foi emitido.
                        var principal = context.Principal!;
                        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                        {
                            context.Fail("Token sem identificador de utilizador.");
                            return;
                        }

                        var sessions = context.HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
                        var stamp = principal.FindFirstValue(TokenService.SecurityStampClaim);
                        if (!await sessions.IsSessionValidAsync(userId, stamp, context.HttpContext.RequestAborted))
                        {
                            context.Fail("A sessão já não é válida.");
                            return;
                        }

                        await sessions.TrackActivityAsync(userId, context.HttpContext.RequestAborted);
                    }
                };
            });

        services.AddAuthorization();
        services.AddMemoryCache();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        if (emailSettings.IsSmtpConfigured)
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, LoggingEmailSender>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IContractPdfService, ContractPdfService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserSessionService, UserSessionService>();

        // Províncias e municípios vêm da API pública de Angola; ver AngolaLocationsClient.
        var locationsBaseUrl = configuration["AngolaLocations:BaseUrl"] ?? "https://angolaprovinciasapi.ao/api/v2/";
        services.AddHttpClient<IAngolaLocationsProvider, AngolaLocationsClient>(client =>
        {
            client.BaseAddress = new Uri(locationsBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
