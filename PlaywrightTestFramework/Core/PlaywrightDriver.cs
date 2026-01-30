using Microsoft.Playwright;

namespace PlaywrightTestFramework.Core
{
    public class PlaywrightDriver
    {
        private static readonly AsyncLocal<IPlaywright?> _playwright = new();
        private static readonly AsyncLocal<IBrowser?> _browser = new();
        private static readonly AsyncLocal<IBrowserContext?> _context = new();
        private static readonly AsyncLocal<IPage?> _page = new();

        public static IPlaywright? Playwright => _playwright.Value;
        public static IBrowser? Browser => _browser.Value;
        public static IBrowserContext? Context => _context.Value;
        public static IPage? Page => _page.Value;

        public static async Task InitializeAsync(string browserType = "chromium", bool headless = false)
        {
            _playwright.Value = await Microsoft.Playwright.Playwright.CreateAsync();

            _browser.Value = browserType.ToLower() switch
            {
                "chromium" => await _playwright.Value.Chromium.LaunchAsync(new() { Headless = headless }),
                "firefox" => await _playwright.Value.Firefox.LaunchAsync(new() { Headless = headless }),
                "webkit" => await _playwright.Value.Webkit.LaunchAsync(new() { Headless = headless }),
                _ => await _playwright.Value.Chromium.LaunchAsync(new() { Headless = headless })
            };

            _context.Value = await _browser.Value.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true,
                RecordVideoDir = "Videos/",
                // RecordTraceDir = "Traces/" // if needed
            });

            // Start tracing for debugging
            await _context.Value.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            _page.Value = await _context.Value.NewPageAsync();
            _page.Value.SetDefaultTimeout(Config.ConfigReader.Timeout);
        }

        public static async Task QuitAsync()
        {
            if (_context.Value != null)
            {
                await _context.Value.Tracing.StopAsync(new()
                {
                    Path = $"Traces/trace-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
                });
            }

            await _page.Value?.CloseAsync()!;
            await _context.Value?.CloseAsync()!;
            await _browser.Value?.CloseAsync()!;
            _playwright.Value?.Dispose();
        }

        public static async Task<IPage> NewPageAsync()
        {
            return await _context.Value!.NewPageAsync();
        }
    }
}