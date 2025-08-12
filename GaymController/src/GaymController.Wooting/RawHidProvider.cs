using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using GaymController.Shared.Mapping;

namespace GaymController.Wooting {
    /// <summary>
    /// Basic raw HID provider that replays analog values based on a
    /// per-device mapping file. The implementation is intentionally
    /// lightweight and avoids per-frame allocations; it merely simulates
    /// HID input to validate higher-level plumbing when real hardware or
    /// SDK is unavailable.
    /// </summary>
    public sealed class RawHidProvider : IWootingProvider {
        private readonly Dictionary<int, string> _mapping = new();
        private Thread? _worker;
        private bool _running;

        public event EventHandler<InputEvent>? OnKeyAnalog;

        public RawHidProvider(){
            var mapPath = Path.Combine(AppContext.BaseDirectory, "wooting-mapping.json");
            if (File.Exists(mapPath)){
                try {
                    var json = File.ReadAllText(mapPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<int,string>>(json);
                    if (dict != null) foreach (var kv in dict) _mapping[kv.Key] = kv.Value;
                } catch { /* ignore malformed mapping */ }
            }
        }

        public void Start(){
            if (_running) return;
            _running = true;
            _worker = new Thread(ReadLoop){ IsBackground = true };
            _worker.Start();
        }

        private void ReadLoop(){
            var rand = new Random();
            while (_running){
                Thread.Sleep(16); // ~60Hz
                foreach (var kv in _mapping){
                    var value = rand.NextDouble();
                    OnKeyAnalog?.Invoke(this,
                        new InputEvent(kv.Value, value,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()*1000));
                }
            }
        }

        public void Stop(){
            _running = false;
            _worker?.Join();
            _worker = null;
        }

        public void Dispose(){ Stop(); }
    }
}
