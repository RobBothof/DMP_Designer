using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using System.Diagnostics;
using MathNet.Numerics;
using System.Runtime.CompilerServices;
using RayTracer;
using System.Threading;
using System.Threading.Tasks;

public class Tracer2 : IGenerator
{
    public const uint NumSamples = 10;

    private Sphere[] _spheres;
    private Material[] _materials;
    private ulong _totalRays = 0;

    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800, 380160, 0);
    // public float shadeStep=0.15f;

    public Vector3 SpotLight = new Vector3(-40.0f, 10f, -5f);

    /*
    public Vector3 interpolate(Vector3 A, Vector3 B, float t)
    {
        return new Vector3(A.X * t + B.X * (1 - t), A.Y * t + B.Y * (1 - t), A.Z * t + B.Z * (1 - t));
    }

    public void rasterizeToDepthBuffer(Vector3[] points)
    {
        int minX = (int)Data.rasterWidth;
        int maxX = 0;
        int minY = (int)Data.rasterHeight;
        int maxY = 0;

        int p0x = (int)(points[0].X / Data.rasterPixelsPerStep + 0.5f);
        int p0y = (int)(points[0].Y / Data.rasterPixelsPerStep + 0.5f);
        int p1x = (int)(points[1].X / Data.rasterPixelsPerStep + 0.5f);
        int p1y = (int)(points[1].Y / Data.rasterPixelsPerStep + 0.5f);
        int p2x = (int)(points[2].X / Data.rasterPixelsPerStep + 0.5f);
        int p2y = (int)(points[2].Y / Data.rasterPixelsPerStep + 0.5f);

        // get bounds
        for (int p = 0; p < points.Length; p++)
        {
            minX = Math.Clamp(Math.Min(minX, (int)(points[p].X / Data.rasterPixelsPerStep + 0.5f)), 0, (int)Data.rasterWidth);
            maxX = Math.Clamp(Math.Max(maxX, (int)(points[p].X / Data.rasterPixelsPerStep + 0.5f)), 0, (int)Data.rasterWidth);
            minY = Math.Clamp(Math.Min(minY, (int)(points[p].Y / Data.rasterPixelsPerStep + 0.5f)), 0, (int)Data.rasterHeight);
            maxY = Math.Clamp(Math.Max(maxY, (int)(points[p].Y / Data.rasterPixelsPerStep + 0.5f)), 0, (int)Data.rasterHeight);
        }

        int x = minX;
        int y = minY;

        int area = (p2x - p0x) * (p1y - p0y) - (p2y - p0y) * (p1x - p0x);

        int edge0_xstep = p2y - p1y;
        int edge0_ystep = -(p2x - p1x);
        int edge1_xstep = p0y - p2y;
        int edge1_ystep = -(p0x - p2x);
        int edge2_xstep = p1y - p0y;
        int edge2_ystep = -(p1x - p0x);

        int edge0_base = (x - p1x) * edge0_xstep + (y - p1y) * edge0_ystep;
        int edge1_base = (x - p2x) * edge1_xstep + (y - p2y) * edge1_ystep;
        int edge2_base = (x - p0x) * edge2_xstep + (y - p0y) * edge2_ystep;

        float P0Z = (500 * points[0].Z) / (area * Data.rasterPixelsPerStep);
        float P1Z = (500 * points[1].Z) / (area * Data.rasterPixelsPerStep);
        float P2Z = (500 * points[2].Z) / (area * Data.rasterPixelsPerStep);

        for (y = minY; y < maxY; y++)
        {
            int edge0 = edge0_base + edge0_ystep * (y - minY);
            int edge1 = edge1_base + edge1_ystep * (y - minY);
            int edge2 = edge2_base + edge2_ystep * (y - minY);
            int ydm = y * (int)Data.rasterWidth;
            for (x = minX; x < maxX; x++)
            {
                edge0 += edge0_xstep;
                edge1 += edge1_xstep;
                edge2 += edge2_xstep;

                if (edge0 <= 0 && edge1 <= 0 && edge2 <= 0)
                {
                    Data.depthMap[ydm + x] = Math.Min(Data.depthMap[ydm + x], (UInt16)Math.Clamp(-(edge0 * P0Z + edge1 * P1Z + edge2 * P2Z), 0, UInt16.MaxValue - 1));
                }

            }

        }
    }*/

    public void Generate(int seed)
    {
        Data.lines.Clear();
        Data.dots.Clear();

        // Array.Clear(Data.depthMap,0,Data.depthMap.Length);
        Array.Fill(Data.depthMap, (ushort)(UInt16.MaxValue / 2));
        Array.Fill(Data.shadowMap, (ushort)(UInt16.MaxValue / 2));

        _rand = new Random(seed);

        Vector3 lookfrom = new Vector3(0, 2, 3);
        Vector3 lookat = new Vector3(0, 0, 0);
        float distToFocus = 10;
        float aperture = 0.1f;
        aperture *= 0.15f;

        Camera cam = Camera.Create(
            new Vector3(0, 8, 6),
            new Vector3(0, 0, 0),
            Vector3.UnitY,
            60,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        _spheres = new Sphere[]
        {
            Sphere.Create(new Vector3(0,-100.5f,-1), 100),
            Sphere.Create(new Vector3(0f,1f,-3), 2.0f),
            Sphere.Create(new Vector3(1.5f,2,-2f), 1.0f),
            Sphere.Create(new Vector3(2,0,2), 0.5f),
            Sphere.Create(new Vector3(0,0.5f,2), 1.2f),
            Sphere.Create(new Vector3(-2,0,2), 0.5f),
            Sphere.Create(new Vector3(3.5f,7.5f,-0.5f), 0.3f)
        };

        Vector3 base_color = new Vector3(0.3f, 0.3f, 0.3f);

        _materials = new Material[]
        {
            Material.Lambertian(base_color*0.3f),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Emissive(new Vector3(0.65f, 0.65f, 0.65f))
        };

        float invWidth = 1f / Data.rasterWidth;
        float invHeight = 1f / Data.rasterHeight;
        uint SphereCount = (uint)_spheres.Length;

        long frameRays = 0;
        Parallel.For(0, Data.rasterHeight, y =>
        {
            int rayCount = 0;
            uint state = (uint)(y * 9781 + 1 * 6271) | 1;

            for (uint x = 0; x < Data.rasterWidth; x++)
            {
                Vector4 color = Vector4.Zero;
                for (uint sample = 0; sample < NumSamples; sample++)
                {
                    float u = (x + RandUtil.RandomFloat(ref state)) * invWidth;
                    float v = (y + RandUtil.RandomFloat(ref state)) * invHeight;
                    Ray ray = Camera.GetRay(cam, u, v, ref state);
                    color += Color(SphereCount, _spheres, _materials, ref state, ref ray, 0, ref rayCount);
                }
                color /= NumSamples;

                Data.shadowMap[y * (int)Data.rasterWidth + x] = (ushort)(MathF.Max(MathF.Min(color.Y, 1.0f), 0.0f) * ushort.MaxValue);
            }

            Interlocked.Add(ref frameRays, rayCount);
        });

        Console.WriteLine($"Total rays shot: {frameRays}.");
        Data.DebugConsole.Add($"Total rays shot: {frameRays}.");

        // Vector3[] points = new Vector3[] { P1, P2, P3, P4 };

        // Line l;

        // // Draw Lines            
        // for (int i = 0; i < points.Length; i++)
        // {
        // l = new Line
        // {
        //     type = LineType.Straight,
        //     acceleration = Acceleration.Single,
        //     points = new Vector3[] { points[i], points[(i + 1) % points.Length] }
        // };
        // Data.lines.Add(l);
        // }


        // rasterizeToDepthBuffer(new Vector3[] { P1, P3, P2 });
        // rasterizeToDepthBuffer(new Vector3[] { P1, P4, P3 });

        // for (int x = (int)(P1.X / Data.rasterPixelsPerStep); x < (int)(P3.X / Data.rasterPixelsPerStep); x++)
        // {
        //     for (int y = (int)(P1.Y / Data.rasterPixelsPerStep); y < (int)(P3.Y / Data.rasterPixelsPerStep); y++)
        //     {
        //         int ydm = y * (int)Data.rasterWidth;
        //         Data.shadowMap[ydm + x] = ushort.MinValue;
        //     }
        // }
    }

    public Vector4 Color(
        uint sphereCount,
        Sphere[] spheres,
        Material[] materials,
        ref uint randState,
        ref Ray ray,
        int depth,
        ref int rayCount)
    {
        rayCount += 1;
        RayHit hit;
        hit.Position = new Vector3();
        hit.Normal = new Vector3();
        hit.T = 0;
        float closest = 9999999f;
        bool hitAnything = false;
        uint hitID = 0;
        for (uint i = 0; i < sphereCount; i++)
        {
            if (_materials[i].Type != MaterialType.Emissive)
            {
                if (Sphere.Hit(spheres[i], ray, 0.001f, closest, out RayHit tempHit))
                {
                    hitAnything = true;
                    hit = tempHit;
                    hitID = i;
                    closest = hit.T;
                }
            }
        }

        if (hitAnything)
        {
            if (depth < 50 && Scatter(ray, hit, materials[hitID], ref randState, out Vector3 attenuation, out Ray scattered, out Vector3 emission))
            {
                // return Vector4.Clamp(new Vector4(emission, 1f) + new Vector4(attenuation, 1f) * Color(sphereCount, spheres, materials, ref randState, ref scattered, depth + 1, ref rayCount),Vector4.Zero,Vector4.One);
                return new Vector4(emission, 1f) + new Vector4(attenuation, 1f) * Color(sphereCount, spheres, materials, ref randState, ref scattered, depth + 1, ref rayCount);
            }
            else
            {
                return Vector4.Zero;
            }
        }
        else
        {
            // SKY
            Vector3 unitDir = Vector3.Normalize(ray.Direction);
            float t = 0.5f * (unitDir.Y + 1f);
            return (1f - t) * Vector4.One + t * new Vector4(0.2f, 0.2f, 0.2f, 1f);
        }
    }

    public bool Scatter(Ray ray, RayHit hit, Material material, ref uint state, out Vector3 attenuation, out Ray scattered, out Vector3 emission)
    {
        switch (material.Type)
        {
            case MaterialType.Lambertian:
                {
                    Vector3 target = hit.Position + hit.Normal + RandUtil.RandomInUnitSphere(ref state);
                    scattered = Ray.Create(hit.Position, target - hit.Position);
                    attenuation = material.Albedo;
                    emission = Vector3.Zero;

                    // sample light

                    for (int i = 0; i < _spheres.Length; ++i)
                    {
                        if (_materials[i].Type == MaterialType.Emissive)
                        {
                            Ray shadowRay = Ray.Create(hit.Position, _spheres[i].Center - hit.Position);

                            RayHit shadowHit;
                            shadowHit.Position = new Vector3();
                            shadowHit.Normal = new Vector3();
                            shadowHit.T = 0;
                            bool hitAnything = false;
                            for (uint j = 0; j < _spheres.Length; j++)
                            {
                                if (_materials[j].Type != MaterialType.Emissive)
                                {
                                    if (Sphere.Hit(_spheres[j], shadowRay, 0.001f, 9999999f, out RayHit tempHit))
                                    {
                                        hitAnything = true;
                                        shadowHit = tempHit;
                                    }
                                }
                            }

                            if (!hitAnything)
                            {
                                Vector3 SurfaceNormal = hit.Normal;
                                Vector3 LightDirection = Vector3.Normalize(shadowRay.Direction);

                                float cosTheta = MathF.Max(0.0f, Vector3.Dot(LightDirection, SurfaceNormal));

                                emission += _materials[i].Albedo * cosTheta;
                            }
                        }
                    }
                    return true;
                }

            default:
                attenuation = new Vector3();
                emission = new Vector3();
                scattered = Ray.Create(new Vector3(), new Vector3());
                return false;
        }
    }


}