using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace RabbitMQ.ExportToExcel.ExcelCreateWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Login()
        {


            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return View();
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user: user,
                password: password,
                isPersistent: true,
                lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                return View();
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
