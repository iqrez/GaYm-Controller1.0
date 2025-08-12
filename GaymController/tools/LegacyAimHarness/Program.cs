using System;
using System.Globalization;
using System.IO;
using LegacyMouseAimPlugin = LegacyMouseAim.Legacy.LegacyMouseAim;

namespace LegacyAimHarness {
    class Program {
        static void Main(){
            var plugin = new LegacyMouseAimPlugin();
            var baseline = new BaselineCurve();
            double totalErr=0, maxErr=0; int n=0;
            var path = Path.Combine(AppContext.BaseDirectory, "sample.csv");
            foreach(var line in File.ReadLines(path)){
                if(string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split(',');
                float dx=float.Parse(parts[0],CultureInfo.InvariantCulture);
                float dy=float.Parse(parts[1],CultureInfo.InvariantCulture);
                var exp=baseline.ToStick(dx,dy);
                var act=plugin.ToStick(dx,dy);
                double err=Math.Sqrt(Math.Pow(act.X-exp.X,2)+Math.Pow(act.Y-exp.Y,2))/32767.0;
                totalErr+=err; if(err>maxErr)maxErr=err; n++;
            }
            double avgPct= totalErr/Math.Max(1,n)*100.0;
            Console.WriteLine($"Samples:{n} AvgErr%:{avgPct:F4} MaxErr%:{maxErr*100:F4}");
            Console.WriteLine(avgPct<0.5?"PASS":"FAIL");
        }
    }
}
