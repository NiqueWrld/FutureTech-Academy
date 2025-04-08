using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using FutureTech_Academy.Interfaces;
using FutureTech_Academy.Models;
using System.Security.Claims;
using Firebase.Auth;

namespace FutureTech_Academy.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var claimsIdentity = await _authService.Login(model.Email, model.Password);
                if (claimsIdentity != null)
                {
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (FirebaseAuthException ex)
            {
                string errorMessage = ex.Reason switch
                {
                    AuthErrorReason.InvalidEmailAddress => "Invalid email address format.",
                    AuthErrorReason.WrongPassword => "Incorrect password. Please try again.",
                    AuthErrorReason.UserNotFound => "No account found with this email address.",
                    AuthErrorReason.TooManyAttemptsTryLater => "Too many login attempts. Please try again later.",
                    AuthErrorReason.UserDisabled => "This account has been disabled. Please contact support.",
                    _ => "An error occurred during login. Please try again."
                };
                ModelState.AddModelError(string.Empty, errorMessage);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }
            return View(model);
        }

        [HttpGet("SignUp")]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUp(SignUpModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var token = await _authService.SignUp(model.Email, model.Password);
                if (token != null)
                {
                    TempData["SuccessMessage"] = "Account created successfully! Please login with your credentials.";
                    return RedirectToAction("Login");
                }
            }
            catch (FirebaseAuthException ex)
            {
                string errorMessage = ex.Reason switch
                {
                    AuthErrorReason.EmailExists => "An account with this email already exists.",
                    AuthErrorReason.InvalidEmailAddress => "Invalid email address format.",
                    AuthErrorReason.WeakPassword => "Password is too weak. Please use a stronger password.",
                    AuthErrorReason.TooManyAttemptsTryLater => "Too many signup attempts. Please try again later.",
                    _ => "An error occurred during signup. Please try again."
                };
                ModelState.AddModelError(string.Empty, errorMessage);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }
            return View(model);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            _authService.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
} 