using System;
using LegacyMouseAim.Legacy;

int iterations = 600_000; // 10 minutes @1kHz
if (args.Length > 0 && int.TryParse(args[0], out var iters))
{
    iterations = iters;
}

var translator = new LegacyMouseAimTranslator();
var rand = new Random(0);
for (int i = 0; i < iterations; i++)
{
    int dx = rand.Next(-5, 6);
    int dy = rand.Next(-5, 6);
    translator.Translate(dx, dy);
}

Console.WriteLine($"Processed {iterations} iterations using LegacyMouseAim");
