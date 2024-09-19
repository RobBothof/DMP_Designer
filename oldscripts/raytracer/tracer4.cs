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

public class Tracer4: IGenerator
{
    public const uint NumSamples = 20;

    private Sphere[] _spheres;
    private Material[] _materials;

    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800, 380160, 0);

    public void Generate(int seed)
    {
        Data.lines.Clear();
        Data.dots.Clear();

        Array.Fill(Data.depthMap, (ushort)(UInt16.MaxValue / 2));
        Array.Fill(Data.shadowMap, (ushort)(UInt16.MaxValue / 2));

        _rand = new Random(seed);

        float distToFocus = 15;
        float aperture = 0.025f;

        Camera cam = Camera.Create(
            new Vector3(-7, 3.5f, 15.0f),
            new Vector3(0.7f, -0.3f, 0),
            Vector3.UnitY,
            30,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        _spheres = new Sphere[]
        {
            Sphere.Create(new Vector3(0,-2000.5f,-1), 2000),
            // Sphere.Create(new Vector3(0f,1f,-3), 2.0f),
            // Sphere.Create(new Vector3(1.5f,2,-2f), 1.0f),
            Sphere.Create(new Vector3(1.7f,0,2), 0.5f),
            Sphere.Create(new Vector3(0,0.7f,2), 1.2f),
            Sphere.Create(new Vector3(-2f,0,2.5f), 0.5f),
            Sphere.Create(new Vector3(3.5f,7.5f,2.5f), 1.3f)
        };

        Vector3 base_color = new Vector3(0.5f, 0.5f, 0.5f);

        _materials = new Material[]
        {
            Material.Lambertian(new Vector3(1.0f, 1.0f, 1.0f)),
            // Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            // Material.Metal(base_color,0.7f),
            // Material.Metal(base_color,0.9f),
            // Material.Metal(base_color,0.8f),

            Material.Emissive(new Vector3(0.35f, 0.35f, 0.35f)*1f)
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

                double gamma = 2.2f;
                color.Y = (float) Math.Pow(color.Y, 1.0 / gamma);

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
            // if (_materials[i].Type != MaterialType.Emissive)
            // {
                if (Sphere.Hit(spheres[i], ray, 0.001f, closest, out RayHit tempHit))
                {
                    hitAnything = true;
                    hit = tempHit;
                    hitID = i;
                    closest = hit.T;
                }
            // }
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
            // return Vector4.Zero;
            // SKY
            return new Vector4(0.01f, 0.01f, 0.01f,1.0f);
            Vector3 unitDir = Vector3.Normalize(ray.Direction);
            float t = 0.5f * (unitDir.Y + 1f);
            return (1f - t) * Vector4.One*0.0f + t * Vector4.One * 0.015f;
        }
    }

    public bool Scatter(Ray ray, RayHit hit, Material material, ref uint state, out Vector3 attenuation, out Ray scattered, out Vector3 emission)
    {
        switch (material.Type)
        {
            case MaterialType.Lambertian:
                {
                    Vector3 direction = hit.Normal + RandUtil.RandomOnHemisphere(ref state, hit.Normal);
                    scattered = Ray.Create(hit.Position, direction);
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

                                // emission += _materials[i].Albedo * cosTheta;
                            }
                        }
                    }
                    
                    return true;
                }
    
            case MaterialType.Emissive:
                {
                    attenuation = Vector3.Zero;
                    emission = material.Albedo;
                    scattered = Ray.Create(new Vector3(), new Vector3());
                    return false;
                }

            case MaterialType.Metal:
            {
                emission = Vector3.Zero;
                Vector3 reflected = Vector3.Reflect(Vector3.Normalize(ray.Direction), hit.Normal);
                scattered = Ray.Create(
                    hit.Position,
                    reflected + material.FuzzOrRefIndex * RandUtil.RandomInUnitSphere(ref state));
                attenuation = material.Albedo;
                return Vector3.Dot(scattered.Direction, hit.Normal) > 0;
            }
    
            default:
                attenuation = new Vector3();
                emission = new Vector3();
                scattered = Ray.Create(new Vector3(), new Vector3());
                return false;
        }
    }


}