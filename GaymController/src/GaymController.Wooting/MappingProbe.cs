using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GaymController.Shared.Mapping;

namespace GaymController.Wooting {
    /// <summary>
    /// Captures analog key events from a <see cref="IWootingProvider"/> to
    /// assist in building mapping files.  The probe records the maximum absolute
    /// value observed for each key source and can optionally persist the results
    /// to disk.
    /// </summary>
    public sealed class MappingProbe : IDisposable {
        private readonly IWootingProvider _provider;
        private readonly Dictionary<string, double> _captures = new();

        public MappingProbe(IWootingProvider provider) {
            _provider = provider;
            _provider.OnKeyAnalog += HandleEvent;
        }

        /// <summary>Gets the captured analog values keyed by source identifier.</summary>
        public IReadOnlyDictionary<string, double> Captured => _captures;

        /// <summary>Starts the underlying provider.</summary>
        public void Start() => _provider.Start();

        /// <summary>Stops the underlying provider.</summary>
        public void Stop() => _provider.Stop();

        /// <summary>Saves the captured data as pretty printed JSON.</summary>
        public void Save(string path) {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(_captures, options));
        }

        private void HandleEvent(object? sender, InputEvent e) {
            var v = Math.Abs(e.Value);
            if (_captures.TryGetValue(e.Source, out var existing)) {
                if (v > existing) _captures[e.Source] = v;
            } else {
                _captures[e.Source] = v;
            }
        }

        public void Dispose() {
            _provider.OnKeyAnalog -= HandleEvent;
            _provider.Dispose();
        }
    }
}
