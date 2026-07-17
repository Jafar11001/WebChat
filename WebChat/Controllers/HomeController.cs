using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebChat.Entities;
using WebChat.Helpers;
using WebChat.ViewModels;

namespace WebChat.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public HomeController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(User);

            // Cookie is valid but the row is gone (deleted user, wiped db).
            // Force a fresh sign-in rather than rendering a chat with no identity.
            if (user is null) return RedirectToAction("Login", "Account");

            var displayName = string.IsNullOrWhiteSpace(user.FullName)
                ? user.UserName!
                : user.FullName;

            return View(new CurrentUserViewModel
            {
                Id = user.Id,
                Name = displayName,
                Email = user.Email ?? "",
                Initials = AvatarHelper.Initials(displayName),
                Color = AvatarHelper.Color(user.Id)
            });
        }
    }
}
