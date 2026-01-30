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