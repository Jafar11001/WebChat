using WebChat.DAL;
using WebChat.Models;

namespace WebChat.Services
{
    public class MessageService
    {
        private readonly AppDbContext _appDbContext;

        public MessageService(AppDbContext db)
        {
            _appDbContext = db;
        }

        public async Task<Message> SaveMessage(
            string conversationId,
            string senderId,
            string content,
            string? senderName = null,
            string? senderInitials = null,
            string? senderColor = null)
        {
            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                SenderName = senderName,
                SenderInitials = senderInitials,
                SenderColor = senderColor,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _appDbContext.Messages.Add(msg);
            await _appDbContext.SaveChangesAsync();

            return msg;
        }
    }
}
