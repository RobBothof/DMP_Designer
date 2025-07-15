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
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Syntax;


public class LandShape3b : IGenerator
{
    private List<IShape> _shapes;
    private List<Light> _lights;
    private List<Cuboid> _cuboids;
    private List<Pyramid> _pyramids;
    private List<Sphere> _spheres;

    private List<Tower> _towers;

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
    int stepSize = 64;

    int numShadowLines = 500;

    private RandomRobber _rng;
    private CancellationToken _cancellationToken;

    public void Generate(int seed, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _cancellationToken.ThrowIfCancellationRequested();
        paper = new Vector3(Data.paperSize.X * Data.stepsPerMM, Data.paperSize.Y * Data.stepsPerMM, 0);
        paperCenter = paper * 0.5f;

        Data.lines.Clear();
        Data.dots.Clear();

        float distToFocus = 10f;
        float aperture = 0f;

        cam = Camera.Create(
            new Vector3(90, 40f, 90),
            new Vector3(-1, 5f, 0),
            -Vector3.UnitY,
            95f,
            (float)Data.rasterWidth / Data.rasterHeight,
            aperture,
            distToFocus);

        Array.Fill(Data.depthMap, (ushort)(UInt16.MaxValue / 2));
        Array.Fill(Data.shadowMap, (ushort)(UInt16.MaxValue / 2));

        seed = 1898010300;
        _rng = new RandomRobber((uint)seed);

        Material mat1 = Material.Lambertian(0.7f, 0.0f);
        Material mat2 = Material.ShadowMatte(1.5f, 0.0f);

        _lights = new List<Light>
        {
            // new Light { Position = new Vector3(2000.0f, 4500.0f, -1000.0f), Intensity = 5.0f }
            new Light { Position = new Vector3(0.0f, 0.0f, 1000.0f), Intensity = 5.0f }
        };

        // _towers = new List<Tower>();
        // _towers.Add(new Tower(1, new Vector3(0, 0, 0), new Vector3(10, 30, 10), Quaternion.Identity));

        // foreach (Tower tower in _towers)
        // {
        //     tower.Generate(1);
        // }

        _cuboids = new List<Cuboid>();
        _pyramids = new List<Pyramid>();
        _spheres = new List<Sphere>();

        int id_counter = 10;

        // _cuboids.Add(Cuboid.Create(new Vector3(20, 10, 20), new Vector3(10f, 1f, 10f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.0f), mat1, id_counter++));
        // _cuboids.Add(Cuboid.Create(new Vector3(0, 0, 0), new Vector3(160f, 1, 160f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.35f * MathF.PI), mat1, id_counter++));

        // _spheres.Add(Sphere.Create(new Vector3(0, 0, 0), 10f, mat1, id_counter++));

        // for (int i = 0; i < 500; i++)
        // {
        //     Vector3 origin = new Vector3(40, 140, 100);
        //     Vector3 pos = _rng.RandomUnitVector() * 60 - origin;
        //     // Vector3 size = new Vector3(10, 10, 10) * (0.25f + _rng.RandomFloat() * 0.75f);
        //     Vector3 size = (_rng.RandomUnitVector()+new Vector3(3, 2, 1)) * 5f * (0.15f + _rng.RandomFloat() * 0.85f);

        //     // Calculate the direction from center to the cube position
        //     Vector3 dirToSurface = Vector3.Normalize(pos);

        //     // Create a rotation that aligns the Y-axis with this direction
        //     Quaternion orientation;

        //     // Handle the case where direction is parallel to Y-axis
        //     if (Math.Abs(dirToSurface.Y) > 0.9999f)
        //     {
        //         if (dirToSurface.Y > 0)
        //             orientation = Quaternion.Identity;  // Already aligned with Y-axis
        //         else
        //             orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);  // Rotate 180° around X
        //     }
        //     else
        //     {
        //         // Calculate rotation from Y-axis to direction
        //         Vector3 rotAxis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dirToSurface));
        //         float angle = MathF.Acos(Vector3.Dot(Vector3.UnitY, dirToSurface));
        //         orientation = Quaternion.CreateFromAxisAngle(rotAxis, angle);
        //     }

        //     // Quaternion orientation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(pos), 0);
        //     _cuboids.Add(Cuboid.Create(pos, size, orientation, mat1, id_counter++));
        // }


        for (int i = 0; i < 5000; i++)
        {
            Vector3 origin = new Vector3(2, -17, 2);
            Vector3 pos = _rng.RandomUnitVector() * 55 - origin;
            // Vector3 size = new Vector3(10, 10, 10) * (0.25f + _rng.RandomFloat() * 0.75f);
            Vector3 size = (_rng.RandomUnitVector() + new Vector3(5, 2, 1)) * 5f * (0.15f + _rng.RandomFloat() * 0.85f);

            // Calculate the direction from center to the cube position
            Vector3 dirToSurface = Vector3.Normalize(pos);

            // Create a rotation that aligns the Y-axis with this direction
            Quaternion orientation;

            // Handle the case where direction is parallel to Y-axis
            if (Math.Abs(dirToSurface.Y) > 0.9999f)
            {
                if (dirToSurface.Y > 0)
                    orientation = Quaternion.Identity;  // Already aligned with Y-axis
                else
                    orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);  // Rotate 180° around X
            }
            else
            {
                // Calculate rotation from Y-axis to direction
                Vector3 rotAxis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dirToSurface));
                float angle = MathF.Acos(Vector3.Dot(Vector3.UnitY, dirToSurface));
                orientation = Quaternion.CreateFromAxisAngle(rotAxis, angle);
            }

            // Quaternion orientation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(pos), 0);
            _cuboids.Add(Cuboid.Create(pos, size, orientation, mat1, id_counter++));
        }

        for (int i = 0; i < 200; i++)
        {
            Vector3 origin = new Vector3(5, -17, 2);
            Vector3 pos = new Vector3(_rng.RandomFloat(), _rng.RandomFloat() * 1.5f - 0.77f, _rng.RandomFloat() * 1.1f) * 62 - origin;
            // Vector3 size = new Vector3(10, 10, 10) * (0.25f + _rng.RandomFloat() * 0.75f);
            Vector3 size = (_rng.RandomUnitVector() + new Vector3(5, 2, 1)) * 3f * (0.15f + _rng.RandomFloat() * 0.85f);

            // Calculate the direction from center to the cube position
            Vector3 dirToSurface = Vector3.Normalize(pos);

            // Create a rotation that aligns the Y-axis with this direction
            Quaternion orientation;

            // Handle the case where direction is parallel to Y-axis
            if (Math.Abs(dirToSurface.Y) > 0.9999f)
            {
                if (dirToSurface.Y > 0)
                    orientation = Quaternion.Identity;  // Already aligned with Y-axis
                else
                    orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);  // Rotate 180° around X
            }
            else
            {
                // Calculate rotation from Y-axis to direction
                Vector3 rotAxis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dirToSurface));
                float angle = MathF.Acos(Vector3.Dot(Vector3.UnitY, dirToSurface));
                orientation = Quaternion.CreateFromAxisAngle(rotAxis, angle);
            }

            // Quaternion orientation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(pos), 0);
            _cuboids.Add(Cuboid.Create(pos, size, orientation, mat1, id_counter++));
        }
        _shapes = new List<IShape>();



        /// END SCENE SETUP

        _cancellationToken.ThrowIfCancellationRequested();

        // The collection of shapes is used to trace visibility and shadows.
        _shapes.AddRange(_cuboids.SelectMany(c => c.Quads).Cast<IShape>());
        // _shapes.AddRange(_pyramids.SelectMany(p => p.Triangles).Cast<IShape>());
        // _shapes.AddRange(_spheres.Cast<IShape>());

        Parallel.ForEach(_cuboids, cub1 =>
        // foreach (Cuboid cub1 in _cuboids)
        {
            _cancellationToken.ThrowIfCancellationRequested();
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

                    }
                }
            }
        }
        );


        Parallel.ForEach(_pyramids, p =>
        // foreach (Pyramid p in _pyramids)
        {
            _cancellationToken.ThrowIfCancellationRequested();
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
        );

        // Draw Shadow Lines
        float yStep = paper.Y / numShadowLines;

        // Create array of indices and shuffle them
        int[] shadowLineIndices = Enumerable.Range(-1, numShadowLines + 3).ToArray();
        for (int i = shadowLineIndices.Length - 1; i > 0; i--)
        {
            int j = (int)(_rng.RandomFloat() * (i + 1));
            // Swap elements
            int temp = shadowLineIndices[i];
            shadowLineIndices[i] = shadowLineIndices[j];
            shadowLineIndices[j] = temp;
        }

        // Use randomized indices for drawing shadow lines
        Parallel.ForEach(shadowLineIndices, s =>
        {
            _cancellationToken.ThrowIfCancellationRequested();
            Vector3 from = new Vector3(0, yStep * s, 0);
            Vector3 to = new Vector3(paper.X, yStep * s, 0);
            Vector3 P0 = from;
            Vector3 P1 = to;

            float xSteps = paper.X / stepSize;
            bool startDraw = false;

            Vector3 P = from;

            for (int i = 0; i < xSteps; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                P += new Vector3(stepSize, 0, 0);

                Vector3 target = cam.LowerLeftCorner + P.X * cam.Horizontal / paper.X + P.Y * cam.Vertical / paper.Y;

                if (TraceShadow(cam.Origin, target, out Vector3 hitPoint))
                {
                    if (!startDraw)
                    {
                        startDraw = true;
                        P0 = Camera.Project(cam, hitPoint) * paper.X + paperCenter;
                    }
                    P1 = Camera.Project(cam, hitPoint) * paper.X + paperCenter;
                    continue;
                }
                if (startDraw)
                {
                    // P1 = P;
                    Data.lines.Add(new Line
                    {
                        type = LineType.Straight,
                        acceleration = Acceleration.Single,
                        points = new Vector3[] { P0, P1 }
                    });
                    startDraw = false;
                }
            }

            if (startDraw)
            {
                Data.lines.Add(new Line
                {
                    type = LineType.Straight,
                    acceleration = Acceleration.Single,
                    points = new Vector3[] { P0, P1 }
                });
            }
        });

    }



    // Check the lines visibility, split line if needed and project it to paper.
    public void AddLine(Vector3 from, Vector3 to, LineType type, Acceleration acceleration, int parentID)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        Vector3 P0 = Camera.Project(cam, from) * paper.X + paperCenter;
        Vector3 P1 = Camera.Project(cam, to) * paper.X + paperCenter;

        //calculate steps
        float steps = Vector3.Distance(P0, P1) / stepSize;
        bool startDraw = false;

        for (int i = 0; i < steps; i++)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            Vector3 P = Vector3.Lerp(from, to, i / steps);
            Vector3 PCam = Camera.Project(cam, P) * (paper.X) + paperCenter;
            if (PCam.X > 0 && PCam.X < paper.X && PCam.Y > 0 && PCam.Y < paper.Y)
            {
                // Check if point is in front of camera.
                if (Vector3.Dot(Vector3.Normalize(P - cam.Origin), cam.W) < 0)
                {
                    // Reverse cast a ray from the point to the camera to check if something is obscuring the view.

                    // Draw if visible (and optional, not in shadow.)
                    if (TraceVisible(P, cam.Origin, parentID) && TraceVisible(P, _lights[0].Position, -2))
                    // if (TraceVisible(P, cam.Origin, parentID))
                    {
                        if (!startDraw)
                        {
                            startDraw = true;
                            P0 = PCam;
                        }
                        continue;
                    }
                }
            }
            if (startDraw)
            {
                P1 = PCam;
                startDraw = false;
                Data.lines.Add(new Line
                {
                    type = type,
                    acceleration = acceleration,
                    points = new Vector3[] { P0, P1 }
                });
            }
        }

        // Stop drawing if needed.
        if (startDraw)
        {
            P1 = Camera.Project(cam, to) * paper.X + paperCenter;
            Data.lines.Add(new Line
            {
                type = type,
                acceleration = acceleration,
                points = new Vector3[] { P0, P1 }
            });
        }
    }

    public bool TraceVisible(Vector3 from, Vector3 to, int parentID)
    {
        Vector3 direction = Vector3.Normalize(to - from);
        Ray r = Ray.Create(from, direction);
        float maxDistance = Vector3.Distance(from, to); // Calculate the distance between from and to

        for (int j = 0; j < _shapes.Count; j++)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_shapes[j].parentID != parentID && parentID != -1)
            {
                if (_shapes[j].Hit(r, 0.001f, maxDistance, out RayHit tempHit))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool TraceShadow(Vector3 origin, Vector3 target, out Vector3 hitPoint)
    {
        Ray ray = new Ray(origin, Vector3.Normalize(target - origin));
        hitPoint = Vector3.Zero;

        RayHit hit = new RayHit();
        float closest = 9999f;
        bool hitAnything = false;

        for (int i = 0; i < _shapes.Count; i++)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_shapes[i].Hit(ray, 0.001f, closest, out RayHit tempHit))
            {
                hitAnything = true;
                hit = tempHit;
                closest = hit.T;
                hitPoint = hit.Position;
            }
        }

        if (hitAnything)
        {
            // sample lights
            for (int i = 0; i < _lights.Count; i++)
            {
                Vector3 lightDirection = Vector3.Normalize(_lights[i].Position - hit.Position);
                float lightDistance = Vector3.Distance(hit.Position, _lights[i].Position);
                Ray shadowRay = new Ray(hit.Position, lightDirection);
                for (int j = 0; j < _shapes.Count; j++)
                {
                    if (_shapes[j].Hit(shadowRay, 0.001f, lightDistance, out RayHit tempHit))
                    {
                        // Target is in shadow.
                        return true;
                    }
                }
            }
        }
        // Target is not in shadow (or not visible).
        return false;
    }

    /*
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
    */
}