namespace WebChat.Models
{
    public class ChatMessageDto
    {
        public string ConversationId { get; set; }
        public string Content { get; set; }

        public string SenderId { get; set; }
    }
}
