namespace WebChat.ViewModels
{
    // Shape returned by GET /api/users. Display data only — this is sent to
    // every signed-in client, so nothing sensitive belongs here.
    public class UserViewModel
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Initials { get; set; } = default!;
        public string Color { get; set; } = default!;
    }
}
