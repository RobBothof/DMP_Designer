using System;
using System.IO;
using System.Configuration;
using System.Numerics;
using Janus;

public class Shapes_1 : IGenerator {
    public static Random _rand;

    public void Generate(int seed) {
        _rand = new Random(seed);
        int count=100;

        Data.lines.Clear();
        Data.dots.Clear();

        for (int ctr = 0; ctr < count; ctr++) {
            Data.dots.Insert(ctr, new Dot());
            Data.dots[ctr].layer=0;
            Data.dots[ctr].size=6.0f;
            Data.dots[ctr].color=Veldrid.RgbaFloat.Red;

            float dx = (float) _rand.NextDouble()*1000-500;
            float dy = (float) _rand.NextDouble()*1000-500;
            Data.dots[ctr].position = new Vector2(dx,dy);
        }

        count=1;

        // Data.DebugConsole.Add("hello from generator script");
        // Data.DebugConsole.Add("--------------------------------------------------------");
    }
}
