using PlaywrightTestFramework.Config;
using PlaywrightTestFramework.Core;
using PlaywrightTestFramework.Utilities;

namespace PlaywrightTestFramework.Tests.ApiTests
{
    [TestFixture]
    public class ApiTestBase
    {
        private ApiHelper? _apiHelper;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            await PlaywrightDriver.InitializeAsync();
            _apiHelper = await ApiHelper.CreateAsync(PlaywrightDriver.Playwright!, ConfigReader.BaseApiUrl);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _apiHelper!.DisposeAsync();
            await PlaywrightDriver.QuitAsync();
        }
    }
}