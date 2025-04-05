using System.Security.Claims;
using FutureTech_Academy.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace FutureTech_Academy.Controllers;

[Authorize]
[Route("/[controller]/[action]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest loginRequest)
    {
        var claimsIdentity = await authService.Login(loginRequest.Email, loginRequest.Password);

        if (claimsIdentity is null) return Unauthorized("Failed to login");

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true,
            });

        return Ok("Login successfully");
    }


    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> SignUp(string email, string password)
    {
        return Ok(await authService.SignUp(email, password));
    }

    [HttpGet]
    public Task<IActionResult> UserInfo()
    {

        return Task.FromResult<IActionResult>(Ok(authService.UserInfo()));
    }

    [HttpGet]
    public new Task<IActionResult> SignOut()
    {
        authService.SignOut();
        HttpContext.SignOutAsync();
        return Task.FromResult<IActionResult>(Ok());
    }
}