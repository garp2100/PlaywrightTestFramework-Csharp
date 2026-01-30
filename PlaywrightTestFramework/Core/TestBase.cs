using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightTestFramework.Config;
using PlaywrightTestFramework.Utilities;

namespace PlaywrightTestFramework.Core
{
    [TestFixture]
    public class TestBase
    {
        protected IPage Page => PlaywrightDriver.Page!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // Initialize reporting
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