using System.Text.Json;

namespace PlaywrightTestFramework.TestData
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