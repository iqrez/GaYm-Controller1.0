using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using GaymController.Shared.Mapping;

namespace GaymController.Wooting {
    public sealed class RawHidProvider : IWootingProvider {
        private readonly Func<Stream> _opener;
        private Stream _stream;
        private readonly (int Offset, string Key)[] _map;
        private readonly Thread _thread;
        private volatile bool _running;
        private readonly byte[] _buf;
        private readonly double[] _last;

        public event EventHandler<InputEvent>? OnKeyAnalog;

        public RawHidProvider(Func<Stream> opener, IDictionary<int,string> mapping, int reportSize = 64){
            _opener = opener;
            _stream = opener();
            _map = mapping.Select(kv => (kv.Key, kv.Value)).ToArray();
            _buf = new byte[reportSize];
            _last = new double[_map.Length];
            _thread = new Thread(ReadLoop){IsBackground=true};
        }

        public static RawHidProvider FromMapping(Func<Stream> opener, string mappingPath, int reportSize = 64){
            var json = File.ReadAllText(mappingPath);
            var map = JsonSerializer.Deserialize<Dictionary<int,string>>(json) ?? new();
            return new RawHidProvider(opener, map, reportSize);
        }

        public void Start(){
            if(_running) return;
            _running = true;
            _thread.Start();
        }

        public void Stop(){
            if(!_running) return;
            _running = false;
            try{ _stream.Dispose(); }catch{}
            _thread.Join();
        }

        private void ReadLoop(){
            while(_running){
                try{
                    var read = 0;
                    while(read < _buf.Length){
                        var n = _stream.Read(_buf, read, _buf.Length - read);
                        if(n == 0) throw new IOException();
                        read += n;
                    }
                } catch {
                    if(!_running) break;
                    try{ _stream.Dispose(); }catch{}
                    Thread.Sleep(100);
                    try{ _stream = _opener(); } catch { Thread.Sleep(1000); }
                    continue;
                }

                var ts = Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency;
                for(var i=0;i<_map.Length;i++){
                    var (ofs,key) = _map[i];
                    var val = _buf[ofs] / 255.0;
                    if(Math.Abs(val - _last[i]) > 0.0001){
                        _last[i] = val;
                        OnKeyAnalog?.Invoke(this, new InputEvent(key, val, ts));
                    }
                }
            }
        }

        public void Dispose(){ Stop(); }
    }
}
