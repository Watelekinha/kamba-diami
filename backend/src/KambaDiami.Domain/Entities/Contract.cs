using KambaDiami.Domain.Enums;

namespace KambaDiami.Domain.Entities;

public class Contract
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = default!;

    public Guid FamiliaUserId { get; set; }
    public ApplicationUser FamiliaUser { get; set; } = default!;

    public Guid SecretariaUserId { get; set; }
    public ApplicationUser SecretariaUser { get; set; } = default!;

    public string TermsText { get; set; } = default!;
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }

    public string? GeneratedPdfPath { get; set; }
    public string? SignedPdfPathFamilia { get; set; }
    public string? SignedPdfPathSecretaria { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Rascunho;

    public bool EndConfirmedByFamilia { get; set; }
    public bool EndConfirmedBySecretaria { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PaymentReference> PaymentReferences { get; set; } = new List<PaymentReference>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<ContractClause> Clauses { get; set; } = new List<ContractClause>();
}

public class ContractClause
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = default!;

    public int SortOrder { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}
