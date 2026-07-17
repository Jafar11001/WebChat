namespace WebChat.Models
{
    public class Conversation
    {
        public string Id { get; set; } = default!;

        // Groups only. A direct message has no fixed title or avatar: it is
        // whoever the OTHER participant is, which differs per viewer, so it's
        // derived at read time instead of stored.
        public string? Title { get; set; }

        public string? AvatarInitials { get; set; }

        public string? AvatarColor { get; set; }

        public bool IsGroup { get; set; }

        // Demo-only presence flag. There is no real presence tracking yet.
        public bool IsOnline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ConversationParticipant> Participants { get; set; } = new();

        public List<Message> Messages { get; set; } = new();
    }
}
