using System.Security.Claims;
using FluentValidation;
using KambaDiami.Api.Hubs;
using KambaDiami.Api.Services;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Contracts;
using KambaDiami.Application.Dtos;
using KambaDiami.Application.Mapping;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize(Roles = "Familia,Secretaria")]
public class ContractsController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;
    private readonly IContractPdfService _pdf;
    private readonly IFileStorageService _files;
    private readonly IValidator<UpdateContractClausesRequest> _clausesValidator;
    private readonly IValidator<CreateRatingRequest> _ratingValidator;

    public ContractsController(
        KambaDiamiDbContext db,
        IContractPdfService pdf,
        IFileStorageService files,
        IValidator<UpdateContractClausesRequest> clausesValidator,
        IValidator<CreateRatingRequest> ratingValidator)
    {
        _db = db;
        _pdf = pdf;
        _files = files;
        _clausesValidator = clausesValidator;
        _ratingValidator = ratingValidator;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<ContractDto>> Create(CreateContractRequest request)
    {
        var match = await MatchQuery().FirstOrDefaultAsync(m => m.Id == request.MatchId);
        if (match is null) return NotFound(new { message = "Interesse não encontrado." });
        if (!IsParty(match)) return Forbid();
        if (match.Status != MatchStatus.Aceite)
            return BadRequest(new { message = "Só é possível criar o contrato depois de o interesse ser aceite." });

        if (match.Contract is not null)
            return Ok(await LoadDtoAsync(match.Contract.Id));

        ApplicationUser familia;
        ApplicationUser secretaria;
        try
        {
            (familia, secretaria) = match.ResolveDomesticParties();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        // O contrato é preparado pela família (entidade empregadora); a secretária do lar vê-o e assina-o depois.
        if (familia.Id != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = OnlyFamiliaMessage });

        var clauses = DefaultContractClauses.Build(familia, secretaria, match.JobPosting);
        var contract = new Contract
        {
            MatchId = match.Id,
            FamiliaUserId = familia.Id,
            SecretariaUserId = secretaria.Id,
            PaymentType = match.JobPosting.PaymentType,
            PaymentValue = match.JobPosting.PaymentValue,
            Status = ContractStatus.Rascunho,
            TermsText = ContractMappingExtensions.SnapshotTerms(clauses),
            Clauses = clauses
        };

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, await LoadDtoAsync(contract.Id));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<ContractListItemDto>>> GetMine()
    {
        var contracts = await ContractListQuery()
            .Where(c => c.FamiliaUserId == CurrentUserId || c.SecretariaUserId == CurrentUserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return Ok(contracts.Select(c => c.ToListItemDto(CurrentUserId)).ToList());
    }

    [HttpGet("by-match/{matchId:int}")]
    public async Task<ActionResult<ContractDto>> GetByMatch(int matchId)
    {
        var contract = await ContractDetailQuery(tracking: false).FirstOrDefaultAsync(c => c.MatchId == matchId);
        if (contract is null) return NotFound(new { message = "Ainda não existe contrato para este interesse." });
        if (!IsParty(contract)) return Forbid();
        return Ok(contract.ToDto(CurrentUserId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> GetById(int id)
    {
        var contract = await ContractDetailQuery(tracking: false).FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        return Ok(contract.ToDto(CurrentUserId));
    }

    [HttpPut("{id:int}/clauses")]
    public async Task<ActionResult<ContractDto>> UpdateClauses(int id, UpdateContractClausesRequest request)
    {
        var validation = await _clausesValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var contract = await _db.Contracts
            .Include(c => c.Clauses)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        if (!IsFamilia(contract))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Só a família pode alterar as cláusulas. Combinem as alterações na conversa." });
        if (!CanEdit(contract))
            return BadRequest(new { message = "Já não é possível alterar as cláusulas depois de um dos lados carregar o contrato assinado." });

        _db.ContractClauses.RemoveRange(contract.Clauses);
        contract.Clauses.Clear();
        await _db.SaveChangesAsync();

        var order = 1;
        foreach (var input in request.Clauses)
        {
            contract.Clauses.Add(new ContractClause
            {
                SortOrder = order++,
                Title = input.Title.Trim(),
                Body = input.Body.Trim()
            });
        }

        contract.TermsText = ContractMappingExtensions.SnapshotTerms(contract.Clauses);
        contract.GeneratedPdfPath = null;
        contract.Status = ContractStatus.Rascunho;
        contract.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await LoadDtoAsync(contract.Id));
    }

    [HttpPost("{id:int}/generate-pdf")]
    public async Task<IActionResult> GeneratePdf(int id)
    {
        var contract = await ContractPdfQuery().FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        if (!IsFamilia(contract))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = OnlyFamiliaMessage });
        if (contract.Clauses.Count == 0)
            return BadRequest(new { message = "Adiciona pelo menos uma cláusula antes de gerar o PDF." });
        if (contract.SignedPdfPathFamilia is not null || contract.SignedPdfPathSecretaria is not null)
            return BadRequest(new { message = "Já não é possível gerar um novo PDF depois de um dos lados carregar o contrato assinado." });
        if (contract.Status is ContractStatus.Cancelado or ContractStatus.Concluido or ContractStatus.Ativo)
            return BadRequest(new { message = "Este contrato já não pode gerar um novo PDF." });

        var bytes = _pdf.Generate(contract);
        var path = await _files.SaveGeneratedContractAsync(contract.Id, bytes);
        contract.GeneratedPdfPath = path;
        contract.TermsText = ContractMappingExtensions.SnapshotTerms(contract.Clauses);
        contract.Status = ContractStatus.PdfGerado;
        contract.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return File(bytes, "application/pdf", FileName(contract.Id));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var contract = await ContractPdfQuery().FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();

        if (string.IsNullOrWhiteSpace(contract.GeneratedPdfPath))
            return BadRequest(new { message = "Ainda não foi gerado o PDF deste contrato." });

        var absolute = _files.GetAbsolutePath(contract.GeneratedPdfPath);
        if (!System.IO.File.Exists(absolute))
        {
            var regenerated = _pdf.Generate(contract);
            return File(regenerated, "application/pdf", FileName(contract.Id));
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(absolute);
        return File(bytes, "application/pdf", FileName(contract.Id));
    }

    [HttpPost("{id:int}/signed")]
    public async Task<ActionResult<ContractDto>> UploadSigned(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Selecciona o PDF do contrato assinado." });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "O ficheiro excede o limite de 10MB." });
        // Confirma-se pelo conteúdo, e não pelo nome ou pelo tipo declarado, que o ficheiro é mesmo um PDF.
        if (!await FileSignatures.IsPdfAsync(file))
            return BadRequest(new { message = "Carrega um ficheiro PDF." });

        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        if (contract.Status is ContractStatus.Cancelado or ContractStatus.Concluido or ContractStatus.Ativo)
            return BadRequest(new { message = "Este contrato já não aceita novos documentos." });
        if (string.IsNullOrWhiteSpace(contract.GeneratedPdfPath))
            return BadRequest(new { message = "Gera e descarrega o PDF antes de carregar o contrato assinado." });

        // As assinaturas são sequenciais: a família assina primeiro e a secretária do lar assina esse mesmo
        // documento, para que o PDF final tenha as duas assinaturas.
        var isFamilia = contract.FamiliaUserId == CurrentUserId;
        if (!isFamilia && contract.SignedPdfPathFamilia is null)
            return BadRequest(new { message = "A família tem de assinar primeiro. Quando ela carregar o contrato assinado, descarrega-o, assina-o e carrega-o aqui." });
        var partyLabel = isFamilia ? "familia" : "secretaria";

        await using var stream = file.OpenReadStream();
        var path = await _files.SaveSignedContractAsync(contract.Id, partyLabel, stream, file.FileName);

        if (isFamilia)
        {
            contract.SignedPdfPathFamilia = path;
            // Uma assinatura da secretária feita antes deste documento (ordem antiga) não tem a assinatura da
            // família; a secretária tem de assinar o documento novo.
            contract.SignedPdfPathSecretaria = null;
        }
        else
        {
            contract.SignedPdfPathSecretaria = path;
        }

        if (contract.SignedPdfPathFamilia is not null && contract.SignedPdfPathSecretaria is not null)
            contract.Status = ContractStatus.Ativo;
        else
            contract.Status = ContractStatus.AguardandoAssinaturas;

        contract.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await LoadDtoAsync(contract.Id));
    }

    [HttpGet("{id:int}/signed/{party}")]
    public async Task<IActionResult> DownloadSigned(int id, string party)
    {
        var contract = await _db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();

        var path = SignedPath(contract, party);
        if (path is null)
            return BadRequest(new { message = "Essa parte ainda não carregou o contrato assinado." });

        var bytes = await ReadStoredPdfAsync(path);
        if (bytes is null)
            return NotFound(new { message = "O ficheiro assinado já não está disponível no servidor." });

        return File(bytes, "application/pdf", SignedFileName(contract.Id, party));
    }

    [HttpGet("{id:int}/signed")]
    public async Task<IActionResult> DownloadSignedByBoth(int id)
    {
        var contract = await _db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();

        if (contract.SignedPdfPathFamilia is null || contract.SignedPdfPathSecretaria is null)
            return BadRequest(new { message = "O contrato só pode ser descarregado completo depois de ambas as partes carregarem o PDF assinado." });

        // A secretária do lar assina o documento já assinado pela família: o ficheiro dela é o contrato final.
        var finalBytes = await ReadStoredPdfAsync(contract.SignedPdfPathSecretaria);
        if (finalBytes is null)
            return NotFound(new { message = "O contrato assinado já não está disponível no servidor." });

        return File(finalBytes, "application/pdf", $"contrato-kamba-diami-KD-{contract.Id:D6}-assinado-pelas-partes.pdf");
    }

    [HttpPost("{id:int}/confirm-end")]
    public async Task<ActionResult<ContractDto>> ConfirmEnd(int id)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        if (contract.Status != ContractStatus.Ativo)
            return BadRequest(new { message = "Só podes confirmar o fim de um contrato activo." });

        if (contract.FamiliaUserId == CurrentUserId)
            contract.EndConfirmedByFamilia = true;
        else
            contract.EndConfirmedBySecretaria = true;

        if (contract.EndConfirmedByFamilia && contract.EndConfirmedBySecretaria)
        {
            contract.Status = ContractStatus.Concluido;
            contract.EndDate ??= DateTime.UtcNow;
        }

        contract.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await LoadDtoAsync(contract.Id));
    }

    /// <summary>
    /// Qualquer das partes pode cancelar um contrato que ainda não entrou em vigor, indicando o motivo.
    /// O interesse é encerrado, o anúncio volta ao mural e a outra parte é avisada na conversa.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ContractDto>> Cancel(
        int id,
        CancelContractRequest request,
        [FromServices] IHubContext<ChatHub> chatHub)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length < 5)
            return BadRequest(new { message = "Indica o motivo do cancelamento (pelo menos 5 caracteres)." });
        if (reason.Length > 500)
            return BadRequest(new { message = "O motivo não pode ter mais de 500 caracteres." });

        var contract = await _db.Contracts
            .Include(c => c.Match).ThenInclude(m => m.JobPosting)
            .Include(c => c.Match).ThenInclude(m => m.Conversation)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        if (!contract.CanBeCancelled())
        {
            return BadRequest(new
            {
                message = contract.Status switch
                {
                    ContractStatus.Cancelado => "Este contrato já foi cancelado.",
                    ContractStatus.Ativo => "Um contrato activo não pode ser cancelado. Quando o trabalho terminar, as duas partes confirmam o fim.",
                    _ => "Este contrato já não pode ser cancelado."
                }
            });
        }

        var now = DateTime.UtcNow;
        contract.Status = ContractStatus.Cancelado;
        contract.CancelledAt = now;
        contract.CancelledByUserId = CurrentUserId;
        contract.CancellationReason = reason;
        contract.UpdatedAt = now;

        contract.Match.Status = MatchStatus.Encerrado;

        // O anúncio tinha passado a Preenchido quando o interesse foi aceite; volta a receber interesses.
        if (contract.Match.JobPosting.Status == JobPostingStatus.Preenchido)
        {
            contract.Match.JobPosting.Status = JobPostingStatus.Ativo;
            contract.Match.JobPosting.UpdatedAt = now;
        }

        Message? notice = null;
        if (contract.Match.Conversation is not null)
        {
            notice = new Message
            {
                ConversationId = contract.Match.Conversation.Id,
                SenderUserId = CurrentUserId,
                Content = $"Cancelei o contrato KD-{contract.Id:D6}. Motivo: {reason}",
                SentAt = now
            };
            _db.Messages.Add(notice);
        }

        await _db.SaveChangesAsync();

        if (notice is not null)
        {
            notice.Sender = await _db.Users.FirstAsync(u => u.Id == CurrentUserId);
            await chatHub.Clients.Group(ChatHub.GroupName(notice.ConversationId))
                .SendAsync("ReceiveMessage", notice.ToDto());
        }

        return Ok(await LoadDtoAsync(contract.Id));
    }

    [HttpPost("{id:int}/ratings")]
    public async Task<ActionResult<ContractDto>> Rate(int id, CreateRatingRequest request)
    {
        var validation = await _ratingValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var contract = await _db.Contracts
            .Include(c => c.Ratings)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        if (!IsParty(contract)) return Forbid();
        if (contract.Status is not (ContractStatus.Ativo or ContractStatus.Concluido))
            return BadRequest(new { message = "Só podes avaliar depois de o contrato estar activo." });
        if (contract.Ratings.Any(r => r.RaterUserId == CurrentUserId))
            return BadRequest(new { message = "Já avalieste a outra parte neste contrato." });

        var rateeId = contract.FamiliaUserId == CurrentUserId
            ? contract.SecretariaUserId
            : contract.FamiliaUserId;

        contract.Ratings.Add(new Rating
        {
            RaterUserId = CurrentUserId,
            RateeUserId = rateeId,
            Score = request.Score,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        });

        contract.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await LoadDtoAsync(contract.Id));
    }

    private static string? SignedPath(Contract contract, string party) =>
        party.Equals("familia", StringComparison.OrdinalIgnoreCase) ? contract.SignedPdfPathFamilia
        : party.Equals("secretaria", StringComparison.OrdinalIgnoreCase) ? contract.SignedPdfPathSecretaria
        : null;

    private async Task<byte[]?> ReadStoredPdfAsync(string relativePath)
    {
        var absolute = _files.GetAbsolutePath(relativePath);
        if (!System.IO.File.Exists(absolute)) return null;
        return await System.IO.File.ReadAllBytesAsync(absolute);
    }

    private IQueryable<Match> MatchQuery() =>
        _db.Matches
            .AsSplitQuery()
            .Include(m => m.Contract)
            .Include(m => m.InitiatorUser).ThenInclude(u => u.Address).ThenInclude(a => a.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province)
            .Include(m => m.CounterpartUser).ThenInclude(u => u.Address).ThenInclude(a => a.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province)
            .Include(m => m.JobPosting).ThenInclude(j => j.Categories).ThenInclude(c => c.ServiceCategory)
            .Include(m => m.JobPosting).ThenInclude(j => j.Locations).ThenInclude(l => l.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province);

    private IQueryable<Contract> ContractListQuery() =>
        _db.Contracts
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Ratings)
            .Include(c => c.Match).ThenInclude(m => m.Conversation)
            .Include(c => c.Match).ThenInclude(m => m.JobPosting).ThenInclude(j => j.Categories).ThenInclude(cat => cat.ServiceCategory)
            .Include(c => c.FamiliaUser)
            .Include(c => c.SecretariaUser);

    private IQueryable<Contract> ContractDetailQuery(bool tracking)
    {
        IQueryable<Contract> query = _db.Contracts
            .AsSplitQuery()
            .Include(c => c.Clauses)
            .Include(c => c.Ratings).ThenInclude(r => r.RaterUser)
            .Include(c => c.Match).ThenInclude(m => m.Conversation)
            .Include(c => c.Match).ThenInclude(m => m.JobPosting).ThenInclude(j => j.Categories).ThenInclude(cat => cat.ServiceCategory)
            .Include(c => c.FamiliaUser).ThenInclude(u => u.Address).ThenInclude(a => a.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province)
            .Include(c => c.SecretariaUser).ThenInclude(u => u.Address).ThenInclude(a => a.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province);

        return tracking ? query : query.AsNoTracking();
    }

    private IQueryable<Contract> ContractPdfQuery() =>
        _db.Contracts
            .AsSplitQuery()
            .Include(c => c.Clauses)
            .Include(c => c.FamiliaUser).ThenInclude(u => u.Address).ThenInclude(a => a.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province)
            .Include(c => c.SecretariaUser).ThenInclude(u => u.Address).ThenInclude(a => a.Neighborhood)
                .ThenInclude(n => n.Municipality).ThenInclude(m => m.Province);

    private async Task<ContractDto> LoadDtoAsync(int id)
    {
        var contract = await ContractDetailQuery(tracking: false).FirstAsync(c => c.Id == id);
        return contract.ToDto(CurrentUserId);
    }

    private bool IsParty(Match match) =>
        match.InitiatorUserId == CurrentUserId || match.CounterpartUserId == CurrentUserId;

    private bool IsParty(Contract contract) =>
        contract.FamiliaUserId == CurrentUserId || contract.SecretariaUserId == CurrentUserId;

    private bool IsFamilia(Contract contract) => contract.FamiliaUserId == CurrentUserId;

    private const string OnlyFamiliaMessage =
        "Só a família pode gerar o contrato. A secretária do lar vê-o no separador Contratos e assina-o depois da família.";

    private static bool CanEdit(Contract contract) =>
        contract.SignedPdfPathFamilia is null
        && contract.SignedPdfPathSecretaria is null
        && contract.Status is ContractStatus.Rascunho or ContractStatus.PdfGerado;

    private static string FileName(int contractId) => $"contrato-kamba-diami-KD-{contractId:D6}.pdf";

    private static string SignedFileName(int contractId, string party)
    {
        var who = party.Equals("secretaria", StringComparison.OrdinalIgnoreCase) ? "secretaria" : "familia";
        return $"contrato-kamba-diami-KD-{contractId:D6}-assinado-{who}.pdf";
    }
}
