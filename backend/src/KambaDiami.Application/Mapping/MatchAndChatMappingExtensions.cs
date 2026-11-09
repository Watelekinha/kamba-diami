using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;

namespace KambaDiami.Application.Mapping;

public static class MatchAndChatMappingExtensions
{
    public static ParticipantSummaryDto ToParticipantSummary(this ApplicationUser user) =>
        new()
        {
            UserId = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            PhotoPath = user.PhotoPath,
            Role = user.Role
        };

    public static MatchDto ToDto(this Match match, Guid currentUserId)
    {
        var isInitiator = match.InitiatorUserId == currentUserId;
        var counterpart = isInitiator ? match.CounterpartUser : match.InitiatorUser;

        var categoryName = match.JobPosting.Categories.Count > 0
            ? match.JobPosting.Categories.First().ServiceCategory.Name
            : "Serviço doméstico";

        return new MatchDto
        {
            Id = match.Id,
            JobPostingId = match.JobPostingId,
            JobPostingSummary = categoryName,
            Counterpart = counterpart.ToParticipantSummary(),
            IsInitiatedByMe = isInitiator,
            Status = match.Status,
            ConversationId = match.Conversation?.Id,
            CreatedAt = match.CreatedAt,
            RespondedAt = match.RespondedAt
        };
    }

    public static MessageDto ToDto(this Message message) =>
        new()
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderUserId = message.SenderUserId,
            SenderName = $"{message.Sender.FirstName} {message.Sender.LastName}",
            Content = message.Content,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt
        };
}
