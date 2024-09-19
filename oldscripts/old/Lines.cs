using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using MathNet.Numerics;

public class Lines : IGenerator
{
    public static Random _rand;

    MathNet.Numerics.Distributions.Normal gaussian;
    Line l;
    float maxX = 1000000;
    float minX = 30000;
    Vector3 vStart;
    Vector3 vEnd;
    float maxrow = 77;
    float YOFF=0;

    public void Generate(int seed)
    {
        Data.lines.Clear();
        Data.dots.Clear();
        _rand = new Random(seed);

        YOFF = _rand.NextSingle()*5000;
        gaussian = new MathNet.Numerics.Distributions.Normal(907000, 5000,_rand);
        float space = 60000;
        float row = 2;
        float startX = minX;
        float endX = 0;
        float zEnd = 40000 * _rand.NextSingle();
        float zStart = 40000 * _rand.NextSingle();

        for (int i = 0; i < 320; i++)
        {
            if (i > 0)
            {
                startX += space;
            }
            float len = (float)gaussian.Sample();
            endX = startX + len;

            zStart = zEnd;
            zEnd = 40000*_rand.NextSingle();

            if (endX >= maxX)
            {
                float Zsplit = ((endX-maxX) / len) * zStart + (1-((endX-maxX) / len)) * zEnd;
                float nextEndX = endX-maxX + minX;
                endX = maxX;


                vStart = new Vector3(startX, row * 20000+YOFF, zStart);
                vEnd = new Vector3(endX, row * 20000+YOFF, Zsplit);

                l = new Line();
                l.type = LineType.Straight;
                l.acceleration = Acceleration.Start;
                l.points = new Vector3[] { vStart, vEnd };
                
                if (row < maxrow) Data.lines.Add(l);

                startX = minX;
                endX = nextEndX;
                row++;

                YOFF = _rand.NextSingle()*1000;

                vStart = new Vector3(startX, row * 20000+YOFF, Zsplit);
                vEnd = new Vector3(endX, row * 20000+YOFF, zEnd);
                startX = endX;

                l = new Line();
                l.type = LineType.Straight;
                l.acceleration = Acceleration.Start;
                l.points = new Vector3[] { vStart, vEnd };
                if (row < maxrow) Data.lines.Add(l);
            } else {
                vStart = new Vector3(startX, row * 20000+YOFF, zStart);
                vEnd = new Vector3(endX, row * 20000+YOFF, zEnd);
                startX = endX;

                l = new Line();
                l.type = LineType.Straight;
                l.acceleration = Acceleration.Start;
                l.points = new Vector3[] { vStart, vEnd };
                if (i==0) Data.lines.Add(l);                
                if (_rand.NextSingle() > 0.5f) 
                {
                // if (row < maxrow) Data.lines.Add(l);                
                }
            }



        }
    }
}