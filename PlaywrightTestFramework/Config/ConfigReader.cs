using Microsoft.Extensions.Configuration;

namespace PlaywrightTestFramework.Config
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
                    DotNetEnv.Env.Load();
                    
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