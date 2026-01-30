using FluentAssertions;
using NUnit.Framework;
using PlaywrightTestFramework.Core;
using PlaywrightTestFramework.PageObjects;
using PlaywrightTestFramework.Config;

namespace PlaywrightTestFramework.Tests.UITests
{
    [TestFixture]
    public class LoginTests : TestBase
    {
        private LoginPage? _loginPage;

        [SetUp]
        public new async Task Setup()
        {
            await base.Setup();
            _loginPage = new LoginPage(Page);
        }

        [Test]
        [Category("Smoke")]
        public async Task ValidLogin_ShouldNavigateToHomePage()
        {
            // Arrange
            await _loginPage!.NavigateToLoginAsync(ConfigReader.BaseUrl);

            // Act
            await _loginPage.LoginAsync("admin@test.com", "Password123!");

            // Assert
            await Page.WaitForURLAsync("**/Home/Index");
            Page.Url.Should().Contain("/Home/Index");
        }

        [Test]
        [Category("Regression")]
        public async Task InvalidLogin_ShouldDisplayErrorMessage()
        {
            // Arrange
            await _loginPage!.NavigateToLoginAsync(ConfigReader.BaseUrl);

            // Act
            await _loginPage.LoginAsync("invalid@test.com", "wrongpass");

            // Assert
            var isErrorDisplayed = await _loginPage.IsErrorDisplayedAsync();
            isErrorDisplayed.Should().BeTrue();
            
            var errorMessage = await _loginPage.GetErrorMessageAsync();
            errorMessage.Should().Contain("Invalid login attempt");
        }
    }
}