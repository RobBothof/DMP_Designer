using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using System.Net.NetworkInformation;

public class idea001 : IGenerator {
    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800+22000, 380160+22000, 6000);

    public void Generate(int seed) {
        Data.lines.Clear();
        Data.dots.Clear();
        _rand = new Random(seed);

        // Data.lines[0].points = new Vector3[] {new Vector3(0,500000,10000),new Vector3(180000,500000,10000),new Vector3(180000,0,10000)};

        float rotationOffset = MathF.PI*0.37f;
        Vector3 P1 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.0f)));
        Vector3 P2 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 2000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.333f)));
        Vector3 P3 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.666f)));
        Vector3 P4 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 2000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 0.999f)));
        Vector3 P5 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 1.333f)));
        Vector3 P6 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 2000), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 1.666f)));
        Vector3 P7 = PaperCenter + Vector3.Transform(new Vector3(0, 60000+_rand.Next(30000), 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + MathF.PI * 1.999f)));

        // Data.lines[0].points = new Vector3[] {P1,P2,P3,P4,P5,P6,P7,P2};

        List<Vector3> Triangle = new List<Vector3>();
        float step = MathF.PI * 0.6f;
        // for (int i=0; i < 10; i++) {
        //     Vector3 P = PaperCenter + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((i/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + i * step)));
        //     Points.Add(P);
        //     Console.WriteLine(MathF.Cos((i/6f)*MathF.PI));
        // }

        Triangle.Add(PaperCenter + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((0/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + 0 * step))));
        Triangle.Add(PaperCenter + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((1/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + 1 * step))));
        Triangle.Add(PaperCenter + Vector3.Transform(new Vector3(0, 150000 + 100000 * MathF.Cos((9/5f)*MathF.PI),0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (rotationOffset + 9 * step))));
        Triangle.Add(Triangle[0]);

        List<Vector3> PointsSharp = new List<Vector3>();
        for (int k=0;k<Triangle.Count-1;k++){
            // PointsSharp.Add(new Vector3(Points[k].X,Points[k].Y,2000));
            PointsSharp.Add(Vector3.Lerp(Triangle[k],Triangle[k+1],0.005f));
            // PointsSharp.Add(Points[k+1]);h
            PointsSharp.Add(Vector3.Lerp(Triangle[k+1],Triangle[k],0.005f));
        }
        PointsSharp.Add(PointsSharp[0]);

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
        Vector3[] PointsArray = PointsSharp.ToArray();
        // PointsArray[0].Z=0;
        // PointsArray[3].Z=0;

        // PointsArray[0].Z=0;
        // PointsArray[3].Z=0;
        // PointsArray[6].Z=0;
        // PointsArray[9].Z=0;
        // PointsArray[PointsArray.Length-1].Z=0;

       for (int p=0; p<PointsArray.Length - 1; p++) {
            Data.lines.Insert(p,new Line());
            Data.lines[p].type=LineType.Straight;
            Data.lines[p].acceleration=Acceleration.Single;
            // Vector3 pc = Vector3.Lerp(PaperCenter,pt,(float)_rand.NextDouble()+0.5f);
            // Vector3 pt;
            // Vector3 pc;
            // if (p == 0 || p == 10) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.625f);
            //     pc = Vector3.Lerp(PaperCenter,pt,1.05f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z+2000);
            // } else if (p==9) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.375f);
            //     pc = Vector3.Lerp(PaperCenter,pt,1.05f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);
            // } else if (p ==7) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.725f);
            //     pc = Vector3.Lerp(PaperCenter,pt,1.05f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);
            // } else if (p==2) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.275f);
            //     pc = Vector3.Lerp(PaperCenter,pt,1.05f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);                
            // } else if (p ==1 || p ==4) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.25f);
            //     pc = Vector3.Lerp(PaperCenter,pt,0.95f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);
            // } else if (p==8 || p == 5) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.75f);
            //     pc = Vector3.Lerp(PaperCenter,pt,0.95f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z); 
            // } else if (p ==3) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.635f);
            //     pc = Vector3.Lerp(PaperCenter,pt,1.05f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);
            // } else if (p==6) {
            //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.365f);
            //     pc = Vector3.Lerp(PaperCenter,pt,1.05f);
            //     pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);                 
            // //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.45f);
            // //     pc = Vector3.Lerp(PaperCenter,pt,2.7666f);
            // // } else if (p==14) {
            // //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.41f);
            // //     pc = Vector3.Lerp(PaperCenter,pt,2.7666f);
            // // } else if (p==9) {
            // //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.4f);
            // //     pc = Vector3.Lerp(PaperCenter,pt,2.7666f);
            // // } else if (p==4) {
            // //     pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.41f);
            // //     pc = Vector3.Lerp(PaperCenter,pt,2.8666f);
            // } else
            // {
                // pt = Vector3.Lerp(PointsArray[p],PointsArray[p+1],0.5f);
                // pc = Vector3.Lerp(PaperCenter,pt,1.0f);
                // pc = new Vector3(pc.X,pc.Y,PaperCenter.Z);
            // }
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
