using System.Security.Claims;
using KambaDiami.Application.Dtos;
using KambaDiami.Application.Mapping;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize(Roles = "Familia,Secretaria")]
public class MatchesController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;

    public MatchesController(KambaDiamiDbContext db)
    {
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Match> BaseQuery() =>
        _db.Matches
            .Include(m => m.JobPosting).ThenInclude(j => j.Categories).ThenInclude(c => c.ServiceCategory)
            .Include(m => m.InitiatorUser)
            .Include(m => m.CounterpartUser)
            .Include(m => m.Conversation);

    [HttpGet("mine")]
    public async Task<ActionResult<List<MatchDto>>> GetMine()
    {
        var matches = await BaseQuery()
            .Where(m => m.InitiatorUserId == CurrentUserId || m.CounterpartUserId == CurrentUserId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return Ok(matches.Select(m => m.ToDto(CurrentUserId)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<MatchDto>> Create(CreateMatchRequest request)
    {
        var posting = await _db.JobPostings.Include(j => j.Author).FirstOrDefaultAsync(j => j.Id == request.JobPostingId);
        if (posting is null) return NotFound(new { message = "Publicação não encontrada." });
        if (posting.Status != JobPostingStatus.Ativo) return BadRequest(new { message = "Esta publicação já não está activa." });
        if (posting.AuthorUserId == CurrentUserId) return BadRequest(new { message = "Não podes manifestar interesse na tua própria publicação." });

        // Um interesse liga sempre uma família a uma secretária do lar; sem isso não há contrato possível.
        if (User.IsInRole(posting.Author.Role.ToString()))
        {
            return BadRequest(new
            {
                message = posting.Type == JobPostingType.OfertaEmprego
                    ? "Esta oferta de emprego é para secretárias do lar."
                    : "Este anúncio é para famílias à procura de uma secretária do lar."
            });
        }

        var existing = await BaseQuery().FirstOrDefaultAsync(m =>
            m.JobPostingId == request.JobPostingId && m.InitiatorUserId == CurrentUserId);
        if (existing is not null) return Ok(existing.ToDto(CurrentUserId));

        var match = new Match
        {
            JobPostingId = posting.Id,
            InitiatorUserId = CurrentUserId,
            CounterpartUserId = posting.AuthorUserId,
            Status = MatchStatus.Pendente,
            Conversation = new Conversation()
        };

        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(m => m.Id == match.Id);
        return CreatedAtAction(nameof(GetMine), created.ToDto(CurrentUserId));
    }

    [HttpPut("{id:int}/respond")]
    public async Task<ActionResult<MatchDto>> Respond(int id, RespondMatchRequest request)
    {
        var match = await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);
        if (match is null) return NotFound();
        if (match.CounterpartUserId != CurrentUserId) return Forbid();
        if (match.Status != MatchStatus.Pendente) return BadRequest(new { message = "Este interesse já foi respondido." });

        // Aceitar outro interesse num anúncio já preenchido daria dois contratos para a mesma vaga.
        if (request.Accept && match.JobPosting.Status != JobPostingStatus.Ativo)
            return BadRequest(new { message = "Este anúncio já não está activo. Já aceitaste outro interesse nele ou foi retirado." });

        match.Status = request.Accept ? MatchStatus.Aceite : MatchStatus.Recusado;
        match.RespondedAt = DateTime.UtcNow;

        if (request.Accept)
        {
            match.JobPosting.Status = JobPostingStatus.Preenchido;
            match.JobPosting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(match.ToDto(CurrentUserId));
    }
}
