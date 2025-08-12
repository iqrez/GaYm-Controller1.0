using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WootMouseRemap.Diagnostics;

namespace WootMouseRemap.Configuration
{
    /// <summary>
    /// Centralized application configuration management
    /// </summary>
    public class AppConfig
    {
        private static readonly string ConfigPath = Path.Combine("Config", "app.json");
        private static readonly object _lock = new();
        private static AppConfig? _instance;

        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        // Logging Configuration
        public LoggingConfig Logging { get; set; } = new();

        // UI Configuration
        public UIConfig UI { get; set; } = new();

        // Input Configuration
        public InputConfig Input { get; set; } = new();

        // Performance Configuration
        public PerformanceConfig Performance { get; set; } = new();

        // Feature Flags
        public FeatureFlags Features { get; set; } = new();

        private static AppConfig Load()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, GetJsonOptions());
                    if (config != null)
                    {
                        Logger.Info($"Configuration loaded from {ConfigPath}");
                        return config;
                    }
                }

                Logger.Info("Using default configuration");
                var defaultConfig = new AppConfig();
                defaultConfig.Save(); // Save default config
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load configuration, using defaults", ex);
                return new AppConfig();
            }
        }

        public void Save()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                var json = JsonSerializer.Serialize(this, GetJsonOptions());
                File.WriteAllText(ConfigPath, json);
                Logger.Debug($"Configuration saved to {ConfigPath}");
            }, "saving configuration");
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public void Reset()
        {
            lock (_lock)
            {
                _instance = new AppConfig();
                _instance.Save();
                Logger.Info("Configuration reset to defaults");
            }
        }
    }

    public class LoggingConfig
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Info;
        public bool EnableDebugLogging { get; set; } = false;
        public bool LogToEventLog { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 5;
        public int MaxBackupFiles { get; set; } = 10;
    }

    public class UIConfig
    {
        public bool AlwaysOnTop { get; set; } = true;
        public bool StartMinimized { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowTrayNotifications { get; set; } = true;
        public bool CompactMode { get; set; } = false;
        public bool LockPosition { get; set; } = false;
        public bool EdgeSnapping { get; set; } = true;
        public int EdgeSnapThreshold { get; set; } = 12;
        public string Theme { get; set; } = "Dark";
        public string AccentColor { get; set; } = "#8A2BE2";
        public double Opacity { get; set; } = 1.0;
        public int WindowWidth { get; set; } = 800;
        public int WindowHeight { get; set; } = 600;
        public int WindowX { get; set; } = -1; // -1 means center
        public int WindowY { get; set; } = -1; // -1 means center
    }

    public class InputConfig
    {
        public bool EnableMousePassthrough { get; set; } = true;
        public bool EnableKeyboardPassthrough { get; set; } = true;
        public int MouseSensitivity { get; set; } = 100;
        public bool InvertMouseY { get; set; } = false;
        public int DeadZone { get; set; } = 10;
        public bool EnableSmoothing { get; set; } = true;
        public double SmoothingFactor { get; set; } = 0.8;
        public int NoMotionTimeoutMs { get; set; } = 18;
    }

    public class PerformanceConfig
    {
        public int UIUpdateIntervalMs { get; set; } = 50;
        public int InputPollingIntervalMs { get; set; } = 1;
        public bool EnableMultithreading { get; set; } = true;
        public int MaxConcurrentOperations { get; set; } = 4;
        public bool EnablePerformanceCounters { get; set; } = false;
    }

    public class FeatureFlags
    {
        public bool EnableTelemetry { get; set; } = false;
        public bool EnableAutoUpdates { get; set; } = false;
        public bool EnableExperimentalFeatures { get; set; } = false;
        public bool EnableAdvancedDiagnostics { get; set; } = false;
        public bool EnableProfileBackups { get; set; } = true;
        public bool EnableCrashReporting { get; set; } = true;
    }
}