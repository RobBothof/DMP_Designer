using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using MathNet.Numerics;

public class Lines2 : IGenerator
{
    public static Random _rand;

    MathNet.Numerics.Distributions.Normal gaussian;
    Line l;
    float maxX = 1000000;
    float minX = 30000;
    Vector3 vStart;
    Vector3 vEnd;
    float maxrow = 77;
    float YOFF = 0;

    public void Generate(int seed)
    {
        Data.lines.Clear();
        Data.dots.Clear();
        _rand = new Random(seed);

        float space = 25000 * MathF.Sqrt(MathF.Sin(0 * 0.015f) + 1.0f);
        for (int j = 1; j < 2; j++)
        {
            float y = 0;
            float startX = minX;
            float endX = 0;
            for (int i = 2; i < 3325; i++)
            {
                float z = _rand.NextSingle()*8000;
                gaussian = new MathNet.Numerics.Distributions.Normal(60000, 10000, _rand);

                float len = (float)gaussian.Sample();
                endX = startX + len;

                if (endX >= maxX)
                {
                    // float Zsplit = ((endX-maxX) / len) * zStart + (1-((endX-maxX) / len)) * zEnd;
                    float nextEndX = endX - maxX + minX;
                    endX = maxX;

                    vStart = new Vector3(startX, y, z);
                    vEnd = new Vector3(endX, y, z);

                    l = new Line();
                    l.type = LineType.Straight;
                    l.acceleration = Acceleration.Start;
                    l.points = new Vector3[] { vStart, vEnd };
                    if (startX < maxX && y < 1540000 && y > 20000)
                    {
                        Data.lines.Add(l);
                    }

                    z = _rand.NextSingle()*8000;
                    y += MathF.Abs(20000 - y * 0.01f) + _rand.NextSingle()*1f;
                    // y+=10000;
                    space = 245000 * MathF.Sqrt(MathF.Sin(i * 0.00015f) + 1.2f);
                    // space=10000*(MathF.Sin(i*0.015f)+1.2f);
                    // space=MathF.Abs(2000000-y)*0.06f;
                    endX = nextEndX;
                    startX = endX - len;
                    if (startX < minX) startX = minX;
                    vStart = new Vector3(startX, y, z);
                    vEnd = new Vector3(endX, y, z);

                    l = new Line();
                    l.type = LineType.Straight;
                    l.acceleration = Acceleration.Start;
                    l.points = new Vector3[] { vStart, vEnd };
                    if (startX < maxX && y < 1540000 && y > 20000)
                    {
                        Data.lines.Add(l);
                    }


                    startX = endX + space;
                }
                else
                {
                    vStart = new Vector3(startX, y, z);
                    vEnd = new Vector3(endX, y, z);

                    l = new Line();
                    l.type = LineType.Straight;
                    l.acceleration = Acceleration.Start;
                    l.points = new Vector3[] { vStart, vEnd };
                    if (startX < maxX && y < 1540000 && y > 20000)
                    {
                        Data.lines.Add(l);
                    }

                    startX = endX + space;

                }
            }
        }
        /*
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
        */
    }
}