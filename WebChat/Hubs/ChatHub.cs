using Microsoft.AspNetCore.SignalR;
using WebChat.DAL;
using WebChat.Models;
using WebChat.Models;
using WebChat.Services;

namespace WebChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _appDbContext;
        private readonly MessageService _messageService;

        public ChatHub(MessageService messageService, AppDbContext appDbContext = null)
        {
            _messageService = messageService;
            _appDbContext = appDbContext;
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(ChatMessageDto msg)
        {
            // 1. save to DB
            var entity = new Message
            {
                ConversationId = msg.ConversationId,
                SenderId = msg.SenderId,
                Content = msg.Content,
                CreatedAt = DateTime.UtcNow
            };

            _appDbContext.Messages.Add(entity);
            await _appDbContext.SaveChangesAsync();

            // 2. broadcast to group
            await Clients.Group(msg.ConversationId).SendAsync("ReceiveMessage", new
            {
                id = entity.Id,
                conversationId = entity.ConversationId,
                senderId = entity.SenderId,
                content = entity.Content,
                time = entity.CreatedAt.ToString("HH:mm")
            });
        }
    }
}