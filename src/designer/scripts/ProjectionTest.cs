using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;

public class ProjectionTest : IGenerator
{
    public static Random _rand;

    private Matrix4x4 ProjectionMatrix(float near_plane, float far_plane, float fov_horiz, float fov_vert)
    {
        float h, w, Q;

        w = 1 / (float)Math.Tan(fov_horiz * 0.5); // 1/tan(x) == cot(x)
        h = 1 / (float)Math.Tan(fov_vert * 0.5); // 1/tan(x) == cot(x)
        Q = far_plane / (far_plane - near_plane);

        Matrix4x4 ret = new Matrix4x4();

        ret.M11 = w;
        ret.M22 = h;
        ret.M33 = Q;
        ret.M43 = -Q * near_plane;
        ret.M34 = 1;

        return ret;
    }

    public void Generate(int seed)
    {
        Data.lines.Clear();
        Data.dots.Clear();

        // Matrix4x4 projectionMatrix = ProjectionMatrix(-200000,2000000,1.5f,1.5f);
        Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(0.5f,1f,1f,100f);
        // Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspective(2,2,5,6);

        Line l;

        //depthbuf 20.000x30.000 = 600.000.000 * 4 = +-2.4Gb ( a bit less as there is 1024 bytes in a kB)

        float boxSize = 2000;
        Vector3 center1 = new Vector3(520000,750000,0);
        Vector3 center2 = new Vector3(5000,4000,-10000);

        Vector4 v4P1 = Vector4.Transform(new Vector4(center2.X-boxSize,center2.Y-boxSize,center2.Z,1),projectionMatrix);
        Vector4 v4P2 = Vector4.Transform(new Vector4(center2.X+boxSize,center2.Y-boxSize,center2.Z,1),projectionMatrix);
        Vector4 v4P3 = Vector4.Transform(new Vector4(center2.X-boxSize,center2.Y+boxSize,center2.Z,1),projectionMatrix);
        Vector4 v4P4 = Vector4.Transform(new Vector4(center2.X+boxSize,center2.Y+boxSize,center2.Z,1),projectionMatrix);
        Vector4 v4P5 = Vector4.Transform(new Vector4(center2.X-boxSize,center2.Y-boxSize,center2.Z-boxSize,1),projectionMatrix);
        Vector4 v4P6 = Vector4.Transform(new Vector4(center2.X+boxSize,center2.Y-boxSize,center2.Z-boxSize,1),projectionMatrix);
        Vector4 v4P7 = Vector4.Transform(new Vector4(center2.X-boxSize,center2.Y+boxSize,center2.Z-boxSize,1),projectionMatrix);
        Vector4 v4P8 = Vector4.Transform(new Vector4(center2.X+boxSize,center2.Y+boxSize,center2.Z-boxSize,1),projectionMatrix);

        Vector3 P1 = new Vector3(v4P1.X / v4P1.W ,v4P1.Y / v4P1.W, 0 )*100000+center1;
        Vector3 P2 = new Vector3(v4P2.X / v4P2.W ,v4P2.Y / v4P2.W, 0 )*100000+center1;
        Vector3 P3 = new Vector3(v4P3.X / v4P3.W ,v4P3.Y / v4P3.W, 0 )*100000+center1;
        Vector3 P4 = new Vector3(v4P4.X / v4P4.W ,v4P4.Y / v4P4.W, 0 )*100000+center1;
        Vector3 P5 = new Vector3(v4P5.X / v4P5.W ,v4P5.Y / v4P5.W, 0 )*100000+center1;
        Vector3 P6 = new Vector3(v4P6.X / v4P6.W ,v4P6.Y / v4P6.W, 0 )*100000+center1;
        Vector3 P7 = new Vector3(v4P7.X / v4P7.W ,v4P7.Y / v4P7.W, 0 )*100000+center1;
        Vector3 P8 = new Vector3(v4P8.X / v4P8.W ,v4P8.Y / v4P8.W, 0 )*100000+center1;
        
        Console.Write(P1.X);
        Console.Write(",");
        Console.Write(P1.Y);
        Console.Write(",");
        Console.WriteLine(P1.Z);

        Console.Write(P5.X);
        Console.Write(",");
        Console.Write(P5.Y);
        Console.Write(",");
        Console.WriteLine(P5.Z);


        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P1,P2 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P1,P3 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P2,P4 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P3,P4 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P5,P6 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P5,P7 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P6,P8 };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P7,P8 };
        Data.lines.Add(l);        

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P1,P5 };
        Data.lines.Add(l);    


        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P2,P6 };
        Data.lines.Add(l);      

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P3,P7 };
        Data.lines.Add(l);             


        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { P4,P8 };
        Data.lines.Add(l);      
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
