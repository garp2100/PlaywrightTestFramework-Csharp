using Bogus;
using Bogus.DataSets;

namespace PlaywrightTestFramework.TestData
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