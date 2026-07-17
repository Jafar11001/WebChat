using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebChat.Helpers;
using WebChat.Entities;
using WebChat.ViewModels.Account;
using WebApplication4.Helpers;

namespace WebChat.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;


        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }


        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registerVM)
        {
            if (!ModelState.IsValid) return View();

            AppUser newUser = new()
            {
                FullName = registerVM.FullName,
                UserName = registerVM.UserName,
                Email = registerVM.Email
               
            };
            newUser.Initials = AvatarHelper.Initials(registerVM.FullName);
            newUser.Color = AvatarHelper.Color(newUser.Id);

            IdentityResult identityResult = await _userManager.CreateAsync(newUser, registerVM.Password);

            if (!identityResult.Succeeded)
            {
                foreach (IdentityError error in identityResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View();
            }

            // If the role assignment fails the account must not survive, or the
            // user is left with a half-registered login they were never signed
            // in to and can't re-register over.
            IdentityResult roleResult = await _userManager.AddToRoleAsync(newUser,RoleEnum.Member.ToString());

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                ModelState.AddModelError("", "Could not complete registration. Please try again.");
                return View();
            }

            await _signInManager.SignInAsync(newUser, true);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (ModelState.IsValid == false) return View();

            AppUser user = await _userManager.FindByNameAsync(loginVM.UserNameOrEmail);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(loginVM.UserNameOrEmail);
                if (user == null)
                {
                    ModelState.AddModelError("", "Username or password is incorrect");
                    return View();
                }
            }
            Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);

            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError("", "Username or password is incorrect");
                return View();
            }

            if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError("", "Your account is locked out. Please try again later.");
                return View(loginVM);
            }

            

            return RedirectToAction("Index", "Home");
        }

        // POST-only: a GET logout can be fired by any <img src="/Account/Logout">
        // on any page, signing the user out without their intent (CSRF).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // Configured as the cookie's AccessDeniedPath. Browser navigations to a
        // forbidden page land here; /api requests get a 403 status instead (see
        // ConfigureApplicationCookie in Program.cs).
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
