using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WootMouseRemap.Diagnostics
{
    /// <summary>
    /// Performance monitoring and metrics collection
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly ConcurrentDictionary<string, PerformanceCounter> _counters = new();
        private readonly ConcurrentDictionary<string, MovingAverage> _averages = new();
        private readonly System.Threading.Timer _reportTimer;
        private readonly object _lock = new();
        private bool _disposed = false;

        public PerformanceMonitor(TimeSpan reportInterval)
        {
            _reportTimer = new System.Threading.Timer(ReportMetrics, null, reportInterval, reportInterval);
            Logger.Info("Performance monitor started");
        }

        /// <summary>
        /// Start timing an operation
        /// </summary>
        public IDisposable StartTiming(string operationName)
        {
            return new TimingScope(this, operationName);
        }

        /// <summary>
        /// Record a timing measurement
        /// </summary>
        public void RecordTiming(string operationName, TimeSpan duration)
        {
            if (_disposed) return;

            var counter = _counters.GetOrAdd(operationName, _ => new PerformanceCounter());
            var average = _averages.GetOrAdd(operationName, _ => new MovingAverage(100)); // 100 sample window

            lock (_lock)
            {
                counter.Count++;
                counter.TotalTime += duration;
                counter.LastTime = duration;
                counter.MinTime = counter.MinTime == TimeSpan.Zero ? duration : TimeSpan.FromTicks(Math.Min(counter.MinTime.Ticks, duration.Ticks));
                counter.MaxTime = TimeSpan.FromTicks(Math.Max(counter.MaxTime.Ticks, duration.Ticks));

                average.AddSample(duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Record a counter increment
        /// </summary>
        public void IncrementCounter(string counterName, long value = 1)
        {
            if (_disposed) return;

            var counter = _counters.GetOrAdd(counterName, _ => new PerformanceCounter());
            Interlocked.Add(ref counter.Count, value);
        }

        /// <summary>
        /// Get current metrics for an operation
        /// </summary>
        public PerformanceMetrics? GetMetrics(string operationName)
        {
            if (!_counters.TryGetValue(operationName, out var counter) ||
                !_averages.TryGetValue(operationName, out var average))
            {
                return null;
            }

            lock (_lock)
            {
                return new PerformanceMetrics
                {
                    OperationName = operationName,
                    Count = counter.Count,
                    TotalTime = counter.TotalTime,
                    AverageTime = counter.Count > 0 ? TimeSpan.FromTicks(counter.TotalTime.Ticks / counter.Count) : TimeSpan.Zero,
                    MinTime = counter.MinTime,
                    MaxTime = counter.MaxTime,
                    LastTime = counter.LastTime,
                    MovingAverage = TimeSpan.FromMilliseconds(average.Average)
                };
            }
        }

        /// <summary>
        /// Get all current metrics
        /// </summary>
        public Dictionary<string, PerformanceMetrics> GetAllMetrics()
        {
            var result = new Dictionary<string, PerformanceMetrics>();
            
            foreach (var kvp in _counters)
            {
                var metrics = GetMetrics(kvp.Key);
                if (metrics != null)
                {
                    result[kvp.Key] = metrics;
                }
            }

            return result;
        }

        /// <summary>
        /// Reset all counters
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _counters.Clear();
                _averages.Clear();
                Logger.Info("Performance counters reset");
            }
        }

        private void ReportMetrics(object? state)
        {
            if (_disposed) return;

            try
            {
                var metrics = GetAllMetrics();
                if (metrics.Count == 0) return;

                Logger.Debug("=== Performance Report ===");
                foreach (var kvp in metrics.OrderBy(x => x.Key))
                {
                    var m = kvp.Value;
                    Logger.Debug($"{m.OperationName}: Count={m.Count}, Avg={m.AverageTime.TotalMilliseconds:F2}ms, " +
                               $"Min={m.MinTime.TotalMilliseconds:F2}ms, Max={m.MaxTime.TotalMilliseconds:F2}ms, " +
                               $"MovingAvg={m.MovingAverage.TotalMilliseconds:F2}ms");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error reporting performance metrics", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _reportTimer?.Dispose();
            
            // Final report
            ReportMetrics(null);
            
            Logger.Info("Performance monitor disposed");
        }

        private class TimingScope : IDisposable
        {
            private readonly PerformanceMonitor _monitor;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;

            public TimingScope(PerformanceMonitor monitor, string operationName)
            {
                _monitor = monitor;
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _monitor.RecordTiming(_operationName, _stopwatch.Elapsed);
            }
        }

        private class PerformanceCounter
        {
            public long Count;
            public TimeSpan TotalTime;
            public TimeSpan MinTime;
            public TimeSpan MaxTime;
            public TimeSpan LastTime;
        }

        private class MovingAverage
        {
            private readonly double[] _samples;
            private readonly int _windowSize;
            private int _index;
            private int _count;
            private double _sum;

            public MovingAverage(int windowSize)
            {
                _windowSize = windowSize;
                _samples = new double[windowSize];
            }

            public void AddSample(double value)
            {
                if (_count < _windowSize)
                {
                    _samples[_index] = value;
                    _sum += value;
                    _count++;
                }
                else
                {
                    _sum -= _samples[_index];
                    _samples[_index] = value;
                    _sum += value;
                }

                _index = (_index + 1) % _windowSize;
            }

            public double Average => _count > 0 ? _sum / _count : 0;
        }
    }

    public class PerformanceMetrics
    {
        public string OperationName { get; set; } = "";
        public long Count { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MinTime { get; set; }
        public TimeSpan MaxTime { get; set; }
        public TimeSpan LastTime { get; set; }
        public TimeSpan MovingAverage { get; set; }
    }
}