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
        public bool Enabled{get;set;}=false;
        private double _v; private bool _armed;
        public AntiRecoilNode(string id){ Id=id; }
        public void OnEvent(InputEvent e){
            if(e.Source!="Fire") return;
            if(Enabled && e.Value>0.5){ _armed=true; _v=VerticalComp; }
            else { _armed=false; _v=0.0; }
        }
        public void OnTick(double dtMs){
            if(!Enabled || !_armed) return;
            var k = Math.Exp(-dtMs/Math.Max(1.0,DecayMs));
            _v*=k;
        }
        public double Output()=> (Enabled && _armed) ? -_v : 0.0;
    }
    public sealed class AutoSprintNode : INode {
        public string Id{get;} public double Threshold{get;set;}=0.5;
        private bool _enabled; private bool _toggleHeld; private double _move; private bool _out;
        public AutoSprintNode(string id){ Id=id; }
        public void OnEvent(InputEvent e){
            if(e.Source=="Toggle"){
                var pressed = e.Value>0.5;
                if(pressed && !_toggleHeld){ _enabled=!_enabled; }
                _toggleHeld = pressed;
            } else if(e.Source=="Move"){
                _move = e.Value;
            }
        }
        public void OnTick(double dtMs){ _out = _enabled && Math.Abs(_move) >= Threshold; }
        public bool Output()=>_out;
    }
}
