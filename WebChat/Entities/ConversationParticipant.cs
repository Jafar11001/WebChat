using WebChat.Entities;

namespace WebChat.Models
{
    // Who is in a conversation. Until this existed a Conversation was just a
    // title with no link to AspNetUsers, so there was no way to create one and
    // no way to scope the list per user — everyone saw everything.
    public class ConversationParticipant
    {
        public int Id { get; set; }

        public string ConversationId { get; set; } = default!;

        public Conversation? Conversation { get; set; }

        public string UserId { get; set; } = default!;

        public AppUser? User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // When this user last read this conversation. Null = never opened.
        // Drives unread counts now, and read receipts in the next step.
        public DateTime? LastReadAt { get; set; }


    }
}
