using KambaDiami.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace KambaDiami.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PhotoPath { get; set; }
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Ativo;

    public int AddressId { get; set; }
    public Address Address { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public DateTime? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }

    public FamiliaProfile? FamiliaProfile { get; set; }
    public SecretariaProfile? SecretariaProfile { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

public class FamiliaProfile
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public int? NumeroAgregadoFamiliar { get; set; }
    public string? Descricao { get; set; }
}

public class SecretariaProfile
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public string? Bio { get; set; }
    public int? AnosExperiencia { get; set; }
}
