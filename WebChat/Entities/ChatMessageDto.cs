namespace WebChat.Models
{
    // What the client is allowed to say about a message it's sending.
    // Sender identity is deliberately NOT in here — the hub takes that from
    // the authenticated connection, so a client can't post as someone else.
    public class ChatMessageDto
    {
        public string ConversationId { get; set; } = default!;
        public string Content { get; set; } = default!;

        // Echoed back untouched so the sender can match the broadcast to the
        // optimistic bubble it already drew, instead of rendering it twice.
        public string? ClientId { get; set; }
    }
}
