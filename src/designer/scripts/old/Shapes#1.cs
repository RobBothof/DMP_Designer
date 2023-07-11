using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;

public class Shapes_1 : IGenerator {
    public static Random _rand;

    public void Generate(int seed) {
        _rand = new Random(seed);
        int count=9;

        Data.lines.Clear();
        Data.dots.Clear();

        for (int lineIndex=0;lineIndex<count;lineIndex++) {
            int dotIndex=0;
            Data.lines.Insert(lineIndex, new Line());
            Data.lines[lineIndex].type=LineType.Straight;
            List<Vector2> lineDots = new List<Vector2>();
            // Vector2 center = new Vector2((float) _rand.NextDouble()*600-300,(float) _rand.NextDouble()*600-300);
            Vector2 center = new Vector2((float)(lineIndex%3) * 500f - 500f,(float)(lineIndex/3) * 500f - 500f);
            int dotStart = dotIndex;
            for(float r=_rand.NextSingle()*30f;r<360f;r=r+(10f + _rand.NextSingle()*30f)) {
                Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ,r * (MathF.PI/180f));

                Data.dots.Insert(dotIndex, new Dot());
                Data.dots[dotIndex].layer=0;
                Data.dots[dotIndex].size=6.0f;
                Data.dots[dotIndex].color=Veldrid.RgbaFloat.Blue;
                Data.dots[dotIndex].position = Vector2.Transform(new Vector2(0,_rand.NextSingle()*200f+50f),rot) + center;
                lineDots.Add(Data.dots[dotIndex].position);
                dotIndex++;
            }
            lineDots.Add(Data.dots[dotStart].position);
            Data.lines[lineIndex].lineData=lineDots.ToArray();
        }
    }
}
