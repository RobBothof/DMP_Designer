using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using System.Net.NetworkInformation;

public class idea011 : IGenerator {
    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800+30000, 380160+22000, 6000);

    public void Generate(int seed) {
        Data.lines.Clear();
        Data.dots.Clear();
        _rand = new Random(seed);

        // Data.lines[0].points = new Vector3[] {new Vector3(0,500000,10000),new Vector3(180000,500000,10000),new Vector3(180000,0,10000)};

        float rotationOffset = MathF.PI*0.37f;
        Vector3 TranslationOffset = new Vector3(0,0,0);
        List<Vector3> Triangle1 = new List<Vector3>();
        float step = MathF.PI * 0.6f;
        Triangle1.Add(PaperCenter + TranslationOffset + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((0/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + 0 * step))));
        Triangle1.Add(PaperCenter + TranslationOffset + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((1/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + 1 * step))));
        Triangle1.Add(PaperCenter + TranslationOffset + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((9/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + 9 * step))));
        Triangle1.Add(Triangle1[0]);

        Vector3[] Points;
        
        Points = Triangle1.ToArray();
        // for (int p=0; p<Points.Length - 1; p++) {
        {
            Vector3 p1 = Vector3.Lerp(Points[0],Points[1],0.025f);
            Vector3 p2 = Vector3.Lerp(Points[1],Points[0],0.025f);
            Vector3 p3 = Vector3.Lerp(Points[1],Points[2],0.025f);
            Vector3 p4 = Vector3.Lerp(Points[2],Points[1],0.025f);
            Vector3 p5 = Vector3.Lerp(Points[2],Points[0],0.025f);
            Vector3 p6 = Vector3.Lerp(Points[0],Points[2],0.025f);

            Line ln;
            
            ln = new Line();           
            ln.type = LineType.Straight;
            ln.acceleration = Acceleration.Single;
            ln.points = new Vector3[] { p1, p2 };           
            Data.lines.Add(ln);

            ln = new Line();           
            ln.type = LineType.QuadraticBezier;
            ln.acceleration = Acceleration.Single;
            ln.points = new Vector3[] { p2, Points[1], p3 };           
            Data.lines.Add(ln);

            ln = new Line();           
            ln.type = LineType.Straight;
            ln.acceleration = Acceleration.Single;
            ln.points = new Vector3[] { p3, p4 };           
            Data.lines.Add(ln);

            ln = new Line();           
            ln.type = LineType.QuadraticBezier;
            ln.acceleration = Acceleration.Single;
            ln.points = new Vector3[] { p4, Points[2], p5 };           
            Data.lines.Add(ln);

            ln = new Line();           
            ln.type = LineType.Straight;
            ln.acceleration = Acceleration.Single;
            ln.points = new Vector3[] { p5, p6 };           
            Data.lines.Add(ln);

            ln = new Line();           
            ln.type = LineType.QuadraticBezier;
            ln.acceleration = Acceleration.Single;
            ln.points = new Vector3[] { p6, Points[0], p1 };           
            Data.lines.Add(ln);

        }

        // }

        // List<Vector3> PointsSharp = new List<Vector3>();
        // for (int k=0;k<Triangle1.Count-1;k++){
        //     // PointsSharp.Add(new Vector3(Points[k].X,Points[k].Y,2000));
        //     PointsSharp.Add(Vector3.Lerp(Triangle1[k],Triangle1[k+1],0.05f));
        //     // PointsSharp.Add(Points[k+1]);h
        //     PointsSharp.Add(Vector3.Lerp(Triangle1[k+1],Triangle1[k],0.05f));
        // }
        // PointsSharp.Add(PointsSharp[0]);

        // Vector3 Pi01 = Vector3.Lerp(Points[0],Points[1],0.02f);
        // Vector3 Pi10 = Vector3.Lerp(Points[1],Points[0],0.02f);
        // Vector3 Pi12 = Vector3.Lerp(Points[1],Points[2],0.02f);
        // Vector3 Pi21 = Vector3.Lerp(Points[2],Points[1],0.02f);
        // Vector3 Pi23 = Vector3.Lerp(Points[2],Points[3],0.02f);
        // Vector3 Pi32 = Vector3.Lerp(Points[3],Points[2],0.02f);



        // Points.Insert(2,Pi21);
        // Points.Insert(2,Pi12);
        // Points.Insert(1,Pi10);
        // Points.Insert(1,Pi01);

        // Points[0] = new Vector3(Points[0].X,Points[0].Y,0);
        // Points.Add(Points[1]);

        // Points[0]=new Vector3(Points[0].X,Points[0].Y,0);

        // Points.Add(Points[1]);

        // int last = Points.Count-1;
        // Points[last] = new Vector3(Points[last].X,Points[last].Y,0);

        // Vector3[] PointsArray = Points.ToArray();
        // Vector3[] PointsArray = PointsSharp.ToArray();
        // PointsArray[0].Z=0;
        // PointsArray[3].Z=0;

        // PointsArray[0].Z=0;
        // PointsArray[3].Z=0;
        // PointsArray[6].Z=0;
        // PointsArray[9].Z=0;
        // PointsArray[PointsArray.Length-1].Z=0;




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
