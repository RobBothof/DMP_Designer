using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using System.Diagnostics;
using MathNet.Numerics;
using System.Runtime.CompilerServices;
using PathTracer;
using System.Threading;
using System.Threading.Tasks;

public class PathTracer5 : IGenerator
{
    public const uint NumSamples = 10;

    // private Sphere[] _spheres;
    private List<IShape> _shapes;
    private Material[] _materials;

    Vector3 PaperCenter = new Vector3(268800, 380160, 0);

    private RandomRobber _rng;

    public void Generate(int seed, CancellationToken token)
    {
        Data.lines.Clear();
        Data.dots.Clear();

        Array.Fill(Data.depthMap, (ushort)(UInt16.MaxValue / 2));
        Array.Fill(Data.shadowMap, (ushort)(UInt16.MaxValue / 2));

        _rng = new RandomRobber((uint)seed);

        float distToFocus = 15;
        float aperture = 0.025f;

        Camera cam = Camera.Create(
            new Vector3(-6, 6.5f, 15.0f),
            new Vector3(2.0f, -0.7f, 0),
            Vector3.UnitY,
            34,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        _shapes = new List<IShape>
        {
            Sphere.Create(new Vector3(0,-2000.0f,-1), 2000),
            Sphere.Create(new Vector3(2f,1.0f,3.5f), 1.0f),
            Sphere.Create(new Vector3(-1.85f,0.5f,3.0f), 0.5f),

            Quad.Create(new Vector3(-1.0f, 0.0f, 3.0f), new Vector3(2.0f, 0.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f)),
            Quad.Create(new Vector3(-1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 2.0f), new Vector3(0.0f, 2.0f, 0.0f)),
            Quad.Create(new Vector3(-1.0f, 2.0f, 1.0f), new Vector3(0.0f, 0.0f, 2.0f), new Vector3(2.0f, 0.0f, 0.0f)),
            Quad.Create(new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 2.0f), new Vector3(0.0f, 2.0f, 0.0f)),
            Quad.Create(new Vector3(-1.0f, 0.0f, 1.0f), new Vector3(2.0f, 0.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f)),

            Sphere.Create(new Vector3(4.0f,6.0f,4.5f), 1.3f),
        };

        float base_color = 0.7f;

        _materials = new Material[]
        {
            Material.Lambertian(1.0f),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),


            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),
            Material.Lambertian(base_color),

            Material.Emissive(2.35f)
        };

        float invWidth = 1f / Data.rasterWidth;
        float invHeight = 1f / Data.rasterHeight;

        long frameRays = 0;
        Parallel.For(0, Data.rasterHeight, y =>
        {
            int rayCount = 0;
            // uint state = (uint)(y * 9781 + 1 * 6271) | 1;

            for (uint x = 0; x < Data.rasterWidth; x++)
            {
                float color = 0;
                for (uint sample = 0; sample < NumSamples; sample++)
                {
                    float u = (x + _rng.RandomFloat()) * invWidth;  // blur / antialiasing
                    float v = (y + _rng.RandomFloat()) * invHeight; // blur / antialiasing
                    Ray ray = Camera.GetRay(cam, u, v, _rng.RandomInUnitDisk());
                    color += Trace(ref ray, 0, ref rayCount);
                }
                color /= NumSamples;

                double gamma = 2.2f;
                color = (float)Math.Pow(color, 1.0 / gamma);
                Data.shadowMap[y * (int)Data.rasterWidth + x] = (ushort)(MathF.Max(MathF.Min(color, 1.0f), 0.0f) * ushort.MaxValue);
            }

            Interlocked.Add(ref frameRays, rayCount);
        });

        Console.WriteLine($"Total rays shot: {frameRays}.");
        Data.DebugConsole.Add($"Total rays shot: {frameRays}.");

    }

    public float Trace(ref Ray ray, int depth, ref int rayCount)
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
            // if (_materials[i].Type != MaterialType.Emissive)
            // {
            if (_shapes[i].Hit(ray, 0.001f, closest, out RayHit tempHit))
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
            if (depth < 50 && Scatter(ray, hit, hitID, out float attenuation, out Ray scatteredRay, out float emission))
            {
                return emission + attenuation * Trace(ref scatteredRay, depth + 1, ref rayCount);
            }
            else
            {
                return 0;
            }
        }
        else
        {
            // return Vector4.Zero;
            // SKY Color
            return 0.02f;
            Vector3 unitDir = Vector3.Normalize(ray.Direction);
            float t = 0.5f * (unitDir.Y + 1f);
            return (1f - t) * 0.0f + t * 0.015f;
        }
    }

    public bool Scatter(Ray ray, RayHit hit, int hitID, out float attenuation, out Ray scatteredRay, out float emission)
    {
        switch (_materials[hitID].Type)
        {
            case MaterialType.Lambertian:
                {
                    Vector3 direction = hit.Normal + _rng.RandomOnHemisphere(hit.Normal);
                    scatteredRay = Ray.Create(hit.Position, direction);
                    attenuation = _materials[hitID].Albedo;
                    emission = _materials[hitID].Emission; ;

                    // sample light

                    for (int i = 0; i < _shapes.Count; ++i)
                    {
                        if (_materials[i].Type == MaterialType.Emissive)
                        {
                            Ray shadowRay = Ray.Create(hit.Position, _shapes[i].Center - hit.Position);

                            RayHit shadowHit;
                            shadowHit.Position = new Vector3();
                            shadowHit.Normal = new Vector3();
                            shadowHit.T = 0;
                            bool hitAnything = false;
                            for (int j = 0; j < _shapes.Count; j++)
                            {
                                if (_materials[j].Type != MaterialType.Emissive)
                                {
                                    if (_shapes[j].Hit(shadowRay, 0.001f, 9999999f, out RayHit tempHit))
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

                                // emission += _materials[i].emission * cosTheta;
                            }
                        }
                    }

                    return true;
                }

            case MaterialType.Emissive:
                {
                    emission = _materials[hitID].Emission;
                    attenuation = _materials[hitID].Albedo;
                    scatteredRay = Ray.Create(new Vector3(), new Vector3());
                    return true;
                }

            case MaterialType.Metal:
                {
                    emission = _materials[hitID].Emission;
                    Vector3 reflected = Vector3.Reflect(Vector3.Normalize(ray.Direction), hit.Normal);
                    scatteredRay = Ray.Create(hit.Position, reflected + _materials[hitID].FuzzOrRefIndex * _rng.RandomInUnitSphere());
                    attenuation = _materials[hitID].Albedo;
                    return Vector3.Dot(scatteredRay.Direction, hit.Normal) > 0;
                }

            default:
                attenuation = 0;
                emission = 0;
                scatteredRay = Ray.Create(new Vector3(), new Vector3());
                return false;
        }
    }


}