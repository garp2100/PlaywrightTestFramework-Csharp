🏗️ Framework Architecture Overview
```
┌─────────────────────────────────────────────────────────────────────────┐
│                            YOUR TEST CLASS                              │
│                         (e.g., LoginTests.cs)                           │
│                                                                         │
│   [Test] ValidLogin_ShouldNavigateToHomePage()                          │
│       → Uses LoginPage (Page Object)                                    │
│       → Uses ConfigReader (for URLs, settings)                          │
│       → Uses FluentAssertions (for readable assertions)                 │
└───────────────────────────────┬─────────────────────────────────────────┘
│ inherits from
▼
┌─────────────────────────────────────────────────────────────────────────┐
│                             TestBase.cs                                 │
│                     (Core/TestBase.cs)                                  │
│                                                                         │
│   • [OneTimeSetUp] → Initialize ExtentReports                           │
│   • [SetUp]        → Launch browser via PlaywrightDriver                │
│   • [TearDown]     → Screenshot on failure, close browser               │
│   • [OneTimeTearDown] → Generate final HTML report                      │
│                                                                         │
│   Exposes: protected IPage Page (so tests can use it)                   │
└───────────────────────────────┬─────────────────────────────────────────┘
│ uses
▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         PlaywrightDriver.cs                             │
│                      (Core/PlaywrightDriver.cs)                         │
│                                                                         │
│   THE ENGINE - Manages browser lifecycle                                │
│                                                                         │
│   • InitializeAsync() → Creates Playwright → Browser → Context → Page   │
│   • QuitAsync()       → Closes everything, saves trace files            │
│                                                                         │
│   Uses AsyncLocal<T> for thread-safety (parallel test execution)        │
└─────────────────────────────────────────────────────────────────────────┘
```
🔑 The Key Relationships
1. Test → TestBase (Inheritance)
```csharp
   public class LoginTests : TestBase  // Your test inherits TestBase
   {
   // You automatically get:
   // - Browser setup before each test
   // - Browser teardown after each test
   // - Screenshot on failure
   // - Access to "Page" property
   }
```
Why? → DRY principle. Every test needs browser setup/teardown. Put it in one place.

2. TestBase → PlaywrightDriver (Composition)
```csharp
   // TestBase calls PlaywrightDriver like this: 
   [SetUp]
   public async Task Setup()
   {
       await PlaywrightDriver.InitializeAsync(browser, headless);  // Start browser
   }
   
   [TearDown]  
   public async Task TearDown()
   {
       await PlaywrightDriver.QuitAsync();  // Close browser
   }
```
Why? → Separation of concerns. TestBase handles test lifecycle, PlaywrightDriver handles browser lifecycle.

3. Test → Page Objects (Composition)
```csharp
   // In your test:
   _loginPage = new LoginPage(Page);  // Pass the browser page to the page object
   await _loginPage.LoginAsync("user", "pass");
```
Why? → Page Object Model (POM). Each page in your app = one class. If the UI changes, you update ONE file, not 50 tests.

4. Page Object → BasePage (Inheritance)
```csharp
   public class LoginPage : BasePage  // LoginPage inherits common methods
   {
   // Gets: NavigateToAsync(), ClickAsync(), FillAsync(), etc.
   // Only defines: Login-specific locators and actions
   }
```
Why? → Common page actions (click, fill, wait) live in BasePage. Specific pages only define what's unique.

📊 Data Flow (What happens when a test runs)
```
1. NUnit discovers [Test] method
   │
   ▼
2. [OneTimeSetUp] runs ONCE per test class
   └── ExtentReportHelper.InitializeReport()
   │
   ▼
3. [SetUp] runs BEFORE EACH test
   ├── PlaywrightDriver.InitializeAsync()  → Opens browser
   └── ExtentReportHelper.CreateTest()     → Starts logging this test
   │
   ▼
4. Your [Test] method runs
   ├── Creates Page Objects (new LoginPage(Page))
   ├── Reads config (ConfigReader.BaseUrl)
   ├── Interacts with browser (click, fill, assert)
   └── Uses FluentAssertions (should, contain, etc.)
   │
   ▼
5. [TearDown] runs AFTER EACH test
   ├── If FAILED → ScreenshotHelper captures screenshot
   ├── ExtentReportHelper logs pass/fail
   └── PlaywrightDriver.QuitAsync()  → Closes browser
   │
   ▼
6. [OneTimeTearDown] runs ONCE after all tests
   └── ExtentReportHelper.FlushReport()  → Writes HTML report
```

🧩 Supporting Cast (Utilities & Config)

| Component	         | Purpose	                                            | Used By                |
| -----------------  | ---------------------------------------------------- | ---------------------- |
| ConfigReader	     | Reads appsettings.json (URLs, browser, timeouts)	    | TestBase, Tests        |
| ScreenshotHelper	 | Captures screenshots on failure	                    | TestBase               |
| ExtentReportHelper | Generates HTML test reports	                        | TestBase               |
| WaitHelper	     | Reusable wait strategies	                            | Page Objects, Tests    |
| DatabaseHelper	 | Direct DB queries for test setup/validation	        | Tests                  |
| APIHelper	         | REST API calls using Playwright	                    | API Tests              |
| TestDataReader	 | Reads test users from JSON	                        | Tests                  |
| TestDataGenerator	 | Creates random fake data (Bogus)	                    | Tests                  |

🎯 Interview Talking Points
"Why this structure?"

> "Separation of concerns. Browser management is isolated in PlaywrightDriver. Test lifecycle (setup/teardown) is in TestBase. UI interactions are in Page Objects. Tests only contain test logic."

"Why Page Object Model?"

> "Maintainability. When the login page UI changes, I update LoginPage.cs once—not every test that logs in."

"Why AsyncLocal in PlaywrightDriver?"

> "Thread safety for parallel execution. Each test thread gets its own browser instance without conflicts."

"Why TestBase instead of putting setup in each test?"

> "DRY. Every UI test needs a browser. Centralizing it means one place to change if we switch browsers or add tracing."

"What's the test data strategy?"

> "Two approaches: Static data in TestUsers.json for known scenarios (valid/invalid logins), and Bogus-generated data for unique data needs (user registration tests)."

🔄 Quick Mental Model
Think of it as layers:
```
┌─────────────────────────────┐
│      Your Test Code         │  ← Business logic, assertions
├─────────────────────────────┤
│      Page Objects           │  ← UI abstraction layer
├─────────────────────────────┤
│      TestBase               │  ← Test lifecycle management
├─────────────────────────────┤
│      PlaywrightDriver       │  ← Browser lifecycle management  
├─────────────────────────────┤
│      Playwright Library     │  ← Microsoft's browser automation
└─────────────────────────────┘
```

Each layer only talks to the one directly below it. Tests don't know how browsers launch. Page objects don't know about test setup. Clean separation.