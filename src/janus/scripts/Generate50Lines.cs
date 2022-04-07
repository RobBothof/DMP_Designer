using System;
using System.IO;
using System.Configuration;
using System.Numerics;
using Janus;

public class Generate50Lines : IGenerator {
    public static Random _rand;

    public void Generate(int seed) {
        _rand = new Random(seed);
        int count=150;

        Data.lines.Clear();

        for (int ctr = 0; ctr < count; ctr++) {

            Data.lines.Insert(ctr, new Line());
            Data.lines[ctr].type = lineType.Straight;

            int numpoints = Math.Max(2,(int) (_rand.NextSingle() * 4.0f));
            Data.lines[ctr].lineData=new Vector2[numpoints];
            for (int i=0;i<numpoints;i++) {
                float ix = (float) _rand.NextDouble()*1000-500;
                float iy = (float) _rand.NextDouble()*1000-500;
                Data.lines[ctr].lineData[i] = new Vector2(ix,iy);
            }

            Data.lines[ctr].type=0;
        }


        
        Console.WriteLine("hello from generator script 50");
        Data.DebugConsole.Add("hello from generator script");
        Data.DebugConsole.Add("--------------------------------------------------------");
    }
}
