using System.Security.Claims;
using KambaDiami.Application.Mapping;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Hubs;

[Authorize(Roles = "Familia,Secretaria")]
public class ChatHub : Hub
{
    private readonly KambaDiamiDbContext _db;

    public ChatHub(KambaDiamiDbContext db)
    {
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    internal static string GroupName(int conversationId) => $"conversation-{conversationId}";

    public async Task JoinConversation(int conversationId)
    {
        var conversation = await _db.Conversations.Include(c => c.Match).FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conversation is null) throw new HubException("Conversa não encontrada.");
        if (conversation.Match.InitiatorUserId != CurrentUserId && conversation.Match.CounterpartUserId != CurrentUserId)
            throw new HubException("Não tens acesso a esta conversa.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task SendMessage(int conversationId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var conversation = await _db.Conversations.Include(c => c.Match).FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conversation is null) throw new HubException("Conversa não encontrada.");
        if (conversation.Match.InitiatorUserId != CurrentUserId && conversation.Match.CounterpartUserId != CurrentUserId)
            throw new HubException("Não tens acesso a esta conversa.");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = CurrentUserId,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        message.Sender = await _db.Users.FirstAsync(u => u.Id == CurrentUserId);

        await Clients.Group(GroupName(conversationId)).SendAsync("ReceiveMessage", message.ToDto());
    }

    public async Task MarkAsRead(int conversationId)
    {
        var unread = await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderUserId != CurrentUserId && m.ReadAt == null)
            .ToListAsync();

        if (unread.Count == 0) return;

        foreach (var message in unread) message.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await Clients.Group(GroupName(conversationId)).SendAsync("MessagesRead", conversationId, CurrentUserId);
    }
}
