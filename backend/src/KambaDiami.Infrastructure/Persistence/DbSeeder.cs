using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KambaDiami.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<KambaDiamiDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedLocationsAsync(db);
        await SeedServiceCategoriesAsync(db);
        await SeedBootstrapAdminAsync(db, userManager, services);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }
    }

    // As 21 províncias resultantes da reorganização administrativa de Angola de 2024.
    // Os municípios/bairros só estão preenchidos para Luanda por agora; as restantes
    // províncias ficam disponíveis para selecção mas sem dados de município ainda —
    // os utilizadores podem adicionar bairros pela aplicação (ver LocationsController).
    private static readonly string[] ProvinceNames =
    [
        "Bengo",
        "Benguela",
        "Bié",
        "Cabinda",
        "Cuando",
        "Cubango",
        "Cuanza Norte",
        "Cuanza Sul",
        "Cunene",
        "Huambo",
        "Huíla",
        "Icolo e Bengo",
        "Luanda",
        "Lunda Norte",
        "Lunda Sul",
        "Malanje",
        "Moxico",
        "Moxico Leste",
        "Namibe",
        "Uíge",
        "Zaire"
    ];

    private static async Task SeedLocationsAsync(KambaDiamiDbContext db)
    {
        if (!await db.Provinces.AnyAsync(p => p.Name == "Luanda"))
        {
            db.Provinces.Add(new Province
            {
                Name = "Luanda",
                Municipalities = new List<Municipality>
                {
                    new()
                    {
                        Name = "Luanda",
                        Neighborhoods = new List<Neighborhood>
                        {
                            new() { Name = "Maianga" },
                            new() { Name = "Ingombota" }
                        }
                    },
                    new()
                    {
                        Name = "Talatona",
                        Neighborhoods = new List<Neighborhood>
                        {
                            new() { Name = "Talatona Centro" },
                            new() { Name = "Benfica" }
                        }
                    },
                    new()
                    {
                        Name = "Viana",
                        Neighborhoods = new List<Neighborhood>
                        {
                            new() { Name = "Viana Centro" },
                            new() { Name = "Zango" }
                        }
                    }
                }
            });
            await db.SaveChangesAsync();
        }

        var existingNames = await db.Provinces.Select(p => p.Name).ToListAsync();
        var missingProvinces = ProvinceNames
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Province { Name = name })
            .ToList();

        if (missingProvinces.Count > 0)
        {
            db.Provinces.AddRange(missingProvinces);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedServiceCategoriesAsync(KambaDiamiDbContext db)
    {
        if (await db.ServiceCategories.AnyAsync()) return;

        string[] names =
        [
            "Limpeza Geral",
            "Cozinha",
            "Lavagem de Roupa",
            "Engomar",
            "Babá / Cuidado de Crianças",
            "Cuidado de Idosos",
            "Jardinagem",
            "Motorista Doméstico",
            "Outro"
        ];

        db.ServiceCategories.AddRange(names.Select(n => new ServiceCategory { Name = n }));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Cria o primeiro administrador quando ainda não existe nenhum. Os dados vêm da configuração
    /// (secção BootstrapAdmin: user-secrets em desenvolvimento, variáveis de ambiente em produção),
    /// nunca do código. Depois de existir um administrador, esta configuração é ignorada.
    /// </summary>
    private static async Task SeedBootstrapAdminAsync(
        KambaDiamiDbContext db,
        UserManager<ApplicationUser> userManager,
        IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

        var hasAdmin = await userManager.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (hasAdmin) return;

        var section = services.GetRequiredService<IConfiguration>().GetSection("BootstrapAdmin");
        var email = section["Email"]?.Trim().ToLowerInvariant();
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Não existe nenhum administrador. Define BootstrapAdmin:Email e BootstrapAdmin:Password " +
                "na configuração e reinicia a API para o criar.");
            return;
        }

        var neighborhood = await db.Neighborhoods
            .OrderBy(n => n.Municipality.Province.Name == "Luanda" ? 0 : 1)
            .ThenBy(n => n.Id)
            .FirstAsync();

        var address = new Address { NeighborhoodId = neighborhood.Id };
        db.Addresses.Add(address);
        await db.SaveChangesAsync();

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = AngolaPhone.Normalize(section["PhoneNumber"]) ?? string.Empty,
            FirstName = section["FirstName"] ?? "Administrador",
            LastName = section["LastName"] ?? "Kamba Diami",
            Role = UserRole.Admin,
            Status = AccountStatus.Ativo,
            AddressId = address.Id
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            db.Addresses.Remove(address);
            await db.SaveChangesAsync();
            throw new InvalidOperationException(
                "Não foi possível criar o administrador inicial: " +
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
        logger.LogInformation("Administrador inicial criado: {Email}", email);
    }
}
