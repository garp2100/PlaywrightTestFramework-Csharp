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