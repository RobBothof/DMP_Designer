using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;

public class Thick : IGenerator {
    public static Random _rand;

    public void Generate(int seed) {
        Data.lines.Clear();
        Data.dots.Clear();

        Data.lines.Insert(0,new Line());
        Data.lines[0].type=lineType.Quadratic3DBezier;
        // Data.lines[0].lineData = new Vector2[] {new Vector2(1000,1000), new Vector2(250000,1000), new Vector2(500000,1000)};
        Data.lines[0].points = new Vector3[] {new Vector3(0,50,20), new Vector3(50,0,50), new Vector3(100,25,20)};

        // Data.lines.Insert(0,new Line());
        // Data.lines[0].type=lineType.Straight3D;
        // Data.lines[0].lineData = new Vector2[] {new Vector2(1000,1000), new Vector2(250000,1000), new Vector2(500000,1000)};
        // Data.lines[0].points = new Vector3[] {new Vector3(0,0,0), new Vector3(200,100,20)};


        // for (int c=0;c<100;c++) {
        //     Program.AddNewQuadraticBezier(c);
        // }

        // Data.dots.Insert(0, new Dot());
        // Data.dots[0].size=10000.0f;
        // Data.dots[0].color=Veldrid.RgbaFloat.LightGrey;
        // Data.dots[0].position=new Vector2(1000,1000);

        // Data.dots.Insert(1, new Dot());
        // Data.dots[1].size=10000.0f;
        // Data.dots[1].color=Veldrid.RgbaFloat.LightGrey;
        // Data.dots[1].position=new Vector2(500000,500000);



        /*        
        _rand = new Random(seed);
        int count=15;
        float R2 = 1.25f; // radius of the wheel on motor

        int curveindex = count;

        for (int lineIndex=0;lineIndex<count;lineIndex++) {
            int dotIndex=0;

            List<Vector2> lineDots = new List<Vector2>();
            Vector2 center = new Vector2((float)(lineIndex%3) * (51200f / (R2*MathF.PI)) *25f + 15*(51200f / (R2*MathF.PI)),(float)(lineIndex/3) * 22.5f*(51200f / (R2*MathF.PI)) + 15f*(51200f / (R2*MathF.PI)));
            // int dotStart = 0;
            Vector2 startpos = new Vector2(0,0);
            for(float r=_rand.NextSingle()*20f;r<360f;r=r+(5f + _rand.NextSingle()*60f)) {
                Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ,r * (MathF.PI/180f));

                // Data.dots.Insert(dotIndex, new Dot());
                // Data.dots[dotIndex].layer=0;
                // Data.dots[dotIndex].size=5.0f;
                // Data.dots[dotIndex].color=Veldrid.RgbaFloat.LightGrey;
                // Data.dots[dotIndex].position = Vector2.Transform(new Vector2(0,_rand.NextSingle()*160f+100f),rot) + center;
                // lineDots.Add(Data.dots[dotIndex].position);

                if (dotIndex==0) {
                    startpos=Vector2.Transform(new Vector2(0,_rand.NextSingle()*100000f+30000f),rot) + center;
                    lineDots.Add(startpos);
                } else {
                    lineDots.Add(Vector2.Transform(new Vector2(0,_rand.NextSingle()*100000f+30000f),rot) + center);
                }
                dotIndex++;
            }
            lineDots.Add(startpos);

            //add one line connecting all dots
            // Line l = new Line();
            // l.type=lineType.Straight;
            // l.lineData = lineDots.ToArray();
            // Data.lines.Add(l);

            //add multiple quadriv bezier lines

            for (int c=0;c<lineDots.Count;c++) {
                int index0 = c;
                int index1 = (c+1)%(lineDots.Count-1);
                int index2 = (c+2)%(lineDots.Count-1);
                Vector2 A = (lineDots[index0] + lineDots[index1])/2.0f; 
                Vector2 B = (lineDots[index1]); 
                Vector2 C = (lineDots[index1] + lineDots[index2])/2.0f; 

                Line ql = new Line();
                ql.type=lineType.QuadraticBezier;
                ql.lineData = new Vector2[] {A,B,C};
                Data.lines.Add(ql);
            }




        }
        */
    }
}
