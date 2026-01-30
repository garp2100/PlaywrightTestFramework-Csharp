using FluentAssertions;
using Microsoft.Playwright;
using PlaywrightTestFramework.Core;
using PlaywrightTestFramework.PageObjects;
using PlaywrightTestFramework.Config;
using PlaywrightTestFramework.TestData;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightTestFramework.Tests.UITests
{
    [TestFixture]
    public class LoginTests : TestBase
    {
        private LoginPage? _loginPage;

        [SetUp]
        public async Task TestSetup()
        {
            await Task.CompletedTask; 
            _loginPage = new LoginPage(Page);
        }

        [Test]
        [Category("Smoke")]
        public async Task ValidLogin_ShouldNavigateToHomePage()
        {
            // Arrange
            var user = TestDataReader.GetValidUser("admin");
            await _loginPage!.NavigateToLoginAsync(ConfigReader.BaseUrl);

            // Act
            await _loginPage.LoginAsync(user.Username, user.Password);

            // Assert
            await Expect(Page).ToHaveURLAsync(ConfigReader.BaseUrl); 
        }

        [Test]
        [Category("Regression")]
        public async Task InvalidLogin_ShouldDisplayErrorMessage()
        {
            // Arrange
            var user = TestDataReader.GetInvalidUser("wrongPassword");
            await _loginPage!.NavigateToLoginAsync(ConfigReader.BaseUrl);

            // Act
            await _loginPage.LoginAsync(user.Username, user.Password);

            // Assert
            var isErrorDisplayed = await _loginPage.IsErrorDisplayedAsync();
            isErrorDisplayed.Should().BeTrue();

            var errorMessage = await _loginPage.GetErrorMessageAsync();
            errorMessage.Should().Contain("Invalid login attempt");
        }
    }
}