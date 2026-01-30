using Microsoft.Playwright;

namespace PlaywrightTestFramework.Utilities
{
    public static class ScreenshotHelper
    {
        private static readonly string ScreenshotFolder = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");

        public static async Task<string> CaptureScreenshotAsync(IPage page, string testName)
        {
            if (!Directory.Exists(ScreenshotFolder))
                Directory.CreateDirectory(ScreenshotFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{testName}_{timestamp}.png";
            var filePath = Path.Combine(ScreenshotFolder, fileName);

            await page.ScreenshotAsync(new() { Path = filePath, FullPage = true });

            return filePath;
        }
    }
}