using System.Security.Claims;
using KambaDiami.Application.Dtos;
using KambaDiami.Application.Mapping;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize(Roles = "Familia,Secretaria")]
public class ConversationsController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;

    public ConversationsController(KambaDiamiDbContext db)
    {
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("mine")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetMine()
    {
        var userId = CurrentUserId;
        var rows = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.Match.InitiatorUserId == userId || c.Match.CounterpartUserId == userId)
            .Select(c => new
            {
                c.Id,
                c.MatchId,
                MatchStatus = c.Match.Status,
                c.Match.JobPostingId,
                JobPostingType = c.Match.JobPosting.Type,
                IsInitiatedByMe = c.Match.InitiatorUserId == userId,
                InitiatorId = c.Match.InitiatorUser.Id,
                InitiatorFirstName = c.Match.InitiatorUser.FirstName,
                InitiatorLastName = c.Match.InitiatorUser.LastName,
                InitiatorPhoto = c.Match.InitiatorUser.PhotoPath,
                InitiatorRole = c.Match.InitiatorUser.Role,
                CounterpartId = c.Match.CounterpartUser.Id,
                CounterpartFirstName = c.Match.CounterpartUser.FirstName,
                CounterpartLastName = c.Match.CounterpartUser.LastName,
                CounterpartPhoto = c.Match.CounterpartUser.PhotoPath,
                CounterpartRole = c.Match.CounterpartUser.Role,
                Category = c.Match.JobPosting.Categories.Select(x => x.ServiceCategory.Name).FirstOrDefault(),
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault(),
                LastMessageAt = c.Messages.Max(m => (DateTime?)m.SentAt),
                UnreadCount = c.Messages.Count(m => m.SenderUserId != userId && m.ReadAt == null)
            })
            .ToListAsync();

        var result = rows
            .Select(row =>
            {
                var counterpart = row.IsInitiatedByMe
                    ? new ParticipantSummaryDto
                    {
                        UserId = row.CounterpartId,
                        Name = $"{row.CounterpartFirstName} {row.CounterpartLastName}",
                        PhotoPath = row.CounterpartPhoto,
                        Role = row.CounterpartRole
                    }
                    : new ParticipantSummaryDto
                    {
                        UserId = row.InitiatorId,
                        Name = $"{row.InitiatorFirstName} {row.InitiatorLastName}",
                        PhotoPath = row.InitiatorPhoto,
                        Role = row.InitiatorRole
                    };

                return new ConversationSummaryDto
                {
                    Id = row.Id,
                    MatchId = row.MatchId,
                    JobPostingId = row.JobPostingId,
                    JobPostingType = row.JobPostingType,
                    JobPostingSummary = row.Category ?? "Serviço doméstico",
                    MatchStatus = row.MatchStatus,
                    IsInitiatedByMe = row.IsInitiatedByMe,
                    Counterpart = counterpart,
                    LastMessage = row.LastMessage,
                    LastMessageAt = row.LastMessageAt,
                    UnreadCount = row.UnreadCount
                };
            })
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(int id)
    {
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.Conversation != null && m.Conversation.Id == id);
        if (match is null) return NotFound();
        if (match.InitiatorUserId != CurrentUserId && match.CounterpartUserId != CurrentUserId)
            return Forbid();

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages.Select(m => m.ToDto()).ToList());
    }
}
