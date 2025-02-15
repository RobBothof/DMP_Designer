using System;
using System.Numerics;
using System.Collections.Generic;
using Designer;
using MathNet.Numerics;
using RayTracer;

using System.Threading;
using System.Threading.Tasks;
using Veldrid;

using System.Linq;
using System.Net.Http.Headers;

public class DirectLighting5 : IGenerator
{
    private List<IShape> _shapes;
    private List<Light> _lights;
    private List<Cuboid> _cuboids;
    private List<Pyramid> _pyramids;

    Camera cam;

    Vector3 paper;
    Vector3 paperCenter;

    /// <summary>
    /// The size of the steps to take when drawing lines and checking for shadows and visibility.
    /// Use 16 for quality but takes time to render.
    /// Use 1024 for quick render.
    /// </summary>
    /// 

    // int stepSize = 1024;
    int stepSize = 16;

    int numShadowLines = 800;

    private RandomRobber _rng;

    public void Generate(int seed)
    {

        paper = new Vector3(Data.paperSize.X * Data.stepsPerMM, Data.paperSize.Y * Data.stepsPerMM, 0);
        paperCenter = paper * 0.5f;

        Data.lines.Clear();
        Data.dots.Clear();

        float distToFocus = 10f;
        // float aperture = 0.025f;
        float aperture = 0f;

        cam = Camera.Create(
            new Vector3(14, 11f, 9f),
            new Vector3(-6, 13f, 0),
            Vector3.UnitY,
            90f,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        Array.Fill(Data.depthMap, (ushort)(UInt16.MaxValue / 2));
        Array.Fill(Data.shadowMap, (ushort)(UInt16.MaxValue / 2));

        _rng = new RandomRobber((uint)seed);

        Material mat1 = Material.Lambertian(0.7f, 0.0f);
        Material mat2 = Material.ShadowMatte(1.5f, 0.0f);

        _lights = new List<Light>
        {
            new Light { Position = new Vector3(3000.0f, 4500.0f, -1000.0f), Intensity = 1.0f }
        };

        _cuboids = new List<Cuboid>();
        _pyramids = new List<Pyramid>();

        int id_counter = 0;
        _cuboids.Add(Cuboid.Create(new Vector3(0, 1, 0), new Vector3(40f, 1f, 40f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.0f), mat2, id_counter++));

        _pyramids.Add(Pyramid.Create(new Vector3(1,2,8), new Vector3(4, 11.0f, 2), new Vector3(0,0,1), new Vector3(0, 1 ,0), mat1, id_counter++));

        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                float a =0;
                Vector3 center = new Vector3(x * 8 - 8f, 2f, z * 8 - 8f);

                for (int y = 0; y < _rng.RandomFloat()*400 + 2; y++)
                {
                    a += _rng.RandomFloat()*0.01f - 0.005f; 
                    float h = _rng.RandomFloat() * 0.5f + 0.2f;
                    float w = _rng.RandomFloat() * 0.2f + 3f;
                    float d = _rng.RandomFloat() * 0.2f + 3f;
                    center += new Vector3(0, h*0.5f, 0);
                    _cuboids.Add(Cuboid.Create(center, new Vector3(w, h, d), Quaternion.CreateFromAxisAngle(Vector3.UnitY, a * 2 * MathF.PI), mat1, id_counter++));
                    center += new Vector3(_rng.RandomFloat()*0.1f, h*0.5f, _rng.RandomFloat()*0.1f);
                }
            }
        }

        /// END SCENE SETUP

        // The collection of shapes is used to trace visibility and shadows.
        _shapes = new List<IShape>();
        _shapes.AddRange(_cuboids.SelectMany(c => c.Quads).Cast<IShape>());
        _shapes.AddRange(_pyramids.SelectMany(p => p.Triangles).Cast<IShape>());


        foreach (Cuboid cub1 in _cuboids)
        {
            if (cub1.Material.Type != MaterialType.ShadowMatte)
            {
                for (int cq = 0; cq < cub1.Quads.Length; cq++)
                {
                    Vector3 CamQuadNormal = Vector3.Normalize(cub1.Quads[cq].Center - cam.Origin);
                    float CamQuadDot = Vector3.Dot(CamQuadNormal, cub1.Quads[cq].normal);
                    Vector3 LightQuadNormal = Vector3.Normalize(cub1.Quads[cq].Center - _lights[0].Position);
                    float LightQuadDot = Vector3.Dot(LightQuadNormal, cub1.Quads[cq].normal);
                    // if (CamQuadDot <= 0 && LightQuadDot <= 0)
                    if (CamQuadDot < 0)
                    {
                        AddLine(cub1.Quads[cq].Vertices[0], cub1.Quads[cq].Vertices[1], LineType.Straight, Acceleration.Single, cub1.Quads[cq].parentID);
                        AddLine(cub1.Quads[cq].Vertices[1], cub1.Quads[cq].Vertices[2], LineType.Straight, Acceleration.Single, cub1.Quads[cq].parentID);
                        AddLine(cub1.Quads[cq].Vertices[2], cub1.Quads[cq].Vertices[3], LineType.Straight, Acceleration.Single, cub1.Quads[cq].parentID);
                        AddLine(cub1.Quads[cq].Vertices[3], cub1.Quads[cq].Vertices[0], LineType.Straight, Acceleration.Single, cub1.Quads[cq].parentID);

                        /*

                        // get paper distance (height) between p0 and p3
                        Vector3 Pc0 = Camera.Project(cam, cub1.Quads[cq].Vertices[0]);
                        Vector3 Pc1 = Camera.Project(cam, cub1.Quads[cq].Vertices[1]);
                        Vector3 Pc2 = Camera.Project(cam, cub1.Quads[cq].Vertices[2]);
                        Vector3 Pc3 = Camera.Project(cam, cub1.Quads[cq].Vertices[3]);

                        // Calculate the angle between the camera direction and the quad normal
                        float angle = MathF.Acos(CamQuadDot) - MathF.PI * 0.5f; // Ensure the value is within the valid range for Acos
                        float largestDistance = MathF.Max(Vector3.Distance(Pc0, Pc3), Vector3.Distance(Pc2, Pc1));

                        float influence = MathF.Min(1.0f, -CamQuadDot * 8f);
                        // float influence = (1 + MathF.Exp(5 * CamQuadDot));                    
                        int maxLines = (int)(1500f * largestDistance * influence);

                        float diffuse = 0.0f;
                        if (LightQuadDot < 0)
                        {
                            diffuse += (-LightQuadDot) * _lights[0].Intensity;
                        }


                        maxLines = (int)(Math.Max(0f, 0.5f - diffuse) * maxLines);

                        if (maxLines < 3)
                        {
                            maxLines = 0;
                        }

                        if (maxLines > 0)
                        {
                            maxLines = Math.Min(Math.Max(maxLines, 4), 10);
                        }
                        float lineStep = 1f / maxLines;
                        for (int j = 0; j < maxLines; j++)
                        {
                            Vector3 Pq0 = Vector3.Lerp(cub1.Quads[cq].Vertices[0], cub1.Quads[cq].Vertices[3], j * lineStep);
                            Vector3 Pq1 = Vector3.Lerp(cub1.Quads[cq].Vertices[1], cub1.Quads[cq].Vertices[2], j * lineStep);
                            AddLine(Pq0, Pq1, LineType.Straight, Acceleration.Single, cub1.Quads[cq].parentID);
                        }
                        */
                    }
                }
            }
        }


        foreach (Pyramid p in _pyramids)
        {
            for (int pt = 0; pt < p.Triangles.Length; pt++)
            {
                Vector3 CamQuadNormal = Vector3.Normalize(p.Triangles[pt].Center - cam.Origin);
                float CamQuadDot = Vector3.Dot(CamQuadNormal, p.Triangles[pt].normal);
                Vector3 LightQuadNormal = Vector3.Normalize(p.Triangles[pt].Center - _lights[0].Position);
                float LightQuadDot = Vector3.Dot(LightQuadNormal, p.Triangles[pt].normal);
                if (CamQuadDot < 0)
                // if (CamQuadDot <= 0 && LightQuadDot <= 0)                
                {
                    AddLine(p.Triangles[pt].Vertices[0], p.Triangles[pt].Vertices[1], LineType.Straight, Acceleration.Single, p.Triangles[pt].parentID);
                    if (pt != 0 && pt != 1)
                    {
                        AddLine(p.Triangles[pt].Vertices[1], p.Triangles[pt].Vertices[2], LineType.Straight, Acceleration.Single, p.Triangles[pt].parentID);
                    }
                    AddLine(p.Triangles[pt].Vertices[2], p.Triangles[pt].Vertices[0], LineType.Straight, Acceleration.Single, p.Triangles[pt].parentID);
                }
            }
        }

        /*
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
                Data.depthMap[y * (int)Data.rasterWidth + x] = (ushort)(ushort.MaxValue - (ushort)(MathF.Max(MathF.Min(depth * 20000, ushort.MaxValue), 0.0f)));
            }

            Interlocked.Add(ref frameRays, rayCount);
        });

        Console.WriteLine($"Total rays shot: {frameRays}.");
        Data.DebugConsole.Add($"Total rays shot: {frameRays}.");
        */

        // Draw Shadow Lines
        Line sl;

        float yStep = paper.Y / numShadowLines;
        Parallel.For(-1, numShadowLines+2, s =>
        // for (int s=0; s<numLines; s++) 
        {
            Vector3 from = new Vector3(0, yStep * s, 0);
            Vector3 to = new Vector3(paper.X, yStep * s, 0);
            Vector3 P0 = from;
            Vector3 P1 = to;


            float xSteps = paper.X / stepSize;
            bool draw = false;

            // double pstepsize = (double)paper.X / (double)xSteps;

            Vector3 P = from;

            for (int i = 0; i < xSteps; i++)
            {
                P += new Vector3(stepSize, 0, 0);

                // if (P.X > 0 && P.X < paper.X && P.Y > 0 && P.Y < paper.Y)
                // {
                    Ray ray =  Ray.Create(cam.Origin, cam.LowerLeftCorner + (P.X * cam.Horizontal) / paper.X + (P.Y * cam.Vertical) / paper.Y - cam.Origin);

                    Vector3 hitPoint;

                    if (TraceShadow(ref ray, out hitPoint))
                    {
                        if (!draw)
                        {
                            draw = true;
                            P0 = Camera.Project(cam, hitPoint) * paper.X + paperCenter; 
                        }
                        P1 = Camera.Project(cam, hitPoint) * paper.X + paperCenter; 
                    }
                    else
                    {
                        if (draw)
                        {
                            // P1 = P;
                            sl = new Line();
                            sl.type = LineType.Straight;
                            sl.acceleration = Acceleration.Single;
                            sl.points = new Vector3[] { P0, P1 };
                            Data.lines.Add(sl);
                            draw = false;
                        }
                    }
                // }
                // else
                // {
                //     if (draw)
                //     {
                //         // P1 = P;
                //         sl = new Line();
                //         sl.type = LineType.Straight;
                //         sl.acceleration = Acceleration.Single;
                //         sl.points = new Vector3[] { P0, P1 };
                //         Data.lines.Add(sl);
                //         draw = false;
                //     }
                // }
            }

            if (draw)
            {
                // P1 = to;
                sl = new Line();
                sl.type = LineType.Straight;
                sl.acceleration = Acceleration.Single;
                sl.points = new Vector3[] { P0, P1 };
                Data.lines.Add(sl);
            }
        }
        );

    }

    // Check if a line is visible, split line if needed and project to paper
    public void AddLine(Vector3 from, Vector3 to, LineType type, Acceleration acceleration, int parentID)
    {
        Vector3 P0 = Camera.Project(cam, from) * paper.X + paperCenter;
        Vector3 P1 = Camera.Project(cam, to) * paper.X + paperCenter;

        //calculate steps
        Line l;
        int steps = (int)(Vector3.Distance(P0, P1) / stepSize);

        float step = 1f / steps;
        bool draw = false;

        for (int i = 0; i < steps; i++)
        {
            Vector3 P = Vector3.Lerp(from, to, i * step);
            Vector3 PCam = Camera.Project(cam, P) * (paper.X) + paperCenter;
            if (PCam.X > 0 && PCam.X < paper.X && PCam.Y > 0 && PCam.Y < paper.Y)
            {
                // if (TraceVisible(P, cam.Origin, parentID) && TraceVisible(P, _lights[0].Position, -2))
                if (TraceVisible(P,cam.Origin, parentID)) 
                {
                    if (!draw)
                    {
                        draw = true;
                        P0 = PCam;
                    }
                }
                else
                {
                    if (draw)
                    {
                        P1 = PCam;
                        l = new Line();
                        l.type = type;
                        l.acceleration = acceleration;
                        l.points = new Vector3[] { P0, P1 };
                        Data.lines.Add(l);
                        draw = false;
                    }
                }

            } else {
                if (draw)
                {
                    P1 = PCam;
                    l = new Line();
                    l.type = type;
                    l.acceleration = acceleration;
                    l.points = new Vector3[] { P0, P1 };
                    Data.lines.Add(l);
                    draw = false;
                }
            }
        }

        if (draw)
        {
            P1 = Camera.Project(cam, to) * paper.X + paperCenter;
            l = new Line();
            l.type = type;
            l.acceleration = acceleration;
            l.points = new Vector3[] { P0, P1 };
            Data.lines.Add(l);
        }
    }

    public bool TraceVisible(Vector3 from, Vector3 to, int parentID)
    {
        Ray r = Ray.Create(from, Vector3.Normalize(to - from));
        RayHit hit;
        hit.Position = new Vector3();
        hit.Normal = new Vector3();
        hit.T = 0;
        for (int j = 0; j < _shapes.Count; j++)
        {
            if (_shapes[j].parentID != parentID && parentID != -1)
            {
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
            float luminance = 0;

            // sample lights
            for (int i = 0; i < _lights.Count; i++)
            {
                Ray shadowRay = Ray.Create(hit.Position, Vector3.Normalize(_lights[i].Position - hit.Position));

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
                    // Vector3 SurfaceNormal = hit.Normal;
                    // Vector3 LightDirection = Vector3.Normalize(shadowRay.Direction);
                    // float d = MathF.Min(1.0f,MathF.Max(0.0f, Vector3.Dot(LightDirection, SurfaceNormal)));
                    // float cosTheta = d*d;
                    // luminance += _shapes[hitID].Material.Albedo * cosTheta;

                    luminance = 1.0f;
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

    public bool TraceShadow(ref Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.Zero;
        bool inShadow = false;
        RayHit hit;
        hit.Position = new Vector3();
        hit.Normal = new Vector3();
        hit.T = 0;
        float closest = 9999999f;
        bool hitAnything = false;
        // int hitID = 0;
        for (int i = 0; i < _shapes.Count; i++)
        {
            if (_shapes[i].Hit(ray, 0.001f, closest, out RayHit tempHit))
            {
                hitAnything = true;
                hit = tempHit;
                // hitID = i;
                closest = hit.T;
                hitPoint = hit.Position;
            }
        }

        if (hitAnything)
        {
            // sample lights
            for (int i = 0; i < _lights.Count; i++)
            {
                Vector3 lDir = Vector3.Normalize(_lights[i].Position - hit.Position);
                // if (Vector3.Dot(lDir, hit.Normal) < 0)
                // {
                //     inShadow = true;
                // }
                // else
                // {
                Ray shadowRay = Ray.Create(hit.Position, lDir);
                for (int j = 0; j < _shapes.Count; j++)
                {
                    if (_shapes[j].Hit(shadowRay, 0.001f, 9999999f, out RayHit tempHit))
                    {
                        inShadow = true;
                    }
                }
                // }
            }
        }
        return inShadow;
    }
}