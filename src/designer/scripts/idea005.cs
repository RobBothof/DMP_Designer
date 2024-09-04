using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;

public class idea005 : IGenerator {
    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800, 380160, 0);

    public void Generate(int seed) {
        Data.lines.Clear();
        Data.dots.Clear();
        _rand = new Random(seed);

        // Data.lines[0].points = new Vector3[] {new Vector3(0,500000,10000),new Vector3(180000,500000,10000),new Vector3(180000,0,10000)};

        float rotationOffset = -0.7175f + MathF.PI;
        Vector3 P1 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.0f)));
        Vector3 P2 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 2000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.333f)));
        Vector3 P3 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.666f)));
        Vector3 P4 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 2000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.999f)));
        Vector3 P5 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 1.333f)));
        Vector3 P6 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 2000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 1.666f)));
        Vector3 P7 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 1.999f)));

        // Data.lines[0].points = new Vector3[] {P1,P2,P3,P4,P5,P6,P7,P2};

        List<Vector3> Points = new List<Vector3>();
        float step = MathF.PI * 0.6f;
        for (int i=0; i < 10; i++) {
            Vector3 P = PaperCenter + Vector3.Transform(new Vector3(0, 140000 + 200000 * MathF.Cos((i/2.5f)*MathF.PI) + 30000 * MathF.Tan(2+(i/5f)*MathF.PI), 5000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + i * step)));
            Points.Add(P);
            Console.WriteLine(MathF.Cos((i/6f)*MathF.PI));
        }
        Points.Add(Points[0]);

        Points.RemoveAt(3);
        Points.RemoveAt(7);

        // int splitdepth=4000;
        // int split = 0;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 2;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 4;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 6;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 8;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 10;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 12;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // split = 14;
        // Points.Insert(split+1,new Vector3(Points[split].X*0.5f+Points[split+1].X*0.5f,Points[split].Y*0.5f+Points[split+1].Y*0.5f,splitdepth));

        // Points[0]=new Vector3(Points[0].X,Points[0].Y,4500);
        // Points[7]=new Vector3(Points[7].X*0.75f,Points[7].Y*0.75f,5000);
        // Points[2]=new Vector3(Points[2].X*0.75f,Points[2].Y,5000);
        // Points[3]=new Vector3(Points[3].X*0.75f,Points[3].Y,5000);
        // Points[6]=new Vector3(Points[6].X*0.75f,Points[6].Y,5000);
        // Points[3]=new Vector3(Points[3].X,Points[3].Y,5500);
        // Points[4]=new Vector3(Points[4].X,Points[4].Y,4500);
        // Points[5]=new Vector3(Points[5].X,Points[5].Y,7000);
        // Points[6]=new Vector3(Points[6].X,Points[6].Y,4500);
        // Points[7]=new Vector3(Points[7].X,Points[7].Y,5500);
        // Points[8]=new Vector3(Points[8].X,Points[8].Y,4500);


        // Points.Add(Points[1]);
        // Points.Add(Points[2]);

        // Points[0]=new Vector3(Points[0].X,Points[0].Y,0);
        // Points[Points.Count-1]=new Vector3(Points[Points.Count-1].X,Points[Points.Count-1].Y,0);

        Vector3[] PointsArray = Points.ToArray();


            // Data.lines.Insert(0,new Line());
            // Data.lines[0].type=LineType.Straight;
            // Data.lines[0].acceleration=Acceleration.Single;
            // Data.lines[0].points = new Vector3[] {PointsArray[0],PointsArray[1]};

            // Data.lines.Insert(1,new Line());
            // Data.lines[1].type=LineType.Straight;
            // Data.lines[1].acceleration=Acceleration.Single;
            // Data.lines[1].points = new Vector3[] {new Vector3(PointsArray[1].X,PointsArray[1].Y,2500),PointsArray[2]};

        for (int p=0; p<PointsArray.Length - 1; p++) {
            Data.lines.Insert(p,new Line());
            Data.lines[p].type=LineType.Straight;
            Data.lines[p].acceleration=Acceleration.Single;
            Data.lines[p].points = new Vector3[] {PointsArray[p],PointsArray[p+1]};
        }



        // for (int i=2; i < 30; i++) {
        //     // float xStart = _rand.NextSingle()*  940000f+40000;
        //     // float xControl = _rand.NextSingle()*940000f+40000;
        //     // float xEnd = _rand.NextSingle()*940000f+40000;
        //     // float yStart = _rand.NextSingle()*1450000f+50000;
        //     // float yControl = _rand.NextSingle()*1450000f+50000;
        //     // float yEnd = _rand.NextSingle()*1450000f+50000;
        //     // float zStart = _rand.NextSingle()*40000f;
        //     // float zControl = _rand.NextSingle()*40000f;
        //     // float zEnd = _rand.NextSingle()*40000f;

        //     float xStart = _rand.NextSingle()*  540000f+240000;
        //     float xControl = _rand.NextSingle()*540000f+240000;
        //     float xEnd = _rand.NextSingle()*540000f+240000;
        //     float yStart = _rand.NextSingle()*650000f+450000;
        //     float yControl = _rand.NextSingle()*650000f+450000;
        //     float yEnd = _rand.NextSingle()*650000f+450000;
        //     float zStart = _rand.NextSingle()*40000f;
        //     float zControl = _rand.NextSingle()*40000f;
        //     float zEnd = _rand.NextSingle()*40000f;

        //     Line l = new Line();
        //     l.type=LineType.QuadraticBezier;
        //     l.acceleration=Acceleration.Single;
        //     l.points = new Vector3[] {new Vector3(xStart,yStart,zStart), new Vector3(xControl,yControl,zControl) , new Vector3(xEnd,yEnd,zEnd)};
        //     Data.lines.Add(l);
        // }
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
