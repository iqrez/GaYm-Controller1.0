using System;
using GaymController.Shared.Contracts;

namespace GaymController.Shared.Mapping {
    public sealed class AxisCurveNode : INode {
        public string Id { get; }
        public double Expo { get; set; } = 0.35;
        public double Gain { get; set; } = 1.0;
        private double _last;
        public AxisCurveNode(string id){ Id=id; }
        public void OnEvent(InputEvent e){ _last = e.Value; }
        public void OnTick(double dtMs) {}
        public double Output(){
            var x = Math.Clamp(_last, -1.0, 1.0);
            var s = Math.Sign(x);
            var y = s * Math.Pow(Math.Abs(x), 1.0 - Expo) * Gain;
            return Math.Clamp(y, -1.0, 1.0);
        }
    }
    public sealed class TurboNode : INode {
        public string Id{get;} public double RateHz{get;set;}=12.0; public double Duty{get;set;}=0.5;
        private double _phase; private bool _in; private bool _out;
        public TurboNode(string id){ Id=id; }
        public void OnEvent(InputEvent e){ _in = e.Value > 0.5; }
        public void OnTick(double dtMs){
            if(!_in){ _out=false; return; }
            _phase += dtMs * RateHz / 1000.0; _phase -= Math.Floor(_phase);
            _out = _phase < Duty;
        }
        public bool Output()=>_out;
    }
    public sealed class AntiRecoilNode : INode {
        public string Id{get;} public double VerticalComp{get;set;}=0.15; public double DecayMs{get;set;}=120.0;
        private double _v; private bool _armed;
        public AntiRecoilNode(string id){ Id=id; }
        public void OnEvent(InputEvent e){
            if(e.Source=="Fire" && e.Value>0.5){ _armed=true; _v=VerticalComp; }
            if(e.Source=="Fire" && e.Value<=0.5){ _armed=false; }
        }
        public void OnTick(double dtMs){ if(!_armed)return; var k = Math.Exp(-dtMs/Math.Max(1.0,DecayMs)); _v*=k; }
        public double Output()=> -_v;
    }
}
