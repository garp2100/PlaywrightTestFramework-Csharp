using Microsoft.Playwright;

namespace PlaywrightTestFramework.PageObjects
{
    public class LoginPage : BasePage
    {
        // Locators
        private readonly ILocator _usernameInput;
        private readonly ILocator _passwordInput;
        private readonly ILocator _loginButton;
        private readonly ILocator _errorMessage;

        // Constructor
        public LoginPage(IPage page) : base(page)
        {
            _usernameInput = page.Locator("[data-automation-id='email-input']");
            _passwordInput = page.Locator("[data-automation-id='password-input']");
            _loginButton = page.Locator("[data-automation-id='login-submit-btn']");
            _errorMessage = page.Locator("[data-automation-id='login-validation-summary']");
        }

        // Page Actions
        public async Task NavigateToLoginAsync(string baseUrl)
        {
            await NavigateToAsync($"{baseUrl}/Account/Login");
        }

        public async Task LoginAsync(string username, string password)
        {
            await _usernameInput.FillAsync(username);
            await _passwordInput.FillAsync(password);
            await _loginButton.ClickAsync();
        }

        public async Task<bool> IsErrorDisplayedAsync()
        {
            return await _errorMessage.IsVisibleAsync();
        }

        public async Task<string> GetErrorMessageAsync()
        {
            return await _errorMessage.TextContentAsync() ?? string.Empty;
        }
    }
}