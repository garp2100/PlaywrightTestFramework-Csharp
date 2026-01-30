# Playwright Setup Guide

## C#
- Create solution in project folder
``` 
dotnet new sln -n YourProjectName
```
- Create test project and .csproj file using Nunit
```
dotnet new nunit -n YourProjectName
dotnet sln add YourProjectName/YourProjectName.csproj
```
- Install core dependencies
```
# Playwright
dotnet add package Microsoft.Playwright
dotnet add package Microsoft.Playwright.NUnit

# Testing Framework (choose one)
dotnet add package NUnit
dotnet add package NUnit3TestAdapter

# Assertions & Utilities
dotnet add package FluentAssertions
dotnet add package Bogus  # For test data generation

# Configuration
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables 
dotnet add package DotNetEnv 

# Reporting (optional)
dotnet add package ExtentReports
dotnet add package Allure.NUnit

# Database (as needed)
dotnet add package Npgsql # connects to PostgreSQL databases
dotnet add package Microsoft.Data.SqlClient # connects to MSSQL databases
dotnet add package Dapper # helper layer for query simplifier

# Accessibility
dotnet add package Deque.AxeCore.Playwright

# API Testing
dotnet add package RestSharp
# OR use Playwright's built-in APIRequestContext

# Build Playwright browsers (Powershell)
# Install Powershell and build the project
dotnet tool install --global PowerShell
dotnet build
# Install browsers
pwsh bin/Debug/net8.0/playwright.ps1 install
```

## Project Structure
```
Be mindful of the namespaces
YourProjectName/
├── Config/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── ConfigReader.cs
├── Core/
│   ├── WebDriverFactory.cs          # (Create FIRST)
│   ├── TestBase.cs                  # (Create SECOND)
│   └── PlaywrightDriver.cs          # (Wrapper - optional)
├── PageObjects/
│   ├── BasePage.cs                  # (Create THIRD)
│   ├── LoginPage.cs
│   ├── HomePage.cs
│   └── ...
├── Tests/
│   ├── UITests/
│   │   ├── LoginTests.cs
│   │   └── ...
│   ├── APITests/
│   │   ├── APITestBase.cs
│   │   └── ProductAPITests.cs
│   └── DatabaseTests/
│       └── DataValidationTests.cs
├── Utilities/
│   ├── ScreenshotHelper.cs
│   ├── WaitHelper.cs
│   ├── DatabaseHelper.cs
│   ├── APIHelper.cs
│   └── ExtentReportHelper.cs
├── TestData/
│   ├── TestDataGenerator.cs
│   └── TestUsers.json
├── Reports/
│    └── (auto-generated)
└── .env

```

## File 1 `Config/appsettings.json` and `.env` file
```json
{
  "TestSettings": {
    "BaseUrl": "https://your-app.com",
    "Browser": "chromium",
    "Headless": false,
    "Timeout": 30000,
    "Screenshot": "OnFailure",
    "VideoRecording": false,
    "TraceRecording": true
  },
  "DatabaseSettings": {
    "ConnectionString": "SET_VIA_ENV_VAR"
  },
  "ApiSettings": {
    "BaseApiUrl": "https://your-app.com/api",
    "ApiKey": ""
  }
}
```

**Store sensitive information such as database username/passwords in the `.env` file. Naming convention is `Section__Key` (double underscore) and it must match the JSON structure from `appsettings.json`
```env
DATABASESETTINGS__CONNECTIONSTRING=Host=localhost;Database=testdb;Username=user;Password=pass
```

**Important**: make sure you add this lines of code to your `YourProjectName.csproj` file so .NET copies files to the output directory
```xml
<ItemGroup>
    <None Update="Config\appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Config\appsettings.Development.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

## File 2 `Congif/ConfigReader.cs` (read settings for all other classes)
```csharp
using Microsoft.Extensions.Configuration;

namespace YourProjectName.Config
{
    public class ConfigReader
    {
        private static IConfiguration? _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    DotNetEnv.Env.Load(); // load appsettings.json values from the .env file

                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
                        .AddEnvironmentVariables();
                    
                    _configuration = builder.Build();
                }
                return _configuration;
            }
        }

        public static string BaseUrl => Configuration["TestSettings:BaseUrl"]!;
        public static string Browser => Configuration["TestSettings:Browser"]!;
        public static bool Headless => bool.Parse(Configuration["TestSettings:Headless"]!);
        public static float Timeout => float.Parse(Configuration["TestSettings:Timeout"]!);
        public static string DbConnectionString => Configuration["DatabaseSettings:ConnectionString"]!;
        public static string BaseApiUrl => Configuration["ApiSettings:BaseApiUrl"]!;
    }
}
```

## File 3 `Core/PlaywrightDriver.cs` (singleton pattern that manages Playwright/Browser lifecycle)
```csharp
using Microsoft.Playwright;

namespace YourProjectName.Core
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
```

## File 4 `Core/TestBase.cs` (base class for all UI tests)
```csharp
using Microsoft.Playwright;
using YourProjectName.Config;
using YourProjectName.Utilities;

namespace YourProjectName.Core
{
    [TestFixture]
    public class TestBase
    {
        protected IPage Page => PlaywrightDriver.Page!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            ExtentReportHelper.InitializeReport();
        }

        [SetUp]
        public async Task Setup()
        {
            await PlaywrightDriver.InitializeAsync(
                ConfigReader.Browser,
                ConfigReader.Headless
            );

            // Create test in report
            ExtentReportHelper.CreateTest(TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        public async Task TearDown()
        {
            var testStatus = TestContext.CurrentContext.Result.Outcome.Status;

            if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                var screenshotPath = await ScreenshotHelper.CaptureScreenshotAsync(
                    Page, 
                    TestContext.CurrentContext.Test.Name
                );
                ExtentReportHelper.LogFail(TestContext.CurrentContext.Result.Message, screenshotPath);
            }
            else if (testStatus == NUnit.Framework.Interfaces.TestStatus.Passed)
            {
                ExtentReportHelper.LogPass("Test Passed");
            }

            await PlaywrightDriver.QuitAsync();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ExtentReportHelper.FlushReport();
        }
    }
}
```

## File 5 `PageObjects/BasePage.cs` (common page operations for POM)
```csharp
using Microsoft.Playwright;

namespace YourProjectName.PageObjects
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
```

## File 5 `PageObjects/LoginPage.cs` (first concrete page example)
```csharp
using Microsoft.Playwright;

namespace YourProjectName.PageObjects
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
            _usernameInput = page.Locator("[data-automation-id='your-locator-id']");
            _passwordInput = page.Locator("[data-automation-id='your-locator-id']");
            _loginButton = page.Locator("[data-automation-id='your-locator-id']");
            _errorMessage = page.Locator("[data-automation-id='your-locator-id']");
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
```

## Utilities: utilities to assist in different tasks such as database connectivity, screenshots on failure or APIs (build as needed)

## Screenshot helper `Utilities/ScreenshotHelper.cs`
```csharp
using Microsoft.Playwright;

namespace YourProjectName.Utilities
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
```

## Database Helper `Utilities/DatabaseHelper.cs`
```csharp
using Dapper;
using Npgsql;
using YourProjectName.Config;

namespace YourProjectName.Utilities
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            _connectionString = ConfigReader.DbConnectionString;
        }

        public async Task<T?> ExecuteScalarAsync<T>(string query, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<T>(query, parameters);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<T>(query, parameters);
        }

        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteAsync(query, parameters);
        }
    }
}
```

## API Helper `Utilities/APIHelper.cs`
```csharp
uusing Microsoft.Playwright;

namespace YourProjectName.Utilities
{
    public class APIHelper : IAsyncDisposable
    {
        private readonly IAPIRequestContext _apiContext;

        private APIHelper(IAPIRequestContext apiContext)
        {
            _apiContext = apiContext;
        }

        public static async Task<APIHelper> CreateAsync(IPlaywright playwright, string baseUrl)
        {
            var apiContext = await playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = baseUrl,
                IgnoreHTTPSErrors = true
            });
            return new APIHelper(apiContext);
        }

        public async Task<IAPIResponse> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
        {
            return await _apiContext.GetAsync(endpoint, new() { Headers = headers });
        }

        public async Task<IAPIResponse> PostAsync(string endpoint, object? data = null, Dictionary<string, string>? headers = null)
        {
            return await _apiContext.PostAsync(endpoint, new() 
            { 
                DataObject = data,
                Headers = headers 
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _apiContext.DisposeAsync();
        }
    }
}
```

## Wait Helper `Utilities/WaitHelper.cs`
```csharp
using System.Diagnostics;
using Microsoft.Playwright;

namespace PlaywrightTestFramework.Utilities
{
    /// <summary>
    /// Provides reusable wait strategies for Playwright tests
    /// </summary>
    public static class WaitHelper
    {
        private const int DefaultTimeout = 30000; // 30 seconds
        private const int DefaultPollingInterval = 500; // 500ms

        #region Element State Waits

        /// <summary>
        /// Wait for element to be visible
        /// </summary>
        public static async Task WaitForElementVisibleAsync(IPage page, string selector, int timeout = DefaultTimeout)
        {
            await page.WaitForSelectorAsync(selector, new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout
            });
        }

        /// <summary>
        /// Wait for element to be hidden
        /// </summary>
        public static async Task WaitForElementHiddenAsync(IPage page, string selector, int timeout = DefaultTimeout)
        {
            await page.WaitForSelectorAsync(selector, new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeout
            });
        }

        /// <summary>
        /// Wait for element to be attached to DOM
        /// </summary>
        public static async Task WaitForElementAttachedAsync(IPage page, string selector, int timeout = DefaultTimeout)
        {
            await page.WaitForSelectorAsync(selector, new()
            {
                State = WaitForSelectorState.Attached,
                Timeout = timeout
            });
        }

        /// <summary>
        /// Wait for element to be detached from DOM
        /// </summary>
        public static async Task WaitForElementDetachedAsync(IPage page, string selector, int timeout = DefaultTimeout)
        {
            await page.WaitForSelectorAsync(selector, new()
            {
                State = WaitForSelectorState.Detached,
                Timeout = timeout
            });
        }

        #endregion

        #region URL and Navigation Waits

        /// <summary>
        /// Wait for URL to match pattern
        /// </summary>
        public static async Task WaitForUrlAsync(IPage page, string urlPattern, int timeout = DefaultTimeout)
        {
            await page.WaitForURLAsync(urlPattern, new() { Timeout = timeout });
        }

        /// <summary>
        /// Wait for URL to contain text
        /// </summary>
        public static async Task WaitForUrlContainsAsync(IPage page, string urlPart, int timeout = DefaultTimeout)
        {
            await page.WaitForURLAsync($"**/*{urlPart}*", new() { Timeout = timeout });
        }

        /// <summary>
        /// Wait for page load state
        /// </summary>
        public static async Task WaitForLoadStateAsync(IPage page, LoadState state = LoadState.Load, int timeout = DefaultTimeout)
        {
            await page.WaitForLoadStateAsync(state, new() { Timeout = timeout });
        }

        /// <summary>
        /// Wait for network to be idle
        /// </summary>
        public static async Task WaitForNetworkIdleAsync(IPage page, int timeout = DefaultTimeout)
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = timeout });
        }

        #endregion

        #region Text and Content Waits

        /// <summary>
        /// Wait for element to contain specific text
        /// </summary>
        public static async Task WaitForTextAsync(IPage page, string selector, string expectedText, int timeout = DefaultTimeout)
        {
            var locator = page.Locator(selector);
            await locator.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout
            });

            await WaitForConditionAsync(async () =>
            {
                var text = await locator.TextContentAsync();
                return text?.Contains(expectedText) ?? false;
            }, timeout, $"Text '{expectedText}' to appear in element '{selector}'");
        }

        /// <summary>
        /// Wait for element to have specific attribute value
        /// </summary>
        public static async Task WaitForAttributeAsync(IPage page, string selector, string attribute, string expectedValue, int timeout = DefaultTimeout)
        {
            var locator = page.Locator(selector);
            
            await WaitForConditionAsync(async () =>
            {
                var value = await locator.GetAttributeAsync(attribute);
                return value == expectedValue;
            }, timeout, $"Attribute '{attribute}' to have value '{expectedValue}' on element '{selector}'");
        }

        #endregion

        #region Count and Collection Waits

        /// <summary>
        /// Wait for specific count of elements
        /// </summary>
        public static async Task WaitForElementCountAsync(IPage page, string selector, int expectedCount, int timeout = DefaultTimeout)
        {
            await WaitForConditionAsync(async () =>
            {
                var count = await page.Locator(selector).CountAsync();
                return count == expectedCount;
            }, timeout, $"Element count of '{selector}' to be {expectedCount}");
        }

        /// <summary>
        /// Wait for at least minimum count of elements
        /// </summary>
        public static async Task WaitForMinimumElementCountAsync(IPage page, string selector, int minCount, int timeout = DefaultTimeout)
        {
            await WaitForConditionAsync(async () =>
            {
                var count = await page.Locator(selector).CountAsync();
                return count >= minCount;
            }, timeout, $"Element count of '{selector}' to be at least {minCount}");
        }

        #endregion

        #region Custom Condition Waits

        /// <summary>
        /// Wait for custom condition with polling
        /// </summary>
        public static async Task WaitForConditionAsync(
            Func<Task<bool>> condition,
            int timeout = DefaultTimeout,
            string? conditionDescription = null,
            int pollingInterval = DefaultPollingInterval)
        {
            var stopwatch = Stopwatch.StartNew();
            var lastException = default(Exception);

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                try
                {
                    if (await condition())
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(pollingInterval);
            }

            var message = conditionDescription != null 
                ? $"Timeout waiting for condition: {conditionDescription}" 
                : "Timeout waiting for condition";

            if (lastException != null)
            {
                throw new TimeoutException($"{message}. Last error: {lastException.Message}", lastException);
            }

            throw new TimeoutException(message);
        }

        /// <summary>
        /// Wait for condition with return value
        /// </summary>
        public static async Task<T> WaitForConditionAsync<T>(
            Func<Task<T?>> condition,
            Func<T?, bool> predicate,
            int timeout = DefaultTimeout,
            string? conditionDescription = null,
            int pollingInterval = DefaultPollingInterval)
        {
            var stopwatch = Stopwatch.StartNew();
            var lastException = default(Exception);
            var lastValue = default(T);

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                try
                {
                    lastValue = await condition();
                    if (predicate(lastValue))
                    {
                        return lastValue!;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(pollingInterval);
            }

            var message = conditionDescription != null
                ? $"Timeout waiting for condition: {conditionDescription}"
                : "Timeout waiting for condition";

            if (lastException != null)
            {
                throw new TimeoutException($"{message}. Last error: {lastException.Message}", lastException);
            }

            throw new TimeoutException($"{message}. Last value: {lastValue}");
        }

        #endregion

        #region JavaScript Execution Waits

        /// <summary>
        /// Wait for JavaScript condition to be true
        /// </summary>
        public static async Task WaitForJavaScriptConditionAsync(IPage page, string jsExpression, int timeout = DefaultTimeout)
        {
            await page.WaitForFunctionAsync(jsExpression, arg: null, new() { Timeout = timeout });
        }

        /// <summary>
        /// Wait for jQuery to be ready (if using jQuery)
        /// </summary>
        public static async Task WaitForJQueryAsync(IPage page, int timeout = DefaultTimeout)
        {
            await page.WaitForFunctionAsync("() => typeof jQuery !== 'undefined' && jQuery.active === 0",
                new PageWaitForFunctionOptions { Timeout = timeout });
        }

        /// <summary>
        /// Wait for Angular to be ready (if using Angular)
        /// </summary>
        public static async Task WaitForAngularAsync(IPage page, int timeout = DefaultTimeout)
        {
            await page.WaitForFunctionAsync(
                "() => window.getAllAngularTestabilities().findIndex(x => !x.isStable()) === -1",
                new PageWaitForFunctionOptions { Timeout = timeout });
        }

        #endregion

        #region API and Response Waits

        /// <summary>
        /// Wait for specific API response
        /// </summary>
        public static async Task<IResponse> WaitForResponseAsync(
            IPage page, 
            string urlPattern, 
            int timeout = DefaultTimeout)
        {
            return await page.WaitForResponseAsync(urlPattern, new() { Timeout = timeout });
        }

        /// <summary>
        /// Wait for API response with status code
        /// </summary>
        public static async Task<IResponse> WaitForResponseWithStatusAsync(
            IPage page,
            string urlPattern,
            int expectedStatus,
            int timeout = DefaultTimeout)
        {
            return await page.WaitForResponseAsync(response =>
                response.Url.Contains(urlPattern) && response.Status == expectedStatus,
                new() { Timeout = timeout }
            );
        }

        /// <summary>
        /// Wait for request to be made
        /// </summary>
        public static async Task<IRequest> WaitForRequestAsync(
            IPage page,
            string urlPattern,
            int timeout = DefaultTimeout)
        {
            return await page.WaitForRequestAsync(urlPattern, new() { Timeout = timeout });
        }

        #endregion

        #region Download and File Waits

        /// <summary>
        /// Wait for download to start
        /// </summary>
        public static async Task<IDownload> WaitForDownloadAsync(
            IPage page,
            Func<Task> triggerAction,
            int timeout = DefaultTimeout)
        {
            var downloadTask = page.WaitForDownloadAsync(new() { Timeout = timeout });
            await triggerAction();
            return await downloadTask;
        }

        #endregion

        #region Popup and Dialog Waits

        /// <summary>
        /// Wait for popup window
        /// </summary>
        public static async Task<IPage> WaitForPopupAsync(
            IPage page,
            Func<Task> triggerAction,
            int timeout = DefaultTimeout)
        {
            var popupTask = page.WaitForPopupAsync(new() { Timeout = timeout });
            await triggerAction();
            return await popupTask;
        }

        /// <summary>
        /// Wait for alert dialog
        /// </summary>
        public static async Task<IDialog> WaitForDialogAsync(
            IPage page,
            Func<Task> triggerAction,
            int timeout = DefaultTimeout)
        {
            var tcs = new TaskCompletionSource<IDialog>();
            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() => tcs.TrySetException(new TimeoutException($"Timeout waiting for dialog after {timeout}ms")));

            void DialogHandler(object? sender, IDialog dialog)
            {
                tcs.TrySetResult(dialog);
            }

            page.Dialog += DialogHandler;
            try
            {
                await triggerAction();
                return await tcs.Task;
            }
            finally
            {
                page.Dialog -= DialogHandler;
                cts.Dispose();
            }
        }

        #endregion

        #region Retry Logic

        /// <summary>
        /// Retry action with exponential backoff
        /// </summary>
        public static async Task<T> RetryAsync<T>(
            Func<Task<T>> action,
            int maxAttempts = 3,
            int initialDelayMs = 1000,
            double backoffMultiplier = 2.0)
        {
            var attempt = 0;
            var delay = initialDelayMs;

            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                    {
                        throw new Exception($"Failed after {maxAttempts} attempts", ex);
                    }

                    await Task.Delay(delay);
                    delay = (int)(delay * backoffMultiplier);
                }
            }
        }

        /// <summary>
        /// Retry action ignoring specific exceptions
        /// </summary>
        public static async Task<T> RetryAsync<T, TException>(
            Func<Task<T>> action,
            int maxAttempts = 3,
            int delayMs = 1000) where TException : Exception
        {
            var attempt = 0;

            while (true)
            {
                try
                {
                    return await action();
                }
                catch (TException)
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }

                    await Task.Delay(delayMs);
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Static wait (use sparingly, prefer explicit waits)
        /// </summary>
        public static async Task WaitAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        /// <summary>
        /// Wait for element to be clickable (visible and enabled)
        /// </summary>
        public static async Task WaitForElementClickableAsync(IPage page, string selector, int timeout = DefaultTimeout)
        {
            var locator = page.Locator(selector);
            
            await WaitForConditionAsync(async () =>
            {
                var isVisible = await locator.IsVisibleAsync();
                var isEnabled = await locator.IsEnabledAsync();
                return isVisible && isEnabled;
            }, timeout, $"Element '{selector}' to be clickable");
        }

        #endregion
    }
}
```

## Extent Report Helper (.NET reports library) `Utilities/ExtentReportHelper.cs`
```csharp
using System.Text;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;

namespace PlaywrightTestFramework.Utilities
{
    /// <summary>
    /// Thread-safe ExtentReports helper for parallel test execution
    /// </summary>
    public static class ExtentReportHelper
    {
        private static volatile ExtentReports? _extent;
        private static readonly AsyncLocal<ExtentTest?> _test = new();
        private static readonly object _lock = new();
        private static string _reportPath = string.Empty;

        /// <summary>
        /// Initialize ExtentReports (call once in OneTimeSetUp)
        /// </summary>
        public static void InitializeReport(string reportName = "TestReport", string documentTitle = "Automation Test Results")
        {
            if (_extent != null)
                return;

            lock (_lock)
            {
                if (_extent != null)
                    return;

                var reportDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
                if (!Directory.Exists(reportDirectory))
                {
                    Directory.CreateDirectory(reportDirectory);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _reportPath = Path.Combine(reportDirectory, $"{reportName}_{timestamp}.html");

                var htmlReporter = new ExtentSparkReporter(_reportPath);
                
                // Configuration
                htmlReporter.Config.DocumentTitle = documentTitle;
                htmlReporter.Config.ReportName = reportName;
                htmlReporter.Config.Theme = Theme.Dark;
                htmlReporter.Config.Encoding = "UTF-8";
                htmlReporter.Config.TimeStampFormat = "MMM dd, yyyy HH:mm:ss";

                _extent = new ExtentReports();
                _extent.AttachReporter(htmlReporter);

                // System/Environment Info
                _extent.AddSystemInfo("OS", Environment.OSVersion.ToString());
                _extent.AddSystemInfo("User", Environment.UserName);
                _extent.AddSystemInfo("Machine", Environment.MachineName);
                _extent.AddSystemInfo(".NET Version", Environment.Version.ToString());
                _extent.AddSystemInfo("Execution Time", DateTime.Now.ToString("F"));
            }
        }

        /// <summary>
        /// Create a new test (call in SetUp)
        /// </summary>
        public static ExtentTest CreateTest(string testName, string? description = null)
        {
            if (_extent == null)
            {
                throw new InvalidOperationException("ExtentReports not initialized. Call InitializeReport() first.");
            }

            lock (_lock)
            {
                _test.Value = string.IsNullOrEmpty(description)
                    ? _extent.CreateTest(testName)
                    : _extent.CreateTest(testName, description);
            }

            return _test.Value;
        }

        /// <summary>
        /// Create a test node (for BDD-style or nested tests)
        /// </summary>
        public static ExtentTest CreateNode(string nodeName, string? description = null)
        {
            if (_test.Value == null)
            {
                throw new InvalidOperationException("No active test. Call CreateTest() first.");
            }

            return string.IsNullOrEmpty(description)
                ? _test.Value.CreateNode(nodeName)
                : _test.Value.CreateNode(nodeName, description);
        }

        #region Logging Methods

        /// <summary>
        /// Log a pass message
        /// </summary>
        public static void LogPass(string message)
        {
            _test.Value?.Pass(message);
        }

        /// <summary>
        /// Log a pass with screenshot
        /// </summary>
        public static void LogPass(string message, string? screenshotPath)
        {
            if (string.IsNullOrEmpty(screenshotPath))
            {
                LogPass(message);
                return;
            }

            _test.Value?.Pass(message, MediaEntityBuilder.CreateScreenCaptureFromPath(screenshotPath).Build());
        }

        /// <summary>
        /// Log a fail message
        /// </summary>
        public static void LogFail(string message)
        {
            _test.Value?.Fail(message);
        }

        /// <summary>
        /// Log a fail with screenshot
        /// </summary>
        public static void LogFail(string message, string? screenshotPath)
        {
            if (string.IsNullOrEmpty(screenshotPath))
            {
                LogFail(message);
                return;
            }

            _test.Value?.Fail(message, MediaEntityBuilder.CreateScreenCaptureFromPath(screenshotPath).Build());
        }

        /// <summary>
        /// Log a fail with exception
        /// </summary>
        public static void LogFail(Exception exception, string? screenshotPath = null)
        {
            if (string.IsNullOrEmpty(screenshotPath))
            {
                _test.Value?.Fail(exception);
            }
            else
            {
                _test.Value?.Fail(exception, MediaEntityBuilder.CreateScreenCaptureFromPath(screenshotPath).Build());
            }
        }

        /// <summary>
        /// Log info message
        /// </summary>
        public static void LogInfo(string message)
        {
            _test.Value?.Info(message);
        }

        /// <summary>
        /// Log info with screenshot
        /// </summary>
        public static void LogInfo(string message, string screenshotPath)
        {
            _test.Value?.Info(message, MediaEntityBuilder.CreateScreenCaptureFromPath(screenshotPath).Build());
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            _test.Value?.Warning(message);
        }

        /// <summary>
        /// Log skip message
        /// </summary>
        public static void LogSkip(string message)
        {
            _test.Value?.Skip(message);
        }

        /// <summary>
        /// Log debug message (uses Info level in ExtentReports 5.x)
        /// </summary>
        public static void LogDebug(string message)
        {
            _test.Value?.Info($"[DEBUG] {message}");
        }

        #endregion

        #region Test Categorization

        /// <summary>
        /// Assign category/tag to test
        /// </summary>
        public static void AssignCategory(params string[] categories)
        {
            _test.Value?.AssignCategory(categories);
        }

        /// <summary>
        /// Assign author to test
        /// </summary>
        public static void AssignAuthor(params string[] authors)
        {
            _test.Value?.AssignAuthor(authors);
        }

        /// <summary>
        /// Assign device to test
        /// </summary>
        public static void AssignDevice(params string[] devices)
        {
            _test.Value?.AssignDevice(devices);
        }

        #endregion

        #region Screenshot Methods

        /// <summary>
        /// Attach base64 screenshot
        /// </summary>
        public static void AttachScreenshot(string base64Screenshot, string title = "Screenshot")
        {
            _test.Value?.Info(title, MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64Screenshot).Build());
        }

        /// <summary>
        /// Attach screenshot from file path
        /// </summary>
        public static void AttachScreenshotFromPath(string screenshotPath, string title = "Screenshot")
        {
            if (File.Exists(screenshotPath))
            {
                _test.Value?.Info(title, MediaEntityBuilder.CreateScreenCaptureFromPath(screenshotPath).Build());
            }
        }

        #endregion

        #region BDD-Style Logging

        /// <summary>
        /// Log Given step
        /// </summary>
        public static void LogGiven(string step)
        {
            _test.Value?.Info($"<b>Given:</b> {step}");
        }

        /// <summary>
        /// Log When step
        /// </summary>
        public static void LogWhen(string step)
        {
            _test.Value?.Info($"<b>When:</b> {step}");
        }

        /// <summary>
        /// Log Then step
        /// </summary>
        public static void LogThen(string step)
        {
            _test.Value?.Info($"<b>Then:</b> {step}");
        }

        /// <summary>
        /// Log And step
        /// </summary>
        public static void LogAnd(string step)
        {
            _test.Value?.Info($"<b>And:</b> {step}");
        }

        #endregion

        #region Advanced Logging

        /// <summary>
        /// Log code block
        /// </summary>
        public static void LogCode(string code, string language = "csharp")
        {
            var codeBlock = $"<pre><code class='language-{language}'>{code}</code></pre>";
            _test.Value?.Info(codeBlock);
        }

        /// <summary>
        /// Log JSON data
        /// </summary>
        public static void LogJson(string json)
        {
            var jsonBlock = $"<pre><code class='language-json'>{json}</code></pre>";
            _test.Value?.Info(jsonBlock);
        }

        /// <summary>
        /// Log table data
        /// </summary>
        public static void LogTable(string[,] data)
        {
            var table = new StringBuilder("<table border='1' style='border-collapse: collapse;'>");

            for (int i = 0; i < data.GetLength(0); i++)
            {
                table.Append("<tr>");
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    var tag = i == 0 ? "th" : "td";
                    table.Append($"<{tag}>{data[i, j]}</{tag}>");
                }
                table.Append("</tr>");
            }

            table.Append("</table>");
            _test.Value?.Info(table.ToString());
        }

        #endregion

        /// <summary>
        /// Flush the report (call in OneTimeTearDown)
        /// </summary>
        public static void FlushReport()
        {
            lock (_lock)
            {
                _extent?.Flush();
            }

            // Log the report path
            if (!string.IsNullOrEmpty(_reportPath))
            {
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"Test Report: {_reportPath}");
                Console.WriteLine($"========================================\n");
            }
        }

        /// <summary>
        /// Get current test instance (for advanced scenarios)
        /// </summary>
        public static ExtentTest? GetTest()
        {
            return _test.Value;
        }

        /// <summary>
        /// Get report file path
        /// </summary>
        public static string GetReportPath()
        {
            return _reportPath;
        }
    }
}
```

## Test Classes
## Sample Login Tests `Tests/UITests/LoginTests.cs`
```csharp
using FluentAssertions;
using Microsoft.Playwright;
using YourProjectName.Core;
using YourProjectName.PageObjects;
using YourProjectName.Config;
using YourProjectName.TestData;
using static Microsoft.Playwright.Assertions;

namespace YourProjectName.Tests.UITests
{
    [TestFixture]
    public class LoginTests : TestBase
    {
        private LoginPage? _loginPage;

        [SetUp]
        public async Task TestSetup()
        {
            // NUnit calls TestBase.Setup() automatically before this
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

            // Assert - using Playwright's built-in assertions
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

            // Assert - using FluentAssertions
            var isErrorDisplayed = await _loginPage.IsErrorDisplayedAsync();
            isErrorDisplayed.Should().BeTrue();

            var errorMessage = await _loginPage.GetErrorMessageAsync();
            errorMessage.Should().Contain("Invalid login attempt");
        }
    }
}
```

## Sample API Tests `Tests/APITests/APITestBase.cs`
```csharp
using NUnit.Framework;
using YourProjectName.Core;
using YourProjectName.Utilities;
using YourProjectName.Config;

namespace YourProjectName.Tests.APITests
{
    [TestFixture]
    public class APITestBase
    {
        protected APIHelper? ApiHelper;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            await PlaywrightDriver.InitializeAsync();
            ApiHelper = await APIHelper.CreateAsync(PlaywrightDriver.Playwright!, ConfigReader.BaseApiUrl);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await ApiHelper!.DisposeAsync();
            await PlaywrightDriver.QuitAsync();
        }
    }
}
```

## Test Data `TestData/TestUsers.json` (sample test users and categories)
```json
{
  "validUsers": {
    "admin": {
      "username": "admin@test.com",
      "password": "Admin@123!"
    },
    "standardUser": {
      "username": "john.doe@test.com",
      "password": "User@123!"
    }
  },
  "invalidUsers": {
    "wrongPassword": {
      "username": "admin@test.com",
      "password": "WrongPassword123!"
    },
    "nonExistentUser": {
      "username": "nonexistent@test.com",
      "password": "Password@123!"
    }
  },
  "apiUsers": {
    "apiAdmin": {
      "username": "api.admin@test.com",
      "password": "ApiAdmin@123!",
      "apiKey": "test-api-key-admin-12345",
      "role": "Administrator",
      "scopes": ["read", "write", "delete"]
    }
  },
  "accessibilityTestUsers": {
    "screenReaderUser": {
      "username": "screenreader@test.com",
      "password": "Screen@123!",
      "firstName": "Screen",
      "lastName": "Reader",
      "email": "screenreader@test.com",
      "role": "User",
      "accessibilityProfile": "screen_reader",
      "preferences": {
        "highContrast": true,
        "largeText": true,
        "keyboardOnly": true
      }
    },
    "keyboardNavigationUser": {
      "username": "keyboard@test.com",
      "password": "Keyboard@123!",
      "firstName": "Keyboard",
      "lastName": "Navigator",
      "email": "keyboard@test.com",
      "role": "User",
      "accessibilityProfile": "keyboard_only",
      "preferences": {
        "skipToContent": true,
        "focusIndicators": true
      }
    }
  },
  "dataSeeding": {
    "bulkUsers": [
      {
        "username": "user001@test.com",
        "password": "User@123!",
        "firstName": "User",
        "lastName": "001",
        "role": "User"
      },
      {
        "username": "user002@test.com",
        "password": "User@123!",
        "firstName": "User",
        "lastName": "002",
        "role": "User"
      },
      {
        "username": "user003@test.com",
        "password": "User@123!",
        "firstName": "User",
        "lastName": "003",
        "role": "User"
      }
    ]
  },
  "testConfiguration": {
    "defaultTimeout": 30000,
    "retryAttempts": 3,
    "passwordPolicy": {
      "minLength": 8,
      "requireUppercase": true,
      "requireLowercase": true,
      "requireDigit": true,
      "requireSpecialChar": true
    },
    "lockoutPolicy": {
      "maxFailedAttempts": 5,
      "lockoutDurationMinutes": 30
    }
  }
}
```

## Test Data Reade `TestData/TestDataReader.cs` Helper Class to read the test users JSON file
```csharp
using System.Text.Json;

namespace YourProjectName.TestData
{
    /// <summary>
    /// Helper class to read test users from JSON
    /// </summary>
    public class TestDataReader
    {
        private static TestUsersData? _testUsersData;
        private static readonly object _lock = new();

        public static TestUsersData TestUsers
        {
            get
            {
                if (_testUsersData == null)
                {
                    lock (_lock)
                    {
                        if (_testUsersData == null)
                        {
                            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "TestUsers.json");
                            
                            if (!File.Exists(jsonPath))
                            {
                                throw new FileNotFoundException($"TestUsers.json not found at: {jsonPath}");
                            }

                            var jsonContent = File.ReadAllText(jsonPath);
                            _testUsersData = JsonSerializer.Deserialize<TestUsersData>(jsonContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            }) ?? throw new InvalidOperationException("Failed to deserialize TestUsers.json");
                        }
                    }
                }
                return _testUsersData;
            }
        }

        /// <summary>
        /// Get valid user by key
        /// </summary>
        public static TestUser GetValidUser(string userKey)
        {
            if (TestUsers.ValidUsers.TryGetValue(userKey, out var user))
            {
                return user;
            }
            throw new KeyNotFoundException($"Valid user '{userKey}' not found in TestUsers.json");
        }

        /// <summary>
        /// Get invalid user by key
        /// </summary>
        public static InvalidTestUser GetInvalidUser(string userKey)
        {
            if (TestUsers.InvalidUsers.TryGetValue(userKey, out var user))
            {
                return user;
            }
            throw new KeyNotFoundException($"Invalid user '{userKey}' not found in TestUsers.json");
        }

        /// <summary>
        /// Get API user by key
        /// </summary>
        public static ApiUser GetApiUser(string userKey)
        {
            if (TestUsers.ApiUsers.TryGetValue(userKey, out var user))
            {
                return user;
            }
            throw new KeyNotFoundException($"API user '{userKey}' not found in TestUsers.json");
        }
    }

    #region Data Models

    public class TestUsersData
    {
        public Dictionary<string, TestUser> ValidUsers { get; set; } = new();
        public Dictionary<string, InvalidTestUser> InvalidUsers { get; set; } = new();
        public Dictionary<string, ApiUser> ApiUsers { get; set; } = new();
        public Dictionary<string, TestUser> AccessibilityTestUsers { get; set; } = new();
        public DataSeeding DataSeeding { get; set; } = new();
        public TestConfiguration TestConfiguration { get; set; } = new();
    }

    public class TestUser
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public UserPreferences? Preferences { get; set; }
    }

    public class InvalidTestUser
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApiUser
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class UserPreferences
    {
        public bool? HighContrast { get; set; }
        public bool? LargeText { get; set; }
        public bool? KeyboardOnly { get; set; }
        public bool? SkipToContent { get; set; }
        public bool? FocusIndicators { get; set; }
    }

    public class DataSeeding
    {
        public List<BulkUser> BulkUsers { get; set; } = new();
    }

    public class BulkUser
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class TestConfiguration
    {
        public int DefaultTimeout { get; set; }
        public int RetryAttempts { get; set; }
    }

    #endregion
}
```
### For `TestDataReader.cs` Set JSON file as "Copy to Output Director" in your .csproj file
```xml
<ItemGroup>
  <None Update="TestData\TestUsers.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Test Data Generator (generates fake data for testing)
```csharp
using Bogus;
using Bogus.DataSets;

namespace YourProjectName.TestData
{
    /// <summary>
    /// Generates random, realistic test data using Bogus library
    /// Use this for tests requiring unique data (registrations, creating records, etc.)
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Faker _faker = new Faker("en");

        #region User Data Generation

        /// <summary>
        /// Generate a complete random user - Not all fields may apply
        /// </summary>
        public static GeneratedUser GenerateUser(string? role = null)
        {
            var userFaker = new Faker<GeneratedUser>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
                .RuleFor(u => u.Username, (f, u) => u.Email)
                .RuleFor(u => u.Password, f => GenerateStrongPassword())
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####"))
                .RuleFor(u => u.DateOfBirth, f => f.Date.Past(50, DateTime.Now.AddYears(-18)))
                .RuleFor(u => u.Address, f => f.Address.FullAddress())
                .RuleFor(u => u.City, f => f.Address.City())
                .RuleFor(u => u.State, f => f.Address.StateAbbr())
                .RuleFor(u => u.ZipCode, f => f.Address.ZipCode())
                .RuleFor(u => u.Country, f => "United States")
                .RuleFor(u => u.Company, f => f.Company.CompanyName())
                .RuleFor(u => u.JobTitle, f => f.Name.JobTitle())
                .RuleFor(u => u.Department, f => f.Commerce.Department())
                .RuleFor(u => u.Role, f => role ?? f.PickRandom("User", "Manager", "Administrator"))
                .RuleFor(u => u.Avatar, f => f.Internet.Avatar())
                .RuleFor(u => u.Bio, f => f.Lorem.Paragraph())
                .RuleFor(u => u.Website, f => f.Internet.Url())
                .RuleFor(u => u.IsActive, f => true);

            return userFaker.Generate();
        }

        /// <summary>
        /// Generate multiple random users
        /// </summary>
        public static List<GeneratedUser> GenerateUsers(int count, string? role = null)
        {
            return Enumerable.Range(0, count)
                .Select(_ => GenerateUser(role))
                .ToList();
        }

        /// <summary>
        /// Generate a strong password that meets typical requirements
        /// </summary>
        public static string GenerateStrongPassword(int length = 12)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            var random = new Random();
            var password = new List<char>
            {
                lowercase[random.Next(lowercase.Length)],
                uppercase[random.Next(uppercase.Length)],
                digits[random.Next(digits.Length)],
                special[random.Next(special.Length)]
            };

            var allChars = lowercase + uppercase + digits + special;
            for (int i = password.Count; i < length; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            return new string(password.OrderBy(_ => random.Next()).ToArray());
        }

        #endregion

        #region Product Data Generation

        /// <summary>
        /// Generate a random product - not all fields may apply
        /// </summary>
        public static GeneratedProduct GenerateProduct()
        {
            var productFaker = new Faker<GeneratedProduct>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price(10, 1000, 2)))
                .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
                .RuleFor(p => p.SKU, f => f.Commerce.Ean13())
                .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 500))
                .RuleFor(p => p.Color, f => f.Commerce.Color())
                .RuleFor(p => p.Material, f => f.Commerce.ProductMaterial())
                .RuleFor(p => p.Brand, f => f.Company.CompanyName())
                .RuleFor(p => p.Weight, f => f.Random.Decimal(0.1m, 50m))
                .RuleFor(p => p.IsAvailable, f => f.Random.Bool(0.9f))
                .RuleFor(p => p.Rating, f => f.Random.Decimal(1, 5))
                .RuleFor(p => p.ReviewCount, f => f.Random.Int(0, 1000));

            return productFaker.Generate();
        }

        /// <summary>
        /// Generate multiple random products
        /// </summary>
        public static List<GeneratedProduct> GenerateProducts(int count)
        {
            return Enumerable.Range(0, count)
                .Select(_ => GenerateProduct())
                .ToList();
        }

        #endregion

        #region Order Data Generation

        /// <summary>
        /// Generate a random order - not all fields may apply
        /// </summary>
        public static GeneratedOrder GenerateOrder()
        {
            var orderFaker = new Faker<GeneratedOrder>()
                .RuleFor(o => o.OrderNumber, f => f.Commerce.Ean13())
                .RuleFor(o => o.OrderDate, f => f.Date.Recent(30))
                .RuleFor(o => o.CustomerName, f => f.Name.FullName())
                .RuleFor(o => o.CustomerEmail, (f, o) => f.Internet.Email(o.CustomerName))
                .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress())
                .RuleFor(o => o.BillingAddress, (f, o) => o.ShippingAddress)
                .RuleFor(o => o.TotalAmount, f => decimal.Parse(f.Commerce.Price(50, 5000, 2)))
                .RuleFor(o => o.Status, f => f.PickRandom("Pending", "Processing", "Shipped", "Delivered", "Cancelled"))
                .RuleFor(o => o.PaymentMethod, f => f.PickRandom("Credit Card", "PayPal", "Bank Transfer"))
                .RuleFor(o => o.TrackingNumber, f => f.Random.AlphaNumeric(16).ToUpper())
                .RuleFor(o => o.Notes, f => f.Lorem.Sentence());

            return orderFaker.Generate();
        }

        #endregion

        #region Company/Organization Data

        /// <summary>
        /// Generate random company data - not all fields may apply
        /// </summary>
        public static GeneratedCompany GenerateCompany()
        {
            var companyFaker = new Faker<GeneratedCompany>()
                .RuleFor(c => c.Name, f => f.Company.CompanyName())
                .RuleFor(c => c.LegalName, (f, c) => $"{c.Name} Inc.")
                .RuleFor(c => c.Industry, f => f.Commerce.Department())
                .RuleFor(c => c.TaxId, f => f.Random.Replace("##-#######"))
                .RuleFor(c => c.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####"))
                .RuleFor(c => c.Email, (f, c) => $"info@{c.Name.ToLower().Replace(" ", "")}.com")
                .RuleFor(c => c.Website, (f, c) => $"https://www.{c.Name.ToLower().Replace(" ", "")}.com")
                .RuleFor(c => c.Address, f => f.Address.FullAddress())
                .RuleFor(c => c.City, f => f.Address.City())
                .RuleFor(c => c.State, f => f.Address.StateAbbr())
                .RuleFor(c => c.ZipCode, f => f.Address.ZipCode())
                .RuleFor(c => c.EmployeeCount, f => f.Random.Int(10, 10000))
                .RuleFor(c => c.FoundedYear, f => f.Date.Past(50).Year);

            return companyFaker.Generate();
        }

        #endregion

        #region Credit Card Data (for testing payment forms)

        /// <summary>
        /// Generate test credit card data (DO NOT use real card numbers)
        /// </summary>
        public static GeneratedCreditCard GenerateTestCreditCard()
        {
            var cardFaker = new Faker<GeneratedCreditCard>()
                .RuleFor(c => c.CardholderName, f => f.Name.FullName())
                .RuleFor(c => c.CardNumber, f => f.Finance.CreditCardNumber())
                .RuleFor(c => c.CardType, f => f.PickRandom("Visa", "Mastercard", "American Express"))
                .RuleFor(c => c.ExpirationMonth, f => f.Random.Int(1, 12).ToString("D2"))
                .RuleFor(c => c.ExpirationYear, f => f.Date.Future(5).Year.ToString())
                .RuleFor(c => c.CVV, f => f.Random.Int(100, 999).ToString())
                .RuleFor(c => c.BillingZipCode, f => f.Address.ZipCode());

            return cardFaker.Generate();
        }

        #endregion

        #region Form Data Generation

        /// <summary>
        /// Generate random email address
        /// </summary>
        public static string GenerateEmail(string? firstName = null, string? lastName = null)
        {
            return firstName != null && lastName != null
                ? _faker.Internet.Email(firstName, lastName)
                : _faker.Internet.Email();
        }

        /// <summary>
        /// Generate random phone number
        /// </summary>
        public static string GeneratePhoneNumber(string format = "###-###-####")
        {
            return _faker.Phone.PhoneNumber(format);
        }

        /// <summary>
        /// Generate random US ZIP code
        /// </summary>
        public static string GenerateZipCode()
        {
            return _faker.Address.ZipCode();
        }

        /// <summary>
        /// Generate random sentence
        /// </summary>
        public static string GenerateSentence(int wordCount = 10)
        {
            return _faker.Lorem.Sentence(wordCount);
        }

        /// <summary>
        /// Generate random paragraph
        /// </summary>
        public static string GenerateParagraph()
        {
            return _faker.Lorem.Paragraph();
        }

        /// <summary>
        /// Generate random URL
        /// </summary>
        public static string GenerateUrl()
        {
            return _faker.Internet.Url();
        }

        /// <summary>
        /// Generate random date in the past
        /// </summary>
        public static DateTime GeneratePastDate(int years = 5)
        {
            return _faker.Date.Past(years);
        }

        /// <summary>
        /// Generate random future date
        /// </summary>
        public static DateTime GenerateFutureDate(int years = 5)
        {
            return _faker.Date.Future(years);
        }

        #endregion

        #region API Test Data

        /// <summary>
        /// Generate API key format string
        /// </summary>
        public static string GenerateApiKey()
        {
            return $"test-api-key-{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Generate JWT-like token (for testing, not cryptographically secure)
        /// </summary>
        public static string GenerateTestToken()
        {
            var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"sub\":\"test\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}"));
            var signature = _faker.Random.AlphaNumeric(43);
            return $"{header}.{payload}.{signature}";
        }

        #endregion

        #region Accessibility Test Data

        /// <summary>
        /// Generate test data for accessibility scenarios
        /// </summary>
        public static AccessibilityTestData GenerateAccessibilityTestData()
        {
            return new AccessibilityTestData
            {
                LongText = _faker.Lorem.Paragraphs(5),
                ShortText = _faker.Lorem.Word(),
                SpecialCharacters = "!@#$%^&*()_+-={}[]|:;<>?,./",
                UnicodeText = "Ñoño José García 中文 العربية",
                EmptyString = string.Empty,
                WhitespaceString = "   ",
                VeryLongText = string.Join(" ", Enumerable.Range(0, 1000).Select(_ => _faker.Lorem.Word()))
            };
        }

        #endregion
    }

    #region Generated Data Models

    public class GeneratedUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class GeneratedProduct
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public bool IsAvailable { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class GeneratedOrder
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class GeneratedCompany
    {
        public string Name { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public int FoundedYear { get; set; }
    }

    public class GeneratedCreditCard
    {
        public string CardholderName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string ExpirationMonth { get; set; } = string.Empty;
        public string ExpirationYear { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string BillingZipCode { get; set; } = string.Empty;
    }

    public class AccessibilityTestData
    {
        public string LongText { get; set; } = string.Empty;
        public string ShortText { get; set; } = string.Empty;
        public string SpecialCharacters { get; set; } = string.Empty;
        public string UnicodeText { get; set; } = string.Empty;
        public string EmptyString { get; set; } = string.Empty;
        public string WhitespaceString { get; set; } = string.Empty;
        public string VeryLongText { get; set; } = string.Empty;
    }

    #endregion
}
```