using Microsoft.AspNetCore.Identity;

namespace WebChat.Entities
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }

        public string Initials { get; set; }

        public string Color { get; set; }

        public DateTime? LastSeenAt { get; set; }
    }
}
