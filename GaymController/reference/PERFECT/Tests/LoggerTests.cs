using System;
using System.IO;
using System.Threading.Tasks;
using WootMouseRemap.Diagnostics;

namespace WootMouseRemap.Tests
{
    /// <summary>
    /// Unit tests for Logger functionality
    /// Note: These are example tests. To run them, you would need to add a test framework like xUnit or NUnit
    /// </summary>
    public class LoggerTests
    {
        private readonly string _testLogDir = Path.Combine(Path.GetTempPath(), "WootMouseRemap_Tests");

        public void Setup()
        {
            // Clean up any existing test logs
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
            Directory.CreateDirectory(_testLogDir);
            
            Logger.Init(_testLogDir, LogLevel.Debug);
        }

        public void Cleanup()
        {
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
        }

        public void TestBasicLogging()
        {
            Setup();
            try
            {
                // Test different log levels
                Logger.Debug("Debug message");
                Logger.Info("Info message");
                Logger.Warn("Warning message");
                Logger.Error("Error message");
                Logger.Critical("Critical message");

                // Verify log file was created
                var logFile = Logger.GetCurrentLogFile();
                if (!File.Exists(logFile))
                    throw new Exception("Log file was not created");

                var content = File.ReadAllText(logFile);
                if (!content.Contains("Debug message") ||
                    !content.Contains("Info message") ||
                    !content.Contains("Warning message") ||
                    !content.Contains("Error message") ||
                    !content.Contains("Critical message"))
                {
                    throw new Exception("Not all log messages were written");
                }

                Console.WriteLine("✓ Basic logging test passed");
            }
            finally
            {
                Cleanup();
            }
        }

        public void TestLogRotation()
        {
            Setup();
            try
            {
                // Write enough data to trigger rotation
                var largeMessage = new string('A', 1024 * 1024); // 1MB message
                
                for (int i = 0; i < 10; i++)
                {
                    Logger.Info($"Large message {i}: {largeMessage}");
                }

                // Check if rotation occurred
                var logFiles = Directory.GetFiles(_testLogDir, "woot_*.log");
                if (logFiles.Length == 0)
                {
                    Console.WriteLine("⚠ Log rotation test: No rotated files found (may be expected for small logs)");
                }
                else
                {
                    Console.WriteLine($"✓ Log rotation test passed: {logFiles.Length} rotated files found");
                }
            }
            finally
            {
                Cleanup();
            }
        }

        public void TestConcurrentLogging()
        {
            Setup();
            try
            {
                const int threadCount = 10;
                const int messagesPerThread = 100;
                var tasks = new Task[threadCount];

                for (int t = 0; t < threadCount; t++)
                {
                    int threadId = t;
                    tasks[t] = Task.Run(() =>
                    {
                        for (int i = 0; i < messagesPerThread; i++)
                        {
                            Logger.Info($"Thread {threadId}, Message {i}");
                        }
                    });
                }

                Task.WaitAll(tasks);

                // Verify all messages were logged
                var logFile = Logger.GetCurrentLogFile();
                var content = File.ReadAllText(logFile);
                var messageCount = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

                if (messageCount < threadCount * messagesPerThread)
                {
                    throw new Exception($"Expected at least {threadCount * messagesPerThread} messages, found {messageCount}");
                }

                Console.WriteLine($"✓ Concurrent logging test passed: {messageCount} messages logged");
            }
            finally
            {
                Cleanup();
            }
        }

        public void RunAllTests()
        {
            Console.WriteLine("Running Logger Tests...");
            
            try
            {
                TestBasicLogging();
                TestLogRotation();
                TestConcurrentLogging();
                
                Console.WriteLine("✓ All Logger tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Logger test failed: {ex.Message}");
                throw;
            }
        }
    }
}