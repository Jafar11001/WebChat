namespace WebChat.ViewModels
{
    // Shape returned by GET /api/conversations.
    // This is what main.js renders the sidebar from — no HTML/JS-hardcoded
    // conversation data anymore, the database is the single source of truth.
    public class ConversationViewModel
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string AvatarInitials { get; set; } = default!;
        public string AvatarColor { get; set; } = default!;
        public bool IsGroup { get; set; }
        public bool IsOnline { get; set; }
        public string? LastMessage { get; set; }
        public string? LastMessageTime { get; set; }
        public string? OtherUserId { get; set; }
        public int UnreadCount { get; set; }
        public string? OtherLastReadTime { get; set; }
        public string? OtherLastSeenTime { get; set; }
    }
}
