using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;

public class Circles : IGenerator {
    public static Random _rand;

    public void Generate(int seed) {
        Data.lines.Clear();
        Data.dots.Clear();

        _rand = new Random(seed);
        float xmax=950000f;
        float ymax=1450000f;
        float cxmax=0f;
        float cymax=0f;
        Vector3 center2 = new Vector3(550000,750000,0);
        float d1 = 250000f;


        for (int i=2; i<52;i++) {
            center2 = new Vector3(_rand.NextSingle()*xmax+50000,_rand.NextSingle()*ymax+50000,_rand.NextSingle()*65000);
            
            if (center2.X > xmax/2f) {
                cxmax = xmax - center2.X;
            } else {
                cxmax=center2.X;
            }

            if (center2.Y > ymax/2f) {
                cymax = ymax - center2.Y;
            } else {
                cymax=center2.Y;
            }

            d1 = MathF.Min(cxmax,cymax)*(0.4f+_rand.NextSingle()*0.4f);

            float d2 = d1 * 1.084f;
            Vector3 P1  = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.0f)));
            Vector3 P2  = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.125f)));
            Vector3 P3  = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.25f)));
            Vector3 P4  = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.375f)));
            Vector3 P5  = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.5f)));
            Vector3 P6  = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.625f)));
            Vector3 P7  = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.75f)));
            Vector3 P8  = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*0.875f)));
            Vector3 P9  = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1f)));
            Vector3 P10 = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.125f)));
            Vector3 P11 = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.25f)));
            Vector3 P12 = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.375f)));
            Vector3 P13 = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.5f)));
            Vector3 P14 = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.625f)));
            Vector3 P15 = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.75f)));
            Vector3 P16 = center2+Vector3.Transform(new Vector3(0,d2,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*1.875f)));
            Vector3 P17 = center2+Vector3.Transform(new Vector3(0,d1,0),Quaternion.CreateFromAxisAngle(Vector3.UnitZ,(MathF.PI*2f)));
            
            Line l;
            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Start;
            l.points = new Vector3[] {P1,P2,P3};
            Data.lines.Add(l);

            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Continue;
            l.points = new Vector3[] {P3,P4,P5};
            Data.lines.Add(l);

            
            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Continue;
            l.points = new Vector3[] {P5,P6,P7};
            Data.lines.Add(l);

            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Continue;
            l.points = new Vector3[] {P7,P8,P9};
            Data.lines.Add(l);

            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Continue;
            l.points = new Vector3[] {P9,P10,P11};
            Data.lines.Add(l);        

            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Continue;
            l.points = new Vector3[] {P11,P12,P13};
            Data.lines.Add(l);

            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Continue;
            l.points = new Vector3[] {P13,P14,P15};
            Data.lines.Add(l);      

            l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Stop;
            l.points = new Vector3[] {P15,P16,P17};
            Data.lines.Add(l); 
            
        }
        /*
        _rand = new Random(seed);

        for (int i=2; i < 30; i++) {
            // float xStart = _rand.NextSingle()*  940000f+40000;
            // float xControl = _rand.NextSingle()*940000f+40000;
            // float xEnd = _rand.NextSingle()*940000f+40000;
            // float yStart = _rand.NextSingle()*1450000f+50000;
            // float yControl = _rand.NextSingle()*1450000f+50000;
            // float yEnd = _rand.NextSingle()*1450000f+50000;
            // float zStart = _rand.NextSingle()*40000f;
            // float zControl = _rand.NextSingle()*40000f;
            // float zEnd = _rand.NextSingle()*40000f;

            float xStart = _rand.NextSingle()*  540000f+240000;
            float xControl = _rand.NextSingle()*540000f+240000;
            float xEnd = _rand.NextSingle()*540000f+240000;
            float yStart = _rand.NextSingle()*650000f+450000;
            float yControl = _rand.NextSingle()*650000f+450000;
            float yEnd = _rand.NextSingle()*650000f+450000;
            float zStart = _rand.NextSingle()*40000f;
            float zControl = _rand.NextSingle()*40000f;
            float zEnd = _rand.NextSingle()*40000f;

            Line l = new Line();
            l.type=LineType.QuadraticBezier;
            l.acceleration=Acceleration.Single;
            l.points = new Vector3[] {new Vector3(xStart,yStart,zStart), new Vector3(xControl,yControl,zControl) , new Vector3(xEnd,yEnd,zEnd)};
            Data.lines.Add(l);
        }
        */
    }
}
        /*
        Vector2 center1 = new Vector2(400000,1000000);
        Vector2 center2 = new Vector2(650000,750000);
        float linew = 2000;

        Data.lines.Insert(0,new Line());
        Data.lines[0].type=LineType.QuadraticBezier;
        Data.lines[0].acceleration=Acceleration.Single;
        Data.lines[0].points = new Vector3[] {new Vector3(0,0,10000), new Vector3(40000,500000,0), new Vector3(80000,0,12000)};

        Data.lines.Insert(1,new Line());
        Data.lines[1].type=LineType.QuadraticBezier;
        Data.lines[1].acceleration=Acceleration.Single;
        Data.lines[1].points = new Vector3[] {new Vector3(0,500000,10000), new Vector3(40000,1500000,0), new Vector3(180000,500000,12000)};
        */

        // Data.lines.Insert(1,new Line());
        // Data.lines[1].type=lineType.QuadraticBezier3D;
        // Data.lines[1].points = new Vector3[] {new Vector3(center1.X,center1.Y+200000,0), new Vector3(center1.X+200000,center1.Y+200000,0), new Vector3(center1.X+200000,center1.Y,10000)};


        // Data.lines.Insert(2,new Line());
        // Data.lines[2].type=lineType.QuadraticBezier3D;
        // Data.lines[2].points = new Vector3[] {new Vector3(center1.X+200000,center1.Y,linew), new Vector3(center1.X+200000,center1.Y-200000,linew), new Vector3(center1.X,center1.Y-200000,linew)};

        // Data.lines.Insert(3,new Line());
        // Data.lines[3].type=lineType.QuadraticBezier3D;
        // Data.lines[3].points = new Vector3[] {new Vector3(center1.X,center1.Y-200000,linew), new Vector3(center1.X-200000,center1.Y-200000,linew), new Vector3(center1.X-200000,center1.Y,linew)};

        // Data.lines.Insert(4,new Line());
        // Data.lines[4].type=lineType.QuadraticBezier3D;
        // Data.lines[4].points = new Vector3[] {new Vector3(center2.X-200000,center2.Y,20000), new Vector3(center2.X-200000,center2.Y+200000,linew), new Vector3(center2.X,center2.Y+200000,linew)};

        /*
        Data.lines.Insert(5,new Line());
        Data.lines[5].type=lineType.QuadraticBezier3D;
        Data.lines[5].points = new Vector3[] {new Vector3(center2.X,center2.Y+200000,20000), new Vector3(center2.X+200000,center2.Y+200000,linew), new Vector3(center2.X+200000,center2.Y,linew)};

        Data.lines.Insert(6,new Line());
        Data.lines[6].type=lineType.QuadraticBezier3D;
        Data.lines[6].points = new Vector3[] {new Vector3(center2.X+200000,center2.Y,20000), new Vector3(center2.X+200000,center2.Y-200000,linew), new Vector3(center2.X,center2.Y-200000,linew)};

        Data.lines.Insert(7,new Line());
        Data.lines[7].type=lineType.QuadraticBezier3D;
        Data.lines[7].points = new Vector3[] {new Vector3(center2.X,center2.Y-200000,20000), new Vector3(center2.X-200000,center2.Y-200000,linew), new Vector3(center2.X-200000,center2.Y,linew)};

*/
