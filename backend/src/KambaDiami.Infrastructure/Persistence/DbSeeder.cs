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

    /// <summary>
    /// As províncias e os municípios vêm da API pública de Angola e não são guardados. Só se garante um
    /// bairro inicial, porque a conta do primeiro administrador precisa de uma morada; os restantes bairros
    /// são criados pelos utilizadores (ver LocationsController). Os slugs são os da API angolaprovinciasapi.ao.
    /// </summary>
    private static async Task SeedLocationsAsync(KambaDiamiDbContext db)
    {
        if (await db.Neighborhoods.AnyAsync()) return;

        db.Neighborhoods.Add(new Neighborhood
        {
            Name = "Ingombota",
            MunicipalitySlug = "ingombota",
            MunicipalityName = "Ingombota",
            ProvinceSlug = "luanda",
            ProvinceName = "Luanda"
        });
        await db.SaveChangesAsync();
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

        var section = services.GetRequiredService<IConfiguration>().GetSection("BootstrapAdmin");
        var email = section["Email"]?.Trim().ToLowerInvariant();
        var password = section["Password"];

        var hasAdmin = await userManager.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (hasAdmin)
        {
            if (string.Equals(section["ResetPassword"], "true", StringComparison.OrdinalIgnoreCase))
                await ResetBootstrapAdminPasswordAsync(userManager, email, password, logger);
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Não existe nenhum administrador. Define BootstrapAdmin:Email e BootstrapAdmin:Password " +
                "na configuração e reinicia a API para o criar.");
            return;
        }

        var neighborhood = await db.Neighborhoods
            .OrderBy(n => n.ProvinceSlug == "luanda" ? 0 : 1)
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

    /// <summary>
    /// Recuperação de acesso: com BootstrapAdmin:ResetPassword=true, a palavra-passe do administrador
    /// com o email configurado volta a ser a da configuração e o bloqueio por tentativas é levantado.
    /// Deve ser desligada depois de usada, para não sobrepor mudanças feitas na aplicação.
    /// </summary>
    private static async Task ResetBootstrapAdminPasswordAsync(
        UserManager<ApplicationUser> userManager, string? email, string? password, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("BootstrapAdmin:ResetPassword está ativo, mas faltam Email ou Password.");
            return;
        }

        var admin = await userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
        if (admin is null || admin.Role != UserRole.Admin)
        {
            var existing = await userManager.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Email).ToListAsync();
            logger.LogWarning(
                "BootstrapAdmin:ResetPassword: não há administrador com o email {Email}. Administradores existentes: {Admins}",
                email, string.Join(", ", existing));
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(admin);
        var result = await userManager.ResetPasswordAsync(admin, token, password);
        if (!result.Succeeded)
        {
            logger.LogError("Não foi possível repor a palavra-passe do administrador: {Errors}",
                string.Join(" ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.SetLockoutEndDateAsync(admin, null);
        await userManager.ResetAccessFailedCountAsync(admin);
        logger.LogInformation("Palavra-passe do administrador {Email} reposta a partir da configuração.", email);
    }
}
