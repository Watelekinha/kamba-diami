using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Dtos;

public class ParticipantSummaryDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = default!;
    public string? PhotoPath { get; set; }
    public UserRole Role { get; set; }
}

public class MatchDto
{
    public int Id { get; set; }
    public int JobPostingId { get; set; }
    public string JobPostingSummary { get; set; } = default!;
    public ParticipantSummaryDto Counterpart { get; set; } = default!;
    public bool IsInitiatedByMe { get; set; }
    public MatchStatus Status { get; set; }
    public int? ConversationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class CreateMatchRequest
{
    public int JobPostingId { get; set; }
}

public class RespondMatchRequest
{
    public bool Accept { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class ConversationSummaryDto
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int JobPostingId { get; set; }
    public JobPostingType JobPostingType { get; set; }
    public string JobPostingSummary { get; set; } = default!;
    public MatchStatus MatchStatus { get; set; }
    public bool IsInitiatedByMe { get; set; }
    public ParticipantSummaryDto Counterpart { get; set; } = default!;
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
