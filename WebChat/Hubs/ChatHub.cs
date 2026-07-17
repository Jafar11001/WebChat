using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using WebChat.Entities;
using WebChat.Helpers;
using WebChat.Models;
using WebChat.Services;

namespace WebChat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MessageService _messageService;
        private readonly ConversationService _conversationService;
        private readonly UserManager<AppUser> _userManager;
        private readonly PresenceTracker _presenceTracker;

        public ChatHub(
            MessageService messageService,
            ConversationService conversationService,
            UserManager<AppUser> userManager,
            PresenceTracker presenceTracker)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _userManager = userManager;
            _presenceTracker = presenceTracker;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _userManager.GetUserId(Context.User!);
            if (userId is not null && _presenceTracker.Connect(userId))
                await Clients.All.SendAsync("PresenceChanged", userId, true);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _userManager.GetUserId(Context.User!);
            if (userId is not null && _presenceTracker.Disconnect(userId))
                await Clients.All.SendAsync("PresenceChanged", userId, false);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(ChatMessageDto msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Content)) return;

            // Identity comes from the authenticated connection, never from the
            // payload — otherwise any client could post as any user.
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user is null) throw new HubException("Not signed in.");

            if (!await _conversationService.IsParticipantAsync(msg.ConversationId, user.Id))
                throw new HubException("You are not in this conversation.");

            var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName! : user.FullName;

            var entity = await _messageService.SaveMessage(
                msg.ConversationId,
                user.Id,
                msg.Content.Trim(),
                displayName,
                AvatarHelper.Initials(displayName),
                AvatarHelper.Color(user.Id));

            // Addressed to the participants themselves, not a group: a recipient
            // who hasn't opened this conversation is in no group, and would
            // otherwise never learn the message exists.
            var recipients = await _conversationService.GetParticipantIdsAsync(msg.ConversationId);

            await Clients.Users(recipients).SendAsync("ReceiveMessage", new
            {
                id = entity.Id,
                clientId = msg.ClientId,
                conversationId = entity.ConversationId,
                senderId = entity.SenderId,
                content = entity.Content,
                time = entity.CreatedAt.ToString("HH:mm"),
                sender = new
                {
                    name = displayName,
                    initials = user.Initials,
                    color = user.Color
                }
            });
        }
        public async Task MarkRead(string conversationId)
        {
            var userId = _userManager.GetUserId(Context.User!);
            if (userId is null) return;
            if (!await _conversationService.IsParticipantAsync(conversationId, userId)) return;

            var readAt = await _conversationService.MarkReadAsync(conversationId, userId);

            // Tell the OTHER participants (the senders) so their ✓ can become ✓✓.
            var others = (await _conversationService.GetParticipantIdsAsync(conversationId))
                .Where(id => id != userId);

            await Clients.Users(others).SendAsync("ConversationRead", conversationId, userId, readAt);
        }
    }
}
