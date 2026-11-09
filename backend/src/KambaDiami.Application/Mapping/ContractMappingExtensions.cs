using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Mapping;

public static class ContractMappingExtensions
{
    /// <summary>Só se cancela antes de entrar em vigor; um contrato activo termina com a confirmação das duas partes.</summary>
    public static bool CanBeCancelled(this Contract contract) =>
        contract.Status is ContractStatus.Rascunho or ContractStatus.PdfGerado or ContractStatus.AguardandoAssinaturas;

    public static ContractDto ToDto(this Contract contract, Guid currentUserId)
    {
        var isFamilia = currentUserId == contract.FamiliaUserId;

        // Só a família prepara as cláusulas e gera o PDF.
        var canEdit = isFamilia
                      && contract.SignedPdfPathFamilia is null
                      && contract.SignedPdfPathSecretaria is null
                      && contract.Status is ContractStatus.Rascunho or ContractStatus.PdfGerado;

        var currentUserSigned = isFamilia
            ? contract.SignedPdfPathFamilia is not null
            : contract.SignedPdfPathSecretaria is not null;

        // A família assina primeiro; a secretária do lar só assina depois, sobre o PDF já assinado pela família.
        var openForSignatures = !string.IsNullOrWhiteSpace(contract.GeneratedPdfPath)
                                && contract.Status is ContractStatus.PdfGerado or ContractStatus.AguardandoAssinaturas;
        var canUploadSigned = openForSignatures && (isFamilia || contract.SignedPdfPathFamilia is not null);
        var myRating = contract.Ratings.FirstOrDefault(r => r.RaterUserId == currentUserId);
        var canRate = contract.Status is ContractStatus.Ativo or ContractStatus.Concluido && myRating is null;
        var currentUserConfirmedEnd = isFamilia ? contract.EndConfirmedByFamilia : contract.EndConfirmedBySecretaria;
        var cancelledBy = contract.CancelledByUserId == contract.FamiliaUserId ? contract.FamiliaUser
            : contract.CancelledByUserId == contract.SecretariaUserId ? contract.SecretariaUser
            : null;

        return new ContractDto
        {
            Id = contract.Id,
            MatchId = contract.MatchId,
            ConversationId = contract.Match.Conversation?.Id ?? 0,
            JobPostingSummary = contract.Match.JobPosting.Categories.Count > 0
                ? string.Join(", ", contract.Match.JobPosting.Categories.Select(c => c.ServiceCategory.Name))
                : "Serviço doméstico",
            Status = contract.Status,
            PaymentType = contract.PaymentType,
            PaymentValue = contract.PaymentValue,
            CanEdit = canEdit,
            HasGeneratedPdf = !string.IsNullOrWhiteSpace(contract.GeneratedPdfPath),
            FamiliaSigned = contract.SignedPdfPathFamilia is not null,
            SecretariaSigned = contract.SignedPdfPathSecretaria is not null,
            CurrentUserSigned = currentUserSigned,
            CanRate = canRate,
            CanConfirmEnd = contract.Status == ContractStatus.Ativo && !currentUserConfirmedEnd,
            EndConfirmedByFamilia = contract.EndConfirmedByFamilia,
            EndConfirmedBySecretaria = contract.EndConfirmedBySecretaria,
            CurrentUserConfirmedEnd = currentUserConfirmedEnd,
            CurrentUserIsFamilia = isFamilia,
            CanUploadSigned = canUploadSigned,
            CanCancel = contract.CanBeCancelled(),
            CancelledAt = contract.CancelledAt,
            CancelledByName = cancelledBy is null ? null : $"{cancelledBy.FirstName} {cancelledBy.LastName}",
            CancelledByMe = contract.CancelledByUserId == currentUserId,
            CancellationReason = contract.CancellationReason,
            CounterpartName = isFamilia ? $"{contract.SecretariaUser.FirstName} {contract.SecretariaUser.LastName}"
                : $"{contract.FamiliaUser.FirstName} {contract.FamiliaUser.LastName}",
            MyRating = myRating?.ToDto(),
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt,
            Familia = contract.FamiliaUser.ToPartyDto(),
            Secretaria = contract.SecretariaUser.ToPartyDto(),
            Clauses = contract.Clauses
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .Select(c => new ContractClauseDto
                {
                    Id = c.Id,
                    SortOrder = c.SortOrder,
                    Title = c.Title,
                    Body = c.Body
                })
                .ToList()
        };
    }

    public static ContractListItemDto ToListItemDto(this Contract contract, Guid currentUserId)
    {
        var isFamilia = currentUserId == contract.FamiliaUserId;
        var counterpart = isFamilia ? contract.SecretariaUser : contract.FamiliaUser;
        var myRating = contract.Ratings.FirstOrDefault(r => r.RaterUserId == currentUserId);

        return new ContractListItemDto
        {
            Id = contract.Id,
            MatchId = contract.MatchId,
            ConversationId = contract.Match.Conversation?.Id ?? 0,
            JobPostingSummary = contract.Match.JobPosting.Categories.Count > 0
                ? string.Join(", ", contract.Match.JobPosting.Categories.Select(c => c.ServiceCategory.Name))
                : "Serviço doméstico",
            Status = contract.Status,
            PaymentType = contract.PaymentType,
            PaymentValue = contract.PaymentValue,
            CounterpartName = $"{counterpart.FirstName} {counterpart.LastName}",
            CounterpartUserId = counterpart.Id,
            CanRate = contract.Status is ContractStatus.Ativo or ContractStatus.Concluido && myRating is null,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt,
            EndDate = contract.EndDate
        };
    }

    public static RatingDto ToDto(this Rating rating) =>
        new()
        {
            Id = rating.Id,
            ContractId = rating.ContractId,
            Score = rating.Score,
            Comment = rating.Comment,
            CreatedAt = rating.CreatedAt,
            RaterUserId = rating.RaterUserId,
            RaterName = rating.RaterUser is null
                ? "Utilizador"
                : $"{rating.RaterUser.FirstName} {rating.RaterUser.LastName}"
        };

    public static ContractPartyDto ToPartyDto(this ApplicationUser user) =>
        new()
        {
            UserId = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            Address = FormatAddress(user),
            Role = user.Role
        };

    public static (ApplicationUser Familia, ApplicationUser Secretaria) ResolveDomesticParties(this Match match)
    {
        var users = new[] { match.InitiatorUser, match.CounterpartUser };
        var familia = users.FirstOrDefault(u => u.Role == UserRole.Familia);
        var secretaria = users.FirstOrDefault(u => u.Role == UserRole.Secretaria);
        if (familia is null || secretaria is null)
            throw new InvalidOperationException("O interesse deve ser entre uma família e uma secretária do lar.");
        return (familia, secretaria);
    }

    public static string SnapshotTerms(IEnumerable<ContractClause> clauses) =>
        string.Join("\n\n", clauses
            .OrderBy(c => c.SortOrder)
            .Select(c => $"{c.Title}\n{c.Body}"));

    private static string FormatAddress(ApplicationUser user)
    {
        var address = user.Address;
        var neighborhood = address?.Neighborhood;
        if (address is null || neighborhood is null) return "Morada registada na plataforma";
        var street = string.IsNullOrWhiteSpace(address.StreetDetail) ? "" : $", {address.StreetDetail}";
        return $"{neighborhood.Name}, {neighborhood.MunicipalityName}, {neighborhood.ProvinceName}{street}";
    }
}
