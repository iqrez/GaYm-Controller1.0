using System;
using System.Drawing;
using System.Windows.Forms;
using GaymController.Shared;

namespace GaymController.App.UI {
    public sealed class CurveEditor : Control {
        public CurveLutBuilder Curve { get; } = new();
        readonly Timer _timer;
        public CurveEditor(){
            DoubleBuffered = true;
            _timer = new Timer { Interval = 16 };
            _timer.Tick += (s,e)=>Invalidate();
            _timer.Start();
        }
        protected override void Dispose(bool disposing){ if(disposing) _timer.Dispose(); base.Dispose(disposing); }
        public byte[] ExportLut() => Curve.ExportLut();
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e); var g=e.Graphics; var w=Width; var h=Height;
            g.Clear(Color.Black);
            using var pen=new Pen(Color.White,2);
            g.DrawLine(pen,0,h/2,w,h/2); g.DrawLine(pen,w/2,0,w/2,h);
            var prev=new PointF(0,h/2);
            for(int i=0;i<w;i++){
                double x=(i/(double)(w-1))*2-1; double y=Curve.Sample(x);
                float yy=(float)((1-(y+1)/2)*h); var p=new PointF(i,yy);
                g.DrawLine(pen,prev,p); prev=p;
            }
        }
    }
}
