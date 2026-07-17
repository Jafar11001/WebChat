using System.ComponentModel.DataAnnotations.Schema;

namespace WebChat.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string ConversationId { get; set; } = default!;

        public Conversation? Conversation { get; set; }

        [ForeignKey]
        public string SenderId { get; set; } = default!;

        // Denormalized sender display info. There is no Users table yet
        // (auth/Identity is a later step), so each message carries enough
        // to render an avatar/name without needing a join.
        public string? SenderName { get; set; }

        public string? SenderInitials { get; set; }

        public string? SenderColor { get; set; }

        public string Content { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
    }
}
