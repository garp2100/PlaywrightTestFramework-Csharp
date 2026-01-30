using Microsoft.Playwright;

namespace PlaywrightTestFramework.PageObjects
{
    public class BasePage
    {
        protected readonly IPage _page;

        public BasePage(IPage page)
        {
            _page = page;
        }

        // Navigation
        public async Task NavigateToAsync(string url)
        {
            await _page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        // Waits
        public async Task WaitForElementAsync(string selector, int timeout = 30000)
        {
            await _page.WaitForSelectorAsync(selector, new() { Timeout = timeout });
        }

        public async Task WaitForUrlAsync(string urlPattern)
        {
            await _page.WaitForURLAsync(urlPattern);
        }

        // Actions
        public async Task ClickAsync(string selector)
        {
            await _page.ClickAsync(selector);
        }

        public async Task FillAsync(string selector, string text)
        {
            await _page.FillAsync(selector, text);
        }

        public async Task SelectOptionAsync(string selector, string value)
        {
            await _page.SelectOptionAsync(selector, value);
        }

        // Assertions
        public async Task<bool> IsElementVisibleAsync(string selector)
        {
            return await _page.IsVisibleAsync(selector);
        }

        public async Task<string> GetTextAsync(string selector)
        {
            return await _page.TextContentAsync(selector) ?? string.Empty;
        }

        public async Task<string> GetAttributeAsync(string selector, string attribute)
        {
            return await _page.GetAttributeAsync(selector, attribute) ?? string.Empty;
        }

        // Screenshot
        public async Task<byte[]> TakeScreenshotAsync()
        {
            return await _page.ScreenshotAsync();
        }
    }
}