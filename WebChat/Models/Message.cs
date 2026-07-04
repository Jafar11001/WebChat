namespace WebChat.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string ConversationId { get; set; }

        public string SenderId { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
