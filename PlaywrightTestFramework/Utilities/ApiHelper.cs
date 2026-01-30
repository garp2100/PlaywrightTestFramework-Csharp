using Microsoft.Playwright;

namespace PlaywrightTestFramework.Utilities
{
    public class ApiHelper : IAsyncDisposable
    {
        private readonly IAPIRequestContext _apiContext;

        private ApiHelper(IAPIRequestContext apiContext)
        {
            _apiContext = apiContext;
        }

        public static async Task<ApiHelper> CreateAsync(IPlaywright playwright, string baseUrl)
        {
            var apiContext = await playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = baseUrl,
                IgnoreHTTPSErrors = true
            });
            return new ApiHelper(apiContext);
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