using KambaDiami.Domain.Enums;

namespace KambaDiami.Domain.Entities;

public class PaymentReference
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = default!;

    public string ReferenceCode { get; set; } = default!;
    public decimal Amount { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public bool MarkedPaidByFamilia { get; set; }
    public DateTime? MarkedPaidByFamiliaAt { get; set; }

    public bool MarkedPaidBySecretaria { get; set; }
    public DateTime? MarkedPaidBySecretariaAt { get; set; }

    public PaymentReferenceStatus Status { get; set; } = PaymentReferenceStatus.Pendente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
