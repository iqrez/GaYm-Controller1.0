using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GaymController.Shared.Mapping;
using HidSharp;

namespace GaymController.Wooting {
    // Raw HID provider that maps incoming bytes to keys using a JSON mapping file.
    public sealed class RawHidProvider : IWootingProvider {
        private readonly int _vendorId;
        private readonly int _productId;
        private readonly int _reportSize;
        private readonly Dictionary<int,string> _map;
        private readonly Func<Stream>? _openOverride;
        private CancellationTokenSource? _cts;
        private Task? _task;

        public event EventHandler<InputEvent>? OnKeyAnalog;

        public RawHidProvider(int vendorId, int productId, int reportSize,
                              Dictionary<int,string> map, Func<Stream>? openOverride = null){
            _vendorId = vendorId; _productId = productId; _reportSize = reportSize;
            _map = map; _openOverride = openOverride;
        }

        public static RawHidProvider FromJson(string path, Func<Stream>? openOverride = null){
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            int vid = root.GetProperty("vendorId").GetInt32();
            int pid = root.GetProperty("productId").GetInt32();
            int size = root.GetProperty("reportSize").GetInt32();
            var map = new Dictionary<int,string>();
            foreach(var p in root.GetProperty("mapping").EnumerateObject())
                map[int.Parse(p.Name)] = p.Value.GetString() ?? string.Empty;
            return new RawHidProvider(vid,pid,size,map,openOverride);
        }

        private Stream OpenDevice(){
            if(_openOverride != null) return _openOverride();
            var dev = DeviceList.Local.GetHidDevices(_vendorId, _productId).FirstOrDefault();
            if(dev == null) throw new InvalidOperationException("Device not found");
            return dev.Open();
        }

        public void Start(){
            if(_cts != null) return;
            _cts = new CancellationTokenSource();
            _task = Task.Run(() => ReadLoop(_cts.Token));
        }

        private void ReadLoop(CancellationToken token){
            while(!token.IsCancellationRequested){
                Stream? s = null;
                try{
                    s = OpenDevice();
                    var buf = new byte[_reportSize];
                    while(!token.IsCancellationRequested){
                        int read = s.Read(buf,0,buf.Length);
                        if(read<=0) break;
                        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()*1000;
                        foreach(var kv in _map){
                            if(kv.Key < read){
                                double val = buf[kv.Key]/255.0;
                                OnKeyAnalog?.Invoke(this,new InputEvent(kv.Value,val,ts));
                            }
                        }
                    }
                } catch {
                    Thread.Sleep(1000);
                } finally {
                    s?.Dispose();
                }
            }
        }

        public void Stop(){
            if(_cts == null) return;
            _cts.Cancel();
            try{ _task?.Wait(); } catch{}
            _cts.Dispose(); _cts=null; _task=null;
        }

        public void Dispose(){ Stop(); }
    }
}
