using System;
using System.Numerics;
using System.Collections.Generic;
using Designer;
using MathNet.Numerics;
using RayTracer;

using System.Threading;
using System.Threading.Tasks;
using Veldrid;

public class DirectLighting3 : IGenerator
{
    private List<IShape> _shapes;
    private List<Light> _lights;
    private List<Cuboid> _cuboids;
    
    Camera cam;

    Vector3 paper;
    Vector3 paperCenter; 

    private RandomRobber _rng;

    public void Generate(int seed)
    {
        paper = new Vector3(Data.paperSize.X*Data.stepsPerMM,Data.paperSize.Y*Data.stepsPerMM,0);
        paperCenter = paper * 0.5f; 

        Data.lines.Clear();
        Data.dots.Clear();
        
        float distToFocus = 15;
        // float aperture = 0.025f;
        float aperture = 0f;

        cam = Camera.Create(
            new Vector3(0, 0, 47.0f),
            new Vector3(0, 0, 0),
            Vector3.UnitY,
            55,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        Array.Fill(Data.depthMap, (ushort)(UInt16.MaxValue / 2));
        Array.Fill(Data.shadowMap, (ushort)(UInt16.MaxValue / 2));

        _rng = new RandomRobber((uint)seed);

        Material mat1 = Material.Lambertian(0.7f, 0.0f);
        Material mat2 = Material.Lambertian(1.5f, 0.0f);

        _lights = new List<Light>
        {
            new Light { Position = new Vector3(13.0f, 16.0f, 60.5f), Intensity = 1.0f }
        };

        _cuboids = new List<Cuboid>();

        int id_counter=0;
        for (int x=0; x < 14; x++){
            for (int y=0; y < 21; y++){
                _cuboids.Add(Cuboid.Create(new Vector3(x*2-13f, y*2-20, 0), new Vector3( 1f,  1f,  1f), Quaternion.CreateFromAxisAngle(_rng.RandomUnitVector(),_rng.RandomFloat()*2*MathF.PI), mat1, id_counter++));
            }
        }

        _cuboids.Add(Cuboid.Create(new Vector3( 0,  0, -1.5f), new Vector3(80f, 80f,  1f), Quaternion.CreateFromAxisAngle(Vector3.UnitX,0.0f), mat2));
        // _cuboids.Add(Cuboid.Create(new Vector3( 4,  6.5f, 1.5f), new Vector3(8, 8,  8f), Quaternion.CreateFromAxisAngle(_rng.RandomUnitVector(),_rng.RandomFloat()*2*MathF.PI), mat1, id_counter++));
        // _cuboids.Add(Cuboid.Create(new Vector3( 0,  0, 1.5f), new Vector3(2, 5,  6f), Quaternion.CreateFromAxisAngle(_rng.RandomUnitVector(),_rng.RandomFloat()*2*MathF.PI), mat1, id_counter++));

        // _cuboids.Add(Cuboid.Create(new Vector3( -4,  -6.5f, 1.5f), new Vector3(8, 8,  8f), Quaternion.CreateFromAxisAngle(_rng.RandomUnitVector(),_rng.RandomFloat()*2*MathF.PI), mat1, id_counter++));

        _shapes = new List<IShape>();
        
        foreach (Cuboid cub1 in _cuboids){
            for (int cq = 0; cq < cub1.Quads.Length; cq++)
            {
                _shapes.Add(cub1.Quads[cq]);
            }
        }

        foreach (Cuboid cub1 in _cuboids){
            for (int cq = 0; cq < cub1.Quads.Length; cq++)
            {          
                Vector3 CamQuadNormal = Vector3.Normalize(cub1.Quads[cq].Center - cam.Origin);
                float CamQuadDot = Vector3.Dot(CamQuadNormal,cub1.Quads[cq].normal);
                if (CamQuadDot < 0) {
                    AddLine(cub1.Quads[cq].Vertices[0],cub1.Quads[cq].Vertices[1],LineType.Straight,Acceleration.Start,cub1.Quads[cq].parentID);
                    AddLine(cub1.Quads[cq].Vertices[1],cub1.Quads[cq].Vertices[2],LineType.Straight,Acceleration.Start,cub1.Quads[cq].parentID);
                    AddLine(cub1.Quads[cq].Vertices[2],cub1.Quads[cq].Vertices[3],LineType.Straight,Acceleration.Start,cub1.Quads[cq].parentID);
                    AddLine(cub1.Quads[cq].Vertices[3],cub1.Quads[cq].Vertices[0],LineType.Straight,Acceleration.Start,cub1.Quads[cq].parentID);    

                    // get paper distance (height) between p0 and p3
                    Vector3 Pc0 = Camera.Project(cam,cub1.Quads[cq].Vertices[0]);
                    Vector3 Pc1 = Camera.Project(cam,cub1.Quads[cq].Vertices[1]);
                    Vector3 Pc2 = Camera.Project(cam,cub1.Quads[cq].Vertices[2]);
                    Vector3 Pc3 = Camera.Project(cam,cub1.Quads[cq].Vertices[3]);


                    // Calculate the angle between the camera direction and the quad normal
                    float angle = MathF.Acos(CamQuadDot)-MathF.PI*0.5f; // Ensure the value is within the valid range for Acos
                    float largestDistance = MathF.Max(Vector3.Distance(Pc0,Pc3),Vector3.Distance(Pc2,Pc1));


                    float influence = MathF.Min(1.0f,-CamQuadDot*8f);
                    // float influence = (1 + MathF.Exp(5 * CamQuadDot));                    
                    int maxLines = (int) (1500f * largestDistance * influence);



                    float diffuse = 0.0f;

                    foreach(Light l in _lights){
                        Vector3 LightQuadNormal = Vector3.Normalize(cub1.Quads[cq].Center - l.Position);
                        float LightQuadDot = Vector3.Dot(CamQuadNormal,cub1.Quads[cq].normal);
                        if (LightQuadDot < 0) {
                            diffuse += (-LightQuadDot)*l.Intensity;
                        }
                    }

                    maxLines = (int) (Math.Max(0f, 0.5f - diffuse)*maxLines);
                    float lineStep = 1f / maxLines;
                    for (int j=0;j<maxLines;j++){
                        Vector3 Pq0 = Vector3.Lerp(cub1.Quads[cq].Vertices[0],cub1.Quads[cq].Vertices[3],j*lineStep);
                        Vector3 Pq1 = Vector3.Lerp(cub1.Quads[cq].Vertices[1],cub1.Quads[cq].Vertices[2],j*lineStep);
                        AddLine(Pq0,Pq1,LineType.Straight,Acceleration.Start,cub1.Quads[cq].parentID);
                    }

                }
            }
        }

        
        float invWidth = 1f / Data.rasterWidth;
        float invHeight = 1f / Data.rasterHeight;

        // Calculate the furthest point in the scene ? to set the max_depth

        
        long frameRays = 0;
        Parallel.For(0, Data.rasterHeight, y =>
        {
            int rayCount = 0;

            for (uint x = 0; x < Data.rasterWidth; x++)
            {
                float u = (x) * invWidth;  // blur / antialiasing
                float v = (y) * invHeight; // blur / antialiasing    

                float shadow = 0;
                float depth = 0;
                Ray ray = Camera.GetRay(cam, u, v, _rng.RandomInUnitDisk());
                float color = Trace(ref ray, ref rayCount, out depth);

                double gamma = 2.2f;
                color = (float)Math.Pow(color, 1.0 / gamma);
                Data.shadowMap[y * (int)Data.rasterWidth + x] = (ushort)(MathF.Max(MathF.Min(color, 1.0f), 0.0f) * ushort.MaxValue);
                Data.depthMap[y * (int)Data.rasterWidth + x] = (ushort)(ushort.MaxValue - (ushort)(MathF.Max(MathF.Min(depth*20000, ushort.MaxValue), 0.0f)));
            }

            Interlocked.Add(ref frameRays, rayCount);
        });

        Console.WriteLine($"Total rays shot: {frameRays}.");
        Data.DebugConsole.Add($"Total rays shot: {frameRays}.");
        

    }

    // Check if a line is visible, split line if needed and project to paper
    public void AddLine(Vector3 from, Vector3 to, LineType type, Acceleration acceleration, int parentID)
    {   
        Vector3 P0 = Camera.Project(cam,from) * paper.X + paperCenter;
        Vector3 P1 = Camera.Project(cam,to) * paper.X + paperCenter;

        //calculate steps
        Line l;
        int steps = (int) (Vector3.Distance(P0,P1)*0.01f);

        float stepSize = 1f / steps;

        bool draw = false;

        for (int i=0; i < steps; i++) {
            Vector3 P = Vector3.Lerp(from,to,i*stepSize);
            Vector3 Pcam = Camera.Project(cam,P)*paper.X + paperCenter;
            if (Pcam.X > 0 && Pcam.X < paper.X && Pcam.Y > 0 && Pcam.Y < paper.Y){
                if (TraceVisible(P,cam.Origin, parentID)){
                    if (!draw){
                        draw = true;
                        P0 = Pcam;
                    }
                } else {
                    if (draw){
                        P1 = Pcam;
                        l = new Line();
                        l.type = type;
                        l.acceleration = acceleration;
                        l.points = new Vector3[] { P0, P1 };
                        Data.lines.Add(l);
                        draw = false;
                    }
                }
            }
        }

        if (draw){
            P1 = Camera.Project(cam,to) * paper.X + paperCenter;
            l = new Line();
            l.type = type;
            l.acceleration = acceleration;
            l.points = new Vector3[] { P0, P1 };
            Data.lines.Add(l);
            draw = false;
        }
    }

    public bool TraceVisible(Vector3 from, Vector3 to, int parentID)
    {
        Ray r = Ray.Create(from, to - from);
        RayHit hit;
        hit.Position = new Vector3();
        hit.Normal = new Vector3();
        hit.T = 0;
        for (int j = 0; j < _shapes.Count; j++)
        {
            if (_shapes[j].parentID != parentID && parentID != -1) { 
                if (_shapes[j].Hit(r, 0.001f, 9999999f, out RayHit tempHit))
                {
                    return false;
                }
            }
        }
        return true;
    }


    public float Trace(ref Ray ray, ref int rayCount, out float depth)
    {
        rayCount += 1;
        RayHit hit;
        hit.Position = new Vector3();
        hit.Normal = new Vector3();
        hit.T = 0;
        float closest = 9999999f;
        bool hitAnything = false;
        int hitID = 0;
        for (int i = 0; i < _shapes.Count; i++)
        {
            if (_shapes[i].Hit(ray, 0.001f, closest, out RayHit tempHit))
            {
                hitAnything = true;
                hit = tempHit;
                hitID = i;
                closest = hit.T;
            }
        }

        depth = closest;

        if (hitAnything)
        {
            float luminance = _shapes[hitID].Material.Shadow;

            // sample lights
            for (int i = 0; i < _lights.Count; i++)
            {
                Ray shadowRay = Ray.Create(hit.Position, _lights[i].Position - hit.Position);

                RayHit shadowHit;
                shadowHit.Position = new Vector3();
                shadowHit.Normal = new Vector3();
                shadowHit.T = 0;
                bool inShadow = false;
                for (int j = 0; j < _shapes.Count; j++)
                {
                        if (_shapes[j].Hit(shadowRay, 0.001f, 9999999f, out RayHit tempHit))
                        {
                            inShadow = true;
                            shadowHit = tempHit;
                        }
                }

                if (!inShadow)
                {
                    Vector3 SurfaceNormal = hit.Normal;
                    Vector3 LightDirection = Vector3.Normalize(shadowRay.Direction);
                    float d = MathF.Min(1.0f,MathF.Max(0.0f, Vector3.Dot(LightDirection, SurfaceNormal)));
                    float cosTheta = d*d;

                    luminance += _shapes[hitID].Material.Albedo * cosTheta;
                } 
            }
            return luminance;
        }
        else
        {
            // SKY Color
            Vector3 unitDir = Vector3.Normalize(ray.Direction);
            float t = 0.5f * (unitDir.Y + 1f);
            return (1f - t) * 0.0f + t * 0.5f;
        }
    }
}