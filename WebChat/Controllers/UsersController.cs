using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebChat.Entities;
using WebChat.Helpers;
using WebChat.ViewModels;

namespace WebChat.Controllers
{
    // Backs the "New Direct Message" picker: who can I start a chat with?
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var meId = _userManager.GetUserId(User);

            var users = await _userManager.Users
                .Where(u => u.Id != meId)
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.FullName, u.UserName })
                .ToListAsync();

            // Only display data leaves here — no emails, no hashes, no stamps.
            return Ok(users.Select(u =>
            {
                var name = string.IsNullOrWhiteSpace(u.FullName) ? u.UserName! : u.FullName;
                return new UserViewModel
                {
                    Id = u.Id,
                    Name = name,
                    Initials = AvatarHelper.Initials(name),
                    Color = AvatarHelper.Color(u.Id)
                };
            }));
        }
    }
}
