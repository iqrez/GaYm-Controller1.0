using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using WootMouseRemap.Diagnostics;

namespace WootMouseRemap.Utilities
{
    /// <summary>
    /// Memory management and optimization utilities
    /// </summary>
    public static class MemoryManager
    {
        private static readonly object _lock = new();
        private static DateTime _lastGcTime = DateTime.MinValue;
        private static readonly TimeSpan MinGcInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Get current memory usage information
        /// </summary>
        public static MemoryInfo GetMemoryInfo()
        {
            var process = Process.GetCurrentProcess();
            var gcInfo = GC.GetTotalMemory(false);
            
            return new MemoryInfo
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                ManagedMemory = gcInfo,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }

        /// <summary>
        /// Perform memory cleanup if needed
        /// </summary>
        public static void OptimizeMemory(bool force = false)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (!force && (now - _lastGcTime) < MinGcInterval)
                    return;

                try
                {
                    var beforeMemory = GC.GetTotalMemory(false);
                    
                    // Collect garbage
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    // Compact large object heap if available
                    if (GCSettings.LargeObjectHeapCompactionMode == GCLargeObjectHeapCompactionMode.Default)
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();
                    }

                    var afterMemory = GC.GetTotalMemory(false);
                    var freed = beforeMemory - afterMemory;
                    
                    _lastGcTime = now;
                    
                    if (freed > 0)
                    {
                        Logger.Debug($"Memory optimization freed {freed / 1024} KB");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error during memory optimization", ex);
                }
            }
        }

        /// <summary>
        /// Set memory pressure hint for GC
        /// </summary>
        public static void SetMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated > 0)
            {
                GC.AddMemoryPressure(bytesAllocated);
            }
        }

        /// <summary>
        /// Remove memory pressure hint for GC
        /// </summary>
        public static void RemoveMemoryPressure(long bytesFreed)
        {
            if (bytesFreed > 0)
            {
                GC.RemoveMemoryPressure(bytesFreed);
            }
        }

        /// <summary>
        /// Check if memory usage is high
        /// </summary>
        public static bool IsMemoryUsageHigh(long thresholdMB = 500)
        {
            var info = GetMemoryInfo();
            return (info.WorkingSet / 1024 / 1024) > thresholdMB;
        }

        /// <summary>
        /// Log current memory statistics
        /// </summary>
        public static void LogMemoryStats()
        {
            var info = GetMemoryInfo();
            Logger.Debug($"Memory Stats - Working Set: {info.WorkingSet / 1024 / 1024} MB, " +
                        $"Managed: {info.ManagedMemory / 1024 / 1024} MB, " +
                        $"GC Gen0: {info.Gen0Collections}, Gen1: {info.Gen1Collections}, Gen2: {info.Gen2Collections}");
        }

        /// <summary>
        /// Configure GC for low-latency scenarios
        /// </summary>
        public static void ConfigureForLowLatency()
        {
            try
            {
                // Set concurrent GC mode for better responsiveness
                if (GCSettings.IsServerGC)
                {
                    Logger.Info("Server GC detected - already optimized for throughput");
                }
                else
                {
                    Logger.Info("Workstation GC detected - configuring for low latency");
                }

                // Set latency mode for interactive applications
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                Logger.Debug($"GC Latency Mode set to: {GCSettings.LatencyMode}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error configuring GC settings", ex);
            }
        }

        /// <summary>
        /// Monitor memory usage and trigger cleanup if needed
        /// </summary>
        public static void MonitorAndOptimize(long highWaterMarkMB = 300)
        {
            var info = GetMemoryInfo();
            var workingSetMB = info.WorkingSet / 1024 / 1024;

            if (workingSetMB > highWaterMarkMB)
            {
                Logger.Warn($"High memory usage detected: {workingSetMB} MB, triggering optimization");
                OptimizeMemory(force: true);
            }
        }

        /// <summary>
        /// Create a memory monitoring scope
        /// </summary>
        public static IDisposable CreateMonitoringScope(string operationName)
        {
            return new MemoryMonitoringScope(operationName);
        }

        private class MemoryMonitoringScope : IDisposable
        {
            private readonly string _operationName;
            private readonly long _startMemory;
            private readonly Stopwatch _stopwatch;

            public MemoryMonitoringScope(string operationName)
            {
                _operationName = operationName;
                _startMemory = GC.GetTotalMemory(false);
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);
                var memoryDelta = endMemory - _startMemory;
                
                Logger.Debug($"Memory scope '{_operationName}': {memoryDelta / 1024} KB allocated, " +
                           $"Duration: {_stopwatch.ElapsedMilliseconds} ms");
            }
        }
    }

    public class MemoryInfo
    {
        public long WorkingSet { get; set; }
        public long PrivateMemory { get; set; }
        public long VirtualMemory { get; set; }
        public long ManagedMemory { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
    }
}