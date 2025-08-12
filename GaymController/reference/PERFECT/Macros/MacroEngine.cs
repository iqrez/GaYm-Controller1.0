using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace WootMouseRemap
{
    public sealed class MacroEngine : IDisposable
    {
        private readonly Xbox360ControllerWrapper _pad;
        private readonly ConcurrentDictionary<Xbox360Button, CancellationTokenSource> _active = new();
        private readonly object _padLock = new();

        public MacroEngine(Xbox360ControllerWrapper pad) => _pad = pad;

        // Start a rapid macro with 50% duty cycle (press half, release half)
        public void StartRapid(Xbox360Button button, double rateHz, int burst = 0)
            => StartRapid(button, rateHz, 0.5, burst);

        // Start a rapid macro with custom duty cycle [0.05..0.95]
        public void StartRapid(Xbox360Button button, double rateHz, double duty, int burst = 0)
        {
            StopRapid(button);

            // Sanity clamps
            if (!double.IsFinite(rateHz) || rateHz <= 0) rateHz = 1;
            rateHz = Math.Clamp(rateHz, 1.0, 100.0); // 1..100 Hz practical
            duty = Math.Clamp(duty, 0.05, 0.95);

            int intervalMs = (int)Math.Max(2, Math.Round(1000.0 / rateHz));
            int pressMs = (int)Math.Max(1, Math.Round(intervalMs * duty));
            int releaseMs = Math.Max(1, intervalMs - pressMs);

            var cts = new CancellationTokenSource();
            _active[button] = cts;
            var token = cts.Token;

            Task.Run(async () =>
            {
                int fired = 0;
                try
                {
                    while (!token.IsCancellationRequested && (burst <= 0 || fired < burst))
                    {
                        token.ThrowIfCancellationRequested();
                        lock (_padLock) { _pad.SetButton(button, true); _pad.Submit(); }
                        await DelaySafe(pressMs, token).ConfigureAwait(false);

                        token.ThrowIfCancellationRequested();
                        lock (_padLock) { _pad.SetButton(button, false); _pad.Submit(); }
                        await DelaySafe(releaseMs, token).ConfigureAwait(false);

                        fired++;
                    }
                }
                catch (OperationCanceledException) { /* expected */ }
                catch { /* ignore */ }
                finally
                {
                    // Ensure button is released on exit and remove from active
                    lock (_padLock) { _pad.SetButton(button, false); _pad.Submit(); }
                    _active.TryRemove(button, out _);
                }
            }, token);
        }

        private static Task DelaySafe(int milliseconds, CancellationToken token)
        {
            if (milliseconds <= 0) return Task.CompletedTask;
            return Task.Delay(milliseconds, token);
        }

        public void StopRapid(Xbox360Button button)
        {
            if (_active.TryRemove(button, out var cts))
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            lock (_padLock) { _pad.SetButton(button, false); _pad.Submit(); }
        }

        public void StopAll()
        {
            var snapshot = _active.ToArray();
            foreach (var kv in snapshot)
            {
                try { kv.Value.Cancel(); kv.Value.Dispose(); } catch { }
                lock (_padLock) { _pad.SetButton(kv.Key, false); }
            }
            _active.Clear();
            lock (_padLock) { _pad.Submit(); }
        }

        public bool IsActive(Xbox360Button button) => _active.ContainsKey(button);

        public void Dispose()
        {
            foreach (var kv in _active) { try { kv.Value.Cancel(); kv.Value.Dispose(); } catch { } }
            _active.Clear();
        }
    }
}
