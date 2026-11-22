using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using PersonalHub.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Web.Controllers
{
    public class ContaController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly RoleManager<TipoUsuario> _roleManager;
        private readonly PersonalHubContext _context;
        public ContaController(PersonalHubContext context,
                               UserManager<Usuario> userManager,
                               SignInManager<Usuario> signInManager,
                               RoleManager<TipoUsuario> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager; 
            _roleManager = roleManager;
            _context = context; 
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Dashboard), "Home");
            }
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(ContaController.Login), "Conta", null);
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "Home");

            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Usuario? usuario = await _context.Users.SingleOrDefaultAsync(e => e.UserName == model.UserName);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Dados inválidos.");
                return View(model);
            }

            var resultadoLogin = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (resultadoLogin.Succeeded)
            {
                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Dashboard", "Home");

                return RedirectToLocal(returnUrl);
            }
               
            ModelState.AddModelError(string.Empty, "Dados inválidos.");
             
            return View(model);
        }
    }
}
