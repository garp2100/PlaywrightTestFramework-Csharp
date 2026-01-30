using NUnit.Framework;
using PlaywrightTestFramework.Config;
using PlaywrightTestFramework.Core;
using PlaywrightTestFramework.Utilities;

namespace PlaywrightTestFramework.Tests.APITests
{
    [TestFixture]
    public class APITestBase
    {
        private APIHelper? _apiHelper;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            await PlaywrightDriver.InitializeAsync();
            _apiHelper = await APIHelper.CreateAsync(PlaywrightDriver.Playwright!, ConfigReader.BaseApiUrl);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _apiHelper!.DisposeAsync();
            await PlaywrightDriver.QuitAsync();
        }
    }
}