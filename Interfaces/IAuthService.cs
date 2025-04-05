using Firebase.Auth;
using System.Security.Claims;

namespace FutureTech_Academy.Interfaces
{
    public interface IAuthService
    {
        public Task<string?> SignUp(string email, string password);
        public Task<ClaimsIdentity?> Login(string email, string password);
        public Task<UserInfo> UserInfo();
        public void SignOut();
    }
}