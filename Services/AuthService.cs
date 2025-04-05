using System.Security.Claims;
using Firebase.Auth;
using FutureTech_Academy.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FutureTech_Academy.Services;

public class AuthService(FirebaseAuthClient firebaseAuth) : IAuthService
{
    public async Task<string?> SignUp(string email, string password)
    {
        var userCredentials = await firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
        return userCredentials is null ? null : await userCredentials.User.GetIdTokenAsync();
    }
    public async Task<ClaimsIdentity?> Login(string email, string password)
    {
        var userCredentials = await firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);

        var claims = new List<Claim>
        {
            new Claim("uid", userCredentials.User.Uid),
        };

        return new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "uid", null);
    }
    public Task<UserInfo> UserInfo()
    {
        return Task.FromResult(firebaseAuth.User.Info);
    }

    public void SignOut() => firebaseAuth.SignOut();
}