namespace WebChat.ViewModels
{
    // Serialised into Index.cshtml as window.CURRENT_USER. This replaces the
    // hardcoded CONFIG.CURRENT_USER that main.js used before Identity existed.
    public class CurrentUserViewModel
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Initials { get; set; } = default!;
        public string Color { get; set; } = default!;
    }
}
