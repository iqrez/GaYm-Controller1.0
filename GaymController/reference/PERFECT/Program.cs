using System;
using System.IO;
using System.Windows.Forms;
using WootMouseRemap.Configuration;
using WootMouseRemap.Diagnostics;

namespace WootMouseRemap
{
    internal static class Program
    {
        private static PerformanceMonitor? _performanceMonitor;

        [STAThread]
        static void Main()
        {
            // Initialize directories
            InitializeDirectories();

            // Single instance check
            using var mutex = new System.Threading.Mutex(true, "WootMouseRemap.Singleton", out bool isNew);
            if (!isNew) 
            {
                MessageBox.Show("WootMouseRemap is already running.", "Already Running", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Initialize configuration and logging
                var config = AppConfig.Instance;
                Logger.Init("Logs", config.Logging.MinLevel);
                Logger.Info("=== WootMouseRemap Starting ===");
                Logger.Info($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                Logger.Info($"OS: {Environment.OSVersion}");
                Logger.Info($".NET: {Environment.Version}");

                // Initialize performance monitoring if enabled
                if (config.Performance.EnablePerformanceCounters)
                {
                    _performanceMonitor = new PerformanceMonitor(TimeSpan.FromMinutes(1));
                }

                // Configure Windows Forms
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Set up global exception handlers
                SetupExceptionHandlers();

                // Initialize core components
                Logger.Info("Initializing core components...");
                
                using var msgWin = new RawInputMsgWindow();
                using var raw = new RawInput(msgWin);
                
                Logger.Info("Installing low-level hooks...");
                LowLevelHooks.Install();

                using var pad = new Xbox360ControllerWrapper();
                Logger.Info("Connecting virtual controller...");
                pad.Connect();

                // Set up cleanup on exit
                Application.ApplicationExit += (_, __) => Cleanup(pad);

                // Start main form
                Logger.Info("Starting main application form...");
                using var form = new OverlayForm(pad, raw, msgWin);
                
                Logger.Info("Application started successfully");
                Application.Run(form);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleCritical(ex, "application startup");
            }
            finally
            {
                Logger.Info("=== WootMouseRemap Shutting Down ===");
                _performanceMonitor?.Dispose();
            }
        }

        private static void InitializeDirectories()
        {
            var directories = new[] { "Logs", "Profiles", "Config", "Backups" };
            
            foreach (var dir in directories)
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create directory '{dir}': {ex.Message}", 
                        "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                Logger.Critical("Unhandled domain exception", exception);
                
                try 
                { 
                    File.AppendAllText(Path.Combine("Logs", "fatal.txt"), 
                        $"[Unhandled] {DateTime.Now:u}\n{e.ExceptionObject}\n\n"); 
                } 
                catch { }
                
                MessageBox.Show(
                    $"A fatal error occurred and the application must close.\n\n" +
                    $"Error: {exception?.Message ?? "Unknown error"}\n\n" +
                    $"Please check Logs\\fatal.txt and woot.log for more details.",
                    "Fatal Error - WootMouseRemap", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            };

            Application.ThreadException += (_, e) =>
            {
                Logger.Error("Unhandled thread exception", e.Exception);
                
                try 
                { 
                    File.AppendAllText(Path.Combine("Logs", "fatal.txt"), 
                        $"[Thread] {DateTime.Now:u}\n{e.Exception}\n\n"); 
                } 
                catch { }

                var result = MessageBox.Show(
                    $"An error occurred:\n\n{e.Exception.Message}\n\n" +
                    $"Would you like to continue running the application?\n\n" +
                    $"Click 'No' to close the application safely.",
                    "Error - WootMouseRemap", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    Application.Exit();
                }
            };
        }

        private static void Cleanup(Xbox360ControllerWrapper pad)
        {
            Logger.Info("Performing cleanup...");
            
            ExceptionHandler.SafeExecute(() => LowLevelHooks.Uninstall(), "uninstalling hooks");
            ExceptionHandler.SafeDispose(pad, "virtual controller");
            
            // Save configuration
            ExceptionHandler.SafeExecute(() => AppConfig.Instance.Save(), "saving configuration");
            
            Logger.Info("Cleanup completed");
        }
    }
}
