namespace WebChat.ViewModels
{
    // Shape returned by GET /api/conversations/{id}/messages.
    public class MessageViewModel
    {
        public int Id { get; set; }
        public string ConversationId { get; set; } = default!;
        public string SenderId { get; set; } = default!;
        public string? SenderName { get; set; }
        public string? SenderInitials { get; set; }
        public string? SenderColor { get; set; }
        public string Content { get; set; } = default!;
        public string Time { get; set; } = default!;
    }
}
