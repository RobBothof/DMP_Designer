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

public class Tracer1 : IGenerator
{
    public const uint NumSamples = 2;

    private Sphere[] _spheres;
    private Material[] _materials;
    private ulong _totalRays = 0;

    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800, 380160, 0);
    // public float shadeStep=0.15f;

    public Vector3 SpotLight = new Vector3(-40.0f, 10f, -5f);

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
    }

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
            new Vector3(0, 5, 7),
            new Vector3(0, 0, 0),
            Vector3.UnitY,
            60,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        _spheres = new Sphere[]
        {
            Sphere.Create(new Vector3(0,-100.5f,-1), 100),
            Sphere.Create(new Vector3(2.2f,0,-2), 1.5f),
            Sphere.Create(new Vector3(0,0,-1), 0.5f),
            Sphere.Create(new Vector3(-2,1,-1), 0.75f),
            Sphere.Create(new Vector3(2,0,1), 0.5f),
            Sphere.Create(new Vector3(0,0,1), 0.5f),
            Sphere.Create(new Vector3(-2,0,1), 0.5f),
            Sphere.Create(new Vector3(0.5f,1.2f,0.5f), 0.75f),
            Sphere.Create(new Vector3(-1.5f,2.5f,-0.5f), 0.3f)
        };

        Vector3 base_color = new Vector3(0.2f, 0.2f, 0.2f);
        _materials = new Material[]
            {
                Material.Metal(base_color, 0.5f),
                Material.Lambertian(base_color),
                Material.Lambertian(base_color),
                Material.Metal(base_color, 0),
                Material.Metal(base_color, 0),
                Material.Metal(base_color, 0.2f),
                Material.Metal(base_color, 0.6f),
                Material.Lambertian(base_color),
                // Material.Dielectric(1.5f),
                // Material.Lambertian(new Vector3(1.7f, 1.7f, 1.7f))
                Material.Emissive(new Vector3(5.1f, 5.1f, 5.1f))
            };

        float invWidth = 1f / Data.rasterWidth;
        float invHeight = 1f / Data.rasterHeight;
        uint SphereCount = (uint)_spheres.Length;

        for(uint y =0; y < Data.rasterHeight; y++)
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

                // Ray ray = Camera.GetRay(cam, x, y, ref state);
                // color += Color(SphereCount, _spheres, _materials, ref state, ref ray, 0, ref rayCount);

                Data.shadowMap[y * (int)Data.rasterWidth + x] = (ushort) (color.Y * ushort.MaxValue);
            }
        };




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

        public static Vector4 Color(
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
                if (Sphere.Hit(spheres[i], ray, 0.001f, closest, out RayHit tempHit))
                {
                    hitAnything = true;
                    hit = tempHit;
                    hitID = i;
                    closest = hit.T;
                }
            }

            if (hitAnything)
            {
                if (depth < 50 && Scatter(ray, hit, materials[hitID], ref randState, out Vector3 attenuation, out Ray scattered))
                {
                    return new Vector4(attenuation, 1f) * Color(sphereCount, spheres, materials, ref randState, ref scattered, depth + 1, ref rayCount);
                }
                else
                {
                    return Vector4.Zero;
                }
            }
            else
            {
                return new Vector4(0.5f, 0.7f, 1f, 1f);
                Vector3 unitDir = Vector3.Normalize(ray.Direction);
                float t = 0.5f * (unitDir.Y + 1f);
                return (1f - t) * Vector4.One + t * new Vector4(0.5f, 0.7f, 1f, 1f);
            }
        }

        public static bool Scatter(Ray ray, RayHit hit, Material material, ref uint state, out Vector3 attenuation, out Ray scattered)
        {
            switch (material.Type)
            {
                case MaterialType.Lambertian:
                {
                    Vector3 target = hit.Position + hit.Normal + RandUtil.RandomInUnitSphere(ref state);
                    scattered = Ray.Create(hit.Position, target - hit.Position);
                    attenuation = material.Albedo;
                    return true;
                }

                case MaterialType.Metal:
                {
                    Vector3 reflected = Vector3.Reflect(Vector3.Normalize(ray.Direction), hit.Normal);
                    scattered = Ray.Create(
                        hit.Position,
                        reflected + material.FuzzOrRefIndex * RandUtil.RandomInUnitSphere(ref state));
                    attenuation = material.Albedo;
                    return Vector3.Dot(scattered.Direction, hit.Normal) > 0;
                }

                case MaterialType.Dielectric:
                {
                    Vector3 outwardNormal;
                    Vector3 reflectDir = Vector3.Reflect(ray.Direction, hit.Normal);
                    float niOverNt;
                    attenuation = new Vector3(1, 1, 1);
                    Vector3 refractDir;
                    float reflectProb;
                    float cosine;
                    if (Vector3.Dot(ray.Direction, hit.Normal) > 0)
                    {
                        outwardNormal = -hit.Normal;
                        niOverNt = material.FuzzOrRefIndex;
                        cosine = material.FuzzOrRefIndex * Vector3.Dot(ray.Direction, hit.Normal) / ray.Direction.Length();
                    }
                    else
                    {
                        outwardNormal = hit.Normal;
                        niOverNt = 1f / material.FuzzOrRefIndex;
                        cosine = -Vector3.Dot(ray.Direction, hit.Normal) / ray.Direction.Length();
                    }
                    if (Refract(ray.Direction, outwardNormal, niOverNt, out refractDir))
                    {
                        reflectProb = Schlick(cosine, material.FuzzOrRefIndex);
                    }
                    else
                    {
                        reflectProb = 1f;
                    }
                    if (RandUtil.RandomFloat(ref state) < reflectProb)
                    {
                        scattered = Ray.Create(hit.Position, reflectDir);
                    }
                    else
                    {
                        scattered = Ray.Create(hit.Position, refractDir);
                    }

                    return true;
                }

                default:
                    attenuation = new Vector3();
                    scattered = Ray.Create(new Vector3(), new Vector3());
                    return false;
            }
        }

        public static bool Refract(Vector3 v, Vector3 n, float niOverNt, out Vector3 refracted)
        {
            Vector3 uv = Vector3.Normalize(v);
            float dt = Vector3.Dot(uv, n);
            float discriminant = 1f - niOverNt * niOverNt * (1 - dt * dt);
            if (discriminant > 0)
            {
                refracted = niOverNt * (uv - n * dt) - n * MathF.Sqrt(discriminant);
                return true;
            }
            else
            {
                refracted = Vector3.Zero;
                return false;
            }
        }

        public static float Schlick(float cosine, float refIndex)
        {
            float r0 = (1 - refIndex) / (1 + refIndex);
            r0 = r0 * r0;
            return r0 + (1 - r0) * MathF.Pow(1 - cosine, 5);
        }
}