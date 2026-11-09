using KambaDiami.Domain.Enums;

namespace KambaDiami.Domain.Entities;

public class Match
{
    public int Id { get; set; }

    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = default!;

    public Guid InitiatorUserId { get; set; }
    public ApplicationUser InitiatorUser { get; set; } = default!;

    public Guid CounterpartUserId { get; set; }
    public ApplicationUser CounterpartUser { get; set; } = default!;

    public MatchStatus Status { get; set; } = MatchStatus.Pendente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Conversation? Conversation { get; set; }
    public Contract? Contract { get; set; }
}
