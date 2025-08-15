using System;
using System.Collections.Generic;

namespace GaymController.Shared {
    public enum CurveMode { Expo, Bezier, Cubic, Custom }
    public sealed class CurveLutBuilder {
        public CurveMode Mode { get; set; } = CurveMode.Expo;
        public double Expo { get; set; } = 0.35;
        public double Gain { get; set; } = 1.0;
        public double BezierY0 { get; set; } = 0;
        public double BezierY1 { get; set; } = 0;
        public double BezierY2 { get; set; } = 1;
        public double BezierY3 { get; set; } = 1;
        public double CubicA { get; set; } = 0;
        public double CubicB { get; set; } = 0;
        public double CubicC { get; set; } = 1;
        public double CubicD { get; set; } = 0;
        public List<(double X,double Y)> CustomPoints { get; } = new() { (-1,-1), (1,1) };
        public double Sample(double x){
            switch(Mode){
                case CurveMode.Expo:
                    {
                        var s=Math.Sign(x);
                        var y=s*Math.Pow(Math.Abs(x),1.0-Expo)*Gain;
                        return Math.Clamp(y,-1.0,1.0);
                    }
                case CurveMode.Bezier:
                    {
                        var t=(x+1)/2.0; var u=1-t;
                        var y=u*u*u*BezierY0 + 3*u*u*t*BezierY1 + 3*u*t*t*BezierY2 + t*t*t*BezierY3;
                        return y*2-1;
                    }
                case CurveMode.Cubic:
                    {
                        var y=((CubicA*x + CubicB)*x + CubicC)*x + CubicD;
                        return Math.Clamp(y,-1.0,1.0);
                    }
                case CurveMode.Custom:
                    {
                        if(CustomPoints.Count<2) return x;
                        for(int i=0;i<CustomPoints.Count-1;i++){
                            var p0=CustomPoints[i]; var p1=CustomPoints[i+1];
                            if(x>=p0.X && x<=p1.X){
                                var t=(x-p0.X)/(p1.X-p0.X);
                                return p0.Y + t*(p1.Y-p0.Y);
                            }
                        }
                        return x;
                    }
                default: return x;
            }
        }
        public byte[] ExportLut(){
            var lut=new byte[256];
            for(int i=0;i<256;i++){
                double x=i/255.0*2-1;
                double y=Sample(x);
                var v=(int)Math.Round((y+1)/2*255);
                if(v<0)v=0; if(v>255)v=255;
                lut[i]=(byte)v;
            }
            return lut;
        }
    }
}
