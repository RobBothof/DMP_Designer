using System;
using System.IO;
using System.Configuration;
using System.Numerics;
using Janus;

public class GenerateLines : IGenerator {
    public static Random _rand;

    public void Generate(int seed) {
        _rand = new Random(seed);
        int count=2;

        Data.lines.Clear();

        for (int ctr = 0; ctr < count; ctr++) {

            Data.lines.Insert(ctr, new Line());
            // Data.lines[ctr].type = lineType.Straight;
            Data.lines[ctr].type=lineType.Bezier;

            // int numpoints = Math.Max(4,(int) (_rand.NextSingle() * 4.0f));
            int numpoints = 4;
            Data.lines[ctr].lineData=new Vector2[numpoints];
            for (int i=0;i<numpoints;i++) {
                float ix = (float) _rand.NextDouble()*1000-500;
                float iy = (float) _rand.NextDouble()*1000-500;
                Data.lines[ctr].lineData[i] = new Vector2(ix,iy);
            }
        }

        count=1;

        for (int ctr = 0+count; ctr < count*2; ctr++) {
            Data.lines.Insert(ctr, new Line());
            Data.lines[ctr].type = lineType.Straight;
            // Data.lines[ctr].type=lineType.Bezier;

            // int numpoints = Math.Max(4,(int) (_rand.NextSingle() * 4.0f));
            // int numpoints = 4;
            int numpoints = 5 ;
            Data.lines[ctr].lineData=new Vector2[numpoints];
            for (int i=0;i<numpoints;i++) {
                float ix = (float) _rand.NextDouble()*1000-500;
                float iy = (float) _rand.NextDouble()*1000-500;
                Data.lines[ctr].lineData[i] = new Vector2(ix,iy);
            }
        }
        Console.WriteLine("hello from generator script 50");
        Data.DebugConsole.Add("hello from generator script");
        Data.DebugConsole.Add("--------------------------------------------------------");
    }
}
