using Microsoft.Playwright;

namespace PlaywrightTestFramework.Core
{
    public class PlaywrightDriver
    {
        private static IPlaywright? _playwright;
        private static IBrowser? _browser;
        private static IBrowserContext? _context;
        private static IPage? _page;

        public static IPlaywright? Playwright => _playwright;
        public static IBrowser? Browser => _browser;
        public static IBrowserContext? Context => _context;
        public static IPage? Page => _page;

        public static async Task InitializeAsync(string browserType = "chromium", bool headless = false)
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            _browser = browserType.ToLower() switch
            {
                "chromium" => await _playwright.Chromium.LaunchAsync(new() { Headless = headless }),
                "firefox" => await _playwright.Firefox.LaunchAsync(new() { Headless = headless }),
                "webkit" => await _playwright.Webkit.LaunchAsync(new() { Headless = headless }),
                _ => await _playwright.Chromium.LaunchAsync(new() { Headless = headless })
            };

            _context = await _browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true,
                RecordVideoDir = "Videos/",
                // RecordTraceDir = "Traces/" // if needed
            });

            // Start tracing for debugging
            await _context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });

            _page = await _context.NewPageAsync();
            _page.SetDefaultTimeout(Config.ConfigReader.Timeout);
        }

        public static async Task QuitAsync()
        {
            if (_context != null)
            {
                await _context.Tracing.StopAsync(new()
                {
                    Path = $"Traces/trace-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
                });
            }

            if (_page != null) await _page.CloseAsync();
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();

            _page = null;
            _context = null;
            _browser = null;
            _playwright = null;
        }

        public static async Task<IPage> NewPageAsync()
        {
            return await _context!.NewPageAsync();
        }
    }
}