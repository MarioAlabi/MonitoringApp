using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringApp.Monitoring.Server.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    [HttpGet("Login")]
    public IActionResult Login() => View();

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string usuario, string password)
    {
        if (usuario == "Admin" && password == "Catolica10")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, usuario),
                new(ClaimTypes.Role, "Administrator")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Error = "Credenciales incorrectas.";
        return View();
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}