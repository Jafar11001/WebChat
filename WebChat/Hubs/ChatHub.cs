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

        public ChatHub(
            MessageService messageService,
            ConversationService conversationService,
            UserManager<AppUser> userManager)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _userManager = userManager;
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
                    name = entity.SenderName,
                    initials = entity.SenderInitials,
                    color = entity.SenderColor
                }
            });
        }
    }
}
