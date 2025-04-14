using FutureTech_Academy.Controllers.Auth;
using FutureTech_Academy.Interfaces;
using FutureTech_Academy.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

public class AccountControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AccountController(_authServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public void Login_Get_ReturnsViewResult()
    {
        // Act
        var result = _controller.Login();

        // Assert
        Xunit.Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Login_Post_ValidCredentials_RedirectsToHome()
    {
        // Arrange
        var model = new LoginModel { Email = "test@example.com", Password = "password123" };
        var claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, model.Email) }, CookieAuthenticationDefaults.AuthenticationScheme);

        _authServiceMock
            .Setup(x => x.Login(model.Email, model.Password))
            .ReturnsAsync(claimsIdentity);

        var mockAuthenticationService = new Mock<IAuthenticationService>();
        _controller.HttpContext.RequestServices = Mock.Of<IServiceProvider>(sp =>
            sp.GetService(typeof(IAuthenticationService)) == mockAuthenticationService.Object);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var redirectResult = Xunit.Assert.IsType<RedirectToActionResult>(result);
        Xunit.Assert.Equal("Index", redirectResult.ActionName);
        Xunit.Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Login_Post_InvalidModelState_ReturnsViewWithModel()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var model = new LoginModel();

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Xunit.Assert.IsType<ViewResult>(result);
        Xunit.Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task Login_Post_InvalidCredentials_ReturnsViewWithError()
    {
        // Arrange
        var model = new LoginModel { Email = "test@example.com", Password = "wrongpassword" };
        _authServiceMock
            .Setup(x => x.Login(model.Email, model.Password))
            .ReturnsAsync((ClaimsIdentity)null);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Xunit.Assert.IsType<ViewResult>(result);
        Xunit.Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public void SignUp_Get_ReturnsViewResult()
    {
        // Act
        var result = _controller.SignUp();

        // Assert
        Xunit.Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task SignUp_Post_ValidModel_RedirectsToLogin()
    {
        // Arrange
        var model = new SignUpModel { Email = "test@example.com", Password = "password123" };
        _authServiceMock
            .Setup(x => x.SignUp(model.Email, model.Password))
            .ReturnsAsync("mockToken");

        // Act
        var result = await _controller.SignUp(model);

        // Assert
        var redirectResult = Xunit.Assert.IsType<RedirectToActionResult>(result);
        Xunit.Assert.Equal("Login", redirectResult.ActionName);
    }

    [Fact]
    public async Task SignUp_Post_InvalidModelState_ReturnsViewWithModel()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var model = new SignUpModel();

        // Act
        var result = await _controller.SignUp(model);

        // Assert
        var viewResult = Xunit.Assert.IsType<ViewResult>(result);
        Xunit.Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task Logout_Post_RedirectsToLogin()
    {
        // Act
        var result = await _controller.Logout();

        // Assert
        var redirectResult = Xunit.Assert.IsType<RedirectToActionResult>(result);
        Xunit.Assert.Equal("Login", redirectResult.ActionName);
    }
}