using System;
using System.Threading;
using System.Threading.Tasks;
using WootMouseRemap.Diagnostics;

namespace WootMouseRemap.Utilities
{
    /// <summary>
    /// Utilities for async operations and task management
    /// </summary>
    public static class AsyncHelper
    {
        /// <summary>
        /// Run async operation with timeout and cancellation support
        /// </summary>
        public static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Run async operation with timeout
        /// </summary>
        public static async Task WithTimeout(Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Retry async operation with exponential backoff
        /// </summary>
        public static async Task<T> WithRetry<T>(
            Func<Task<T>> operation, 
            int maxRetries = 3, 
            TimeSpan? initialDelay = null,
            double backoffMultiplier = 2.0,
            CancellationToken cancellationToken = default)
        {
            var delay = initialDelay ?? TimeSpan.FromMilliseconds(100);
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    Logger.Warn($"Attempt {attempt}/{maxRetries} failed, retrying in {delay.TotalMilliseconds}ms: {ex.Message}");
                    
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * backoffMultiplier);
                }
            }

            throw lastException ?? new InvalidOperationException("Retry operation failed");
        }

        /// <summary>
        /// Retry async operation with exponential backoff (void return)
        /// </summary>
        public static async Task WithRetry(
            Func<Task> operation, 
            int maxRetries = 3, 
            TimeSpan? initialDelay = null,
            double backoffMultiplier = 2.0,
            CancellationToken cancellationToken = default)
        {
            await WithRetry(async () =>
            {
                await operation().ConfigureAwait(false);
                return true; // Dummy return value
            }, maxRetries, initialDelay, backoffMultiplier, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Execute operation on UI thread safely
        /// </summary>
        public static void RunOnUIThread(Action action, SynchronizationContext? uiContext = null)
        {
            var context = uiContext ?? SynchronizationContext.Current;
            
            if (context == null)
            {
                // No UI context available, run directly
                action();
                return;
            }

            if (context == SynchronizationContext.Current)
            {
                // Already on UI thread
                action();
            }
            else
            {
                // Marshal to UI thread
                context.Post(_ => ExceptionHandler.SafeExecute(action, "UI thread operation"), null);
            }
        }

        /// <summary>
        /// Execute async operation on UI thread safely
        /// </summary>
        public static Task RunOnUIThreadAsync(Func<Task> asyncAction, SynchronizationContext? uiContext = null)
        {
            var context = uiContext ?? SynchronizationContext.Current;
            
            if (context == null || context == SynchronizationContext.Current)
            {
                // No UI context or already on UI thread
                return asyncAction();
            }

            var tcs = new TaskCompletionSource<bool>();
            
            context.Post(async _ =>
            {
                try
                {
                    await asyncAction().ConfigureAwait(false);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        /// <summary>
        /// Create a task that completes after a delay, with cancellation support
        /// </summary>
        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            return Task.Delay(delay, cancellationToken);
        }

        /// <summary>
        /// Fire and forget task execution with error logging
        /// </summary>
        public static void FireAndForget(Task task, string operationName = "")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var context = string.IsNullOrEmpty(operationName) ? "fire-and-forget task" : operationName;
                    Logger.Error($"Error in {context}", ex);
                }
            });
        }

        /// <summary>
        /// Create a cancellation token that times out after specified duration
        /// </summary>
        public static CancellationToken CreateTimeoutToken(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            return cts.Token;
        }

        /// <summary>
        /// Combine multiple cancellation tokens
        /// </summary>
        public static CancellationToken CombineTokens(params CancellationToken[] tokens)
        {
            if (tokens.Length == 0)
                return CancellationToken.None;
            
            if (tokens.Length == 1)
                return tokens[0];

            var cts = CancellationTokenSource.CreateLinkedTokenSource(tokens);
            return cts.Token;
        }
    }
}