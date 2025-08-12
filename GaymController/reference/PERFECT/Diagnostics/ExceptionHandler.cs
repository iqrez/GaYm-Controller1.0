using System;
using System.Runtime.CompilerServices;

namespace WootMouseRemap.Diagnostics
{
    /// <summary>
    /// Centralized exception handling utility to replace empty catch blocks
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// Safely execute an action and log any exceptions
        /// </summary>
        public static void SafeExecute(Action action, string context = "", [CallerMemberName] string caller = "")
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrEmpty(context) 
                    ? $"Exception in {caller}" 
                    : $"Exception in {caller}: {context}";
                Logger.Error(message, ex);
            }
        }

        /// <summary>
        /// Safely execute a function and return default value on exception
        /// </summary>
        public static T SafeExecute<T>(Func<T> func, T defaultValue = default!, string context = "", [CallerMemberName] string caller = "")
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrEmpty(context) 
                    ? $"Exception in {caller}" 
                    : $"Exception in {caller}: {context}";
                Logger.Error(message, ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely dispose an object and log any exceptions
        /// </summary>
        public static void SafeDispose(IDisposable? disposable, string objectName = "", [CallerMemberName] string caller = "")
        {
            if (disposable == null) return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrEmpty(objectName) 
                    ? $"Exception disposing object in {caller}" 
                    : $"Exception disposing {objectName} in {caller}";
                Logger.Error(message, ex);
            }
        }

        /// <summary>
        /// Execute action with retry logic
        /// </summary>
        public static bool TryExecuteWithRetry(Action action, int maxRetries = 3, int delayMs = 100, string context = "", [CallerMemberName] string caller = "")
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    var message = string.IsNullOrEmpty(context) 
                        ? $"Attempt {attempt}/{maxRetries} failed in {caller}" 
                        : $"Attempt {attempt}/{maxRetries} failed in {caller}: {context}";
                    
                    if (attempt == maxRetries)
                    {
                        Logger.Error(message + " (final attempt)", ex);
                        return false;
                    }
                    else
                    {
                        Logger.Warn(message + ", retrying...");
                        if (delayMs > 0)
                            System.Threading.Thread.Sleep(delayMs);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Handle critical exceptions that should terminate the application
        /// </summary>
        public static void HandleCritical(Exception ex, string context = "", [CallerMemberName] string caller = "")
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Critical exception in {caller}" 
                : $"Critical exception in {caller}: {context}";
            
            Logger.Critical(message, ex);
            
            // Show user-friendly error dialog
            System.Windows.Forms.MessageBox.Show(
                $"A critical error occurred and the application must close.\n\nError: {ex.Message}\n\nPlease check the logs for more details.",
                "Critical Error - WootMouseRemap",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }
}