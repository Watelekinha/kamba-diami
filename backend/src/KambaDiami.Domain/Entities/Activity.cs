namespace KambaDiami.Domain.Entities;

/// <summary>
/// Cada tentativa de entrada na plataforma, bem ou mal sucedida.
/// Serve as estatísticas do painel e a auditoria de segurança.
/// </summary>
public class LoginEvent
{
    public long Id { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Email { get; set; } = default!;
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Um registo por utilizador e por dia (hora de Angola) em que fez pedidos autenticados.
/// Permite medir utilizadores activos mesmo quando a sessão dura vários dias.
/// </summary>
public class UserDailyActivity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public DateOnly Day { get; set; }
}

public class AdminAuditLog
{
    public long Id { get; set; }

    public Guid AdminUserId { get; set; }
    public ApplicationUser AdminUser { get; set; } = default!;

    public string Action { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string? TargetId { get; set; }
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
