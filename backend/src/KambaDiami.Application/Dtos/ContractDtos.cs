using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Dtos;

public class ContractPartyDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Address { get; set; } = default!;
    public UserRole Role { get; set; }
}

public class ContractClauseDto
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}

public class ContractDto
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int ConversationId { get; set; }
    public string JobPostingSummary { get; set; } = default!;
    public ContractStatus Status { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public bool CanEdit { get; set; }
    public bool HasGeneratedPdf { get; set; }
    public bool FamiliaSigned { get; set; }
    public bool SecretariaSigned { get; set; }
    public bool CurrentUserSigned { get; set; }
    public bool CanRate { get; set; }
    public bool CanConfirmEnd { get; set; }
    public bool EndConfirmedByFamilia { get; set; }
    public bool EndConfirmedBySecretaria { get; set; }
    public bool CurrentUserConfirmedEnd { get; set; }
    public bool CurrentUserIsFamilia { get; set; }
    public bool CanUploadSigned { get; set; }
    public bool CanCancel { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByName { get; set; }
    public bool CancelledByMe { get; set; }
    public string? CancellationReason { get; set; }
    public string CounterpartName { get; set; } = default!;
    public RatingDto? MyRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ContractPartyDto Familia { get; set; } = default!;
    public ContractPartyDto Secretaria { get; set; } = default!;
    public List<ContractClauseDto> Clauses { get; set; } = [];
}

public class ContractListItemDto
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int ConversationId { get; set; }
    public string JobPostingSummary { get; set; } = default!;
    public ContractStatus Status { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public string CounterpartName { get; set; } = default!;
    public Guid CounterpartUserId { get; set; }
    public bool CanRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CreateContractRequest
{
    public int MatchId { get; set; }
}

public class ContractClauseInput
{
    public int? Id { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}

public class CancelContractRequest
{
    public string Reason { get; set; } = default!;
}

public class UpdateContractClausesRequest
{
    public List<ContractClauseInput> Clauses { get; set; } = [];
}
