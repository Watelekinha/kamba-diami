namespace KambaDiami.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;

    public Guid SenderUserId { get; set; }
    public ApplicationUser Sender { get; set; } = default!;

    public string Content { get; set; } = default!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
