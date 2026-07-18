using Microsoft.EntityFrameworkCore;
using WebChat.DAL;
using WebChat.Helpers;
using WebChat.Models;
using WebChat.ViewModels;

namespace WebChat.Services
{
    public class ConversationService
    {
        private readonly AppDbContext _db;

        private readonly PresenceTracker _presenceTracker;
        public ConversationService(AppDbContext db, PresenceTracker presenceTracker)
        {
            _db = db;
            _presenceTracker = presenceTracker;
        }

        // Powers GET /api/conversations — scoped to conversations the caller is
        // actually in. Sorted by most recent activity, with the preview computed
        // from the Messages table so nothing goes stale.
        public async Task<List<ConversationViewModel>> GetConversationsAsync(string userId)
        {
            var raw = await _db.Conversations
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.AvatarInitials,
                    c.AvatarColor,
                    c.IsGroup,
                    c.IsOnline,
                    Others = c.Participants
                        .Where(p => p.UserId != userId)
                        .Select(p => new { p.UserId, p.User!.FullName, p.User.UserName,p.LastReadAt,p.User.LastSeenAt})
                        .ToList(),
                    Last = c.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new { m.Content, m.CreatedAt })
                        .FirstOrDefault(),
                    Unread = c.Messages.Count(m => m.SenderId != userId
                    && m.CreatedAt > (c.Participants
                    .Where(p => p.UserId == userId)
                    .Select(p => (DateTime?)p.LastReadAt)
                    .FirstOrDefault() ?? DateTime.MinValue))


                })
                .ToListAsync();

            return raw
                .OrderByDescending(c => c.Last?.CreatedAt ?? DateTime.MinValue)
                .Select(c =>
                {
                    var vm = new ConversationViewModel
                    {
                        Id = c.Id,
                        IsGroup = c.IsGroup,
                        LastMessage = c.Last?.Content,
                        LastMessageTime = c.Last?.CreatedAt.ToString("HH:mm"),
                        UnreadCount = c.Unread
                    };

                    var other = c.Others.FirstOrDefault();

                    if (!c.IsGroup && other is not null)
                    {
                        // A DM is titled after the person you're talking to.
                        var name = string.IsNullOrWhiteSpace(other.FullName) ? other.UserName! : other.FullName;
                        vm.Title = name;
                        vm.AvatarInitials = AvatarHelper.Initials(name);
                        vm.AvatarColor = AvatarHelper.Color(other.UserId);
                        vm.OtherUserId = other.UserId;
                        vm.OtherLastReadTime = other.LastReadAt?.ToString("HH:mm");
                        vm.IsOnline = _presenceTracker.IsOnline(other.UserId);
                        vm.OtherLastSeenTime = other.LastSeenAt?.ToString("HH:mm");
                    }
                    else
                    {
                        vm.Title = c.Title ?? "Conversation";
                        vm.AvatarInitials = c.AvatarInitials ?? AvatarHelper.Initials(c.Title);
                        vm.AvatarColor = c.AvatarColor ?? AvatarHelper.Color(c.Id);
                        vm.IsOnline= c.Others.Any(o => _presenceTracker.IsOnline(o.UserId));
                    }

                    return vm;
                })
                .ToList();
        }

        // Powers GET /api/conversations/{id}/messages.
        public async Task<List<MessageViewModel>> GetMessagesAsync(string conversationId, string userId)
        {
            // When have the OTHER participants read up to? A message of mine is
            // "read" once everyone else's LastReadAt is at or past its timestamp.
            var others = await _db.ConversationParticipants
                .Where(p => p.ConversationId == conversationId && p.UserId != userId)
                .Select(p => p.LastReadAt)
                .ToListAsync();

            var raw = await _db.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.ConversationId,
                    m.SenderId,
                    m.Sender!.FullName,
                    m.Sender.Initials,
                    m.Sender.Color,
                    m.Content,
                    m.CreatedAt
                })
                .ToListAsync();

            return raw.Select(m => new MessageViewModel
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.FullName,
                SenderInitials = m.Initials,
                SenderColor = m.Color,
                Content = m.Content,
                Time = m.CreatedAt.ToString("HH:mm"),
                Read = m.SenderId == userId
                       && others.Count > 0
                       && others.All(lr => lr.HasValue && lr.Value >= m.CreatedAt)
            }).ToList();
        }

        public async Task<bool> ConversationExistsAsync(string conversationId)
        {
            return await _db.Conversations.AnyAsync(c => c.Id == conversationId);
        }

        // The authorisation check for every read and every send. Being signed in
        // is not enough — you must be in the conversation.
        public async Task<bool> IsParticipantAsync(string conversationId, string userId)
        {
            return await _db.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
        }

        // Who a message should be delivered to. The hub addresses participants
        // directly rather than a SignalR group, so a recipient who doesn't have
        // the conversation open still gets it — otherwise the first message of a
        // new DM would never reach them.
        public async Task<List<string>> GetParticipantIdsAsync(string conversationId)
        {
            return await _db.ConversationParticipants
                .Where(p => p.ConversationId == conversationId)
                .Select(p => p.UserId)
                .ToListAsync();
        }

        // Opening a DM twice must land on the same conversation, not create a
        // second one, so this looks for the existing pair first.
        public async Task<string> GetOrCreateDirectAsync(string userId, string otherUserId)
        {
            if (userId == otherUserId)
                throw new InvalidOperationException("Cannot open a direct message with yourself.");

            var existing = await _db.Conversations
                .Where(c => !c.IsGroup
                            && c.Participants.Count == 2
                            && c.Participants.Any(p => p.UserId == userId)
                            && c.Participants.Any(p => p.UserId == otherUserId))
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (existing is not null) return existing;

            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString("n"),
                IsGroup = false,
                CreatedAt = DateTime.UtcNow,
                Participants =
                {
                    new ConversationParticipant { UserId = userId },
                    new ConversationParticipant { UserId = otherUserId }
                }
            };

            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            return conversation.Id;
        }

        public async Task<ConversationViewModel?> GetConversationForUserAsync(string conversationId, string userId)
        {
            var all = await GetConversationsAsync(userId);
            return all.FirstOrDefault(c => c.Id == conversationId);
        }

        public async Task<DateTime> MarkReadAsync(string conversationId, string userId)
        {
            var now = DateTime.UtcNow;

            var participant = await _db.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);

            if (participant is null) return now;

            participant.LastReadAt = now;
            await _db.SaveChangesAsync();
            return now;
        }
    }
}
