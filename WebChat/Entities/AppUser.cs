using Microsoft.AspNetCore.Identity;

namespace WebChat.Entities
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
