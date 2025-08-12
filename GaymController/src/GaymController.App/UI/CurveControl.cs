using System;
using System.Drawing;
using System.Windows.Forms;
namespace GaymController.App.UI {
    public class CurveControl : Control {
        public double Expo { get; set; } = 0.35;
        public double Gain { get; set; } = 1.0;
        double Sample(double x){ var s=Math.Sign(x); var y=s*Math.Pow(Math.Abs(x),1.0-Expo)*Gain; return Math.Max(-1,Math.Min(1,y)); }
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e); var g=e.Graphics; var w=Width; var h=Height;
            g.Clear(Color.Black); using var pen=new Pen(Color.White,2);
            g.DrawLine(pen,0,h/2,w,h/2); g.DrawLine(pen,w/2,0,w/2,h);
            var prev=new PointF(0,h/2);
            for(int i=0;i<w;i++){ double x=(i/(double)(w-1))*2-1; double y=Sample(x);
                float yy=(float)((1-(y+1)/2)*h); var p=new PointF(i,yy); g.DrawLine(pen,prev,p); prev=p; }
        }
    }
}
