using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;
using System.Diagnostics;
using MathNet.Numerics;
using System.Runtime.CompilerServices;

public class CubesA2_3 : IGenerator
{
    public static Random _rand;
    Vector3 PaperCenter = new Vector3(268800, 380160, 0);
    // public float shadeStep=0.15f;

    public Vector3 SpotLight = new Vector3(10.0f, -5f, 30f);

    public Vector3 interpolate(Vector3 A, Vector3 B, float t)
    {
        return new Vector3(A.X * t + B.X * (1 - t), A.Y * t + B.Y * (1 - t), A.Z * t + B.Z * (1 - t));
    }

    public float Shade(Vector3 normal)
    {
        return (1.0f - 0.70f * Math.Max(0, Vector3.Dot(normal, SpotLight))) * (1.0f - 0.70f * Math.Max(0, Vector3.Dot(normal, SpotLight))) * 0.3f;
    }

    public int Shade2(Vector3 normal, float size)
    {
        // return (int)((Math.Max(0,Vector3.Dot(normal,SpotLight)))*(Math.Max(0,Vector3.Dot(normal,SpotLight)))*3);
        if (Vector3.Dot(normal, SpotLight) > 0.6)
        {
            return (int)(size * 0.75f);
        }
        else
        {
            if (Vector3.Dot(normal, SpotLight) > 0)
            {
                return (int)(size * 0.4f);
            }
            else
            {
                return 0;
            }
        }
    }

    public void rasterizeToDepthBuffer(Vector3[] points)
    {
        int minX = 2 * 8 * Data.stepsPerMM;
        int maxX = 0;
        int minY = 2 * 12 * Data.stepsPerMM;
        int maxY = 0;

        int p0x = (int)(points[0].X + 0.5f);
        int p0y = (int)(points[0].Y + 0.5f);
        int p1x = (int)(points[1].X + 0.5f);
        int p1y = (int)(points[1].Y + 0.5f);
        int p2x = (int)(points[2].X + 0.5f);
        int p2y = (int)(points[2].Y + 0.5f);
        // get bounds
        for (int p = 0; p < points.Length; p++)
        {
            minX = Math.Clamp(Math.Min(minX, (int)(points[p].X + 0.5f)), 0, 2 * 8 * Data.stepsPerMM);
            maxX = Math.Clamp(Math.Max(maxX, (int)(points[p].X + 0.5f)), 0, 2 * 8 * Data.stepsPerMM);
            minY = Math.Clamp(Math.Min(minY, (int)(points[p].Y + 0.5f)), 0, 2 * 12 * Data.stepsPerMM);
            maxY = Math.Clamp(Math.Max(maxY, (int)(points[p].Y + 0.5f)), 0, 2 * 12 * Data.stepsPerMM);
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

        float P0Z = 500 * points[0].Z / area;
        float P1Z = 500 * points[1].Z / area;
        float P2Z = 500 * points[2].Z / area;

        for (y = minY; y < maxY; y++)
        {
            int edge0 = edge0_base + edge0_ystep * (y - minY);
            int edge1 = edge1_base + edge1_ystep * (y - minY);
            int edge2 = edge2_base + edge2_ystep * (y - minY);
            int ydm = y * 2 * 8 * Data.stepsPerMM;
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

    public void DepthCheckAndAdd(Line l)
    {
        bool draw = false;
        Line m = new Line();

        if (l.type == LineType.Straight)
        {
            int startX = (int)(l.points[0].X + 0.5f);
            int startY = (int)(l.points[0].Y + 0.5f);
            int startZ = (int)(l.points[0].Z + 0.5f);
            int endX = (int)(l.points[1].X + 0.5f);
            int endY = (int)(l.points[1].Y + 0.5f);
            int endZ = (int)(l.points[1].Z + 0.5f);

            Int64 deltaX = Math.Abs(endX - startX);
            int dirX = startX < endX ? 1 : -1;

            Int64 deltaY = Math.Abs(endY - startY);
            int dirY = startY < endY ? 1 : -1;

            Int64 deltaZ = Math.Abs(endZ - startZ);
            int dirZ = startZ < endZ ? 1 : -1;

            Int64 deltaMax = Math.Max(deltaZ, Math.Max(deltaX, deltaY));

            Int64 errX = deltaMax / 2;
            Int64 errY = deltaMax / 2;
            Int64 errZ = deltaMax / 2;

            Int64 x = startX;
            Int64 y = startY;
            Int64 z = startZ;

            if (!draw)
            {
                if (x > 0 && y > 0 && x < Program._drawSize[0] * Data.stepsPerMM && y < Program._drawSize[1] * Data.stepsPerMM)
                {
                    if (Data.depthMap[(y / 50) * 2 * 8 * Data.stepsPerMM + (x / 50)] == UInt16.MaxValue)
                    {
                        draw = true;
                        m = new Line();
                        m.type = l.type;
                        m.acceleration = l.acceleration;
                        m.points = new Vector3[2];
                        m.points[0] = new Vector3(x, y, 0);
                    }
                }
            }

            for (Int64 i = deltaMax; i > 0; i--)
            {
                errX -= deltaX;
                if (errX < 0)
                {
                    errX += deltaMax;
                    x += dirX;
                }

                errY -= deltaY;
                if (errY < 0)
                {
                    errY += deltaMax;
                    y += dirY;
                }

                errZ -= deltaZ;
                if (errZ < 0)
                {
                    errZ += deltaMax;
                    z += dirZ;
                }

                if (!draw)
                {
                    if (x > 0 && y > 0 && x < Program._drawSize[0] * Data.stepsPerMM && y < Program._drawSize[1] * Data.stepsPerMM)
                    {
                        if (Data.depthMap[(y / 50) * 2 * 8 * Data.stepsPerMM + (x / 50)] == UInt16.MaxValue)
                        {
                            draw = true;
                            m = new Line();
                            m.type = l.type;
                            m.acceleration = l.acceleration;
                            m.points = new Vector3[2];
                            m.points[0] = new Vector3(x, y, 0);
                        }
                    }
                }
                else
                {
                    if (x > 0 && y > 0 && x < Program._drawSize[0] * Data.stepsPerMM && y < Program._drawSize[1] * Data.stepsPerMM)
                    {
                        if (Data.depthMap[(y / 50) * 2 * 8 * Data.stepsPerMM + (x / 50)] < UInt16.MaxValue)
                        {
                            draw = false;
                            m.points[1] = new Vector3(x, y, 0);
                            Data.lines.Add(m);
                        }
                    }
                    else
                    {
                        draw = false;
                        m.points[1] = new Vector3(x, y, 0);
                        Data.lines.Add(m);
                    }
                }
            }
            if (draw)
            {
                m.points[1] = new Vector3(x, y, 0);
                Data.lines.Add(m);
            }
            // foreach (Vector3 p in l.points) {
            //     if (Data.depthMap[(int)(p.Y/50) * 2 * 8 * Data.stepsPerMM + (int)(p.X/50)] < UInt16.MaxValue) draw=false;
            // }
            // if (draw) Data.lines.Add(l);
        }
    }

    public void Cube(Vector3 position, float size, Quaternion orientation)
    {
        Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(0.15f, 1f, 0.1f, 10000f);

        // Create and rotate locally

        Vector3 PCube1 = Vector3.Transform(new Vector3(-size * 0.5f, -size * 0.5f, size * 0.5f), orientation) + position;
        Vector3 PCube2 = Vector3.Transform(new Vector3(size * 0.5f, -size * 0.5f, size * 0.5f), orientation) + position;
        Vector3 PCube3 = Vector3.Transform(new Vector3(size * 0.5f, size * 0.5f, size * 0.5f), orientation) + position;
        Vector3 PCube4 = Vector3.Transform(new Vector3(-size * 0.5f, size * 0.5f, size * 0.5f), orientation) + position;
        Vector3 PCube5 = Vector3.Transform(new Vector3(-size * 0.5f, -size * 0.5f, -size * 0.5f), orientation) + position;
        Vector3 PCube6 = Vector3.Transform(new Vector3(size * 0.5f, -size * 0.5f, -size * 0.5f), orientation) + position;
        Vector3 PCube7 = Vector3.Transform(new Vector3(size * 0.5f, size * 0.5f, -size * 0.5f), orientation) + position;
        Vector3 PCube8 = Vector3.Transform(new Vector3(-size * 0.5f, size * 0.5f, -size * 0.5f), orientation) + position;

        // Project

        Vector4 v4P1 = Vector4.Transform(new Vector4(PCube1, 1), projectionMatrix);
        Vector4 v4P2 = Vector4.Transform(new Vector4(PCube2, 1), projectionMatrix);
        Vector4 v4P3 = Vector4.Transform(new Vector4(PCube3, 1), projectionMatrix);
        Vector4 v4P4 = Vector4.Transform(new Vector4(PCube4, 1), projectionMatrix);
        Vector4 v4P5 = Vector4.Transform(new Vector4(PCube5, 1), projectionMatrix);
        Vector4 v4P6 = Vector4.Transform(new Vector4(PCube6, 1), projectionMatrix);
        Vector4 v4P7 = Vector4.Transform(new Vector4(PCube7, 1), projectionMatrix);
        Vector4 v4P8 = Vector4.Transform(new Vector4(PCube8, 1), projectionMatrix);

        float multiplier = 100000;

        Vector3 P1 = new Vector3(v4P1.X * multiplier / v4P1.W, v4P1.Y * multiplier / v4P1.W, (PCube1.Z + 200) * 5) + PaperCenter;
        Vector3 P2 = new Vector3(v4P2.X * multiplier / v4P2.W, v4P2.Y * multiplier / v4P2.W, (PCube2.Z + 200) * 5) + PaperCenter;
        Vector3 P3 = new Vector3(v4P3.X * multiplier / v4P3.W, v4P3.Y * multiplier / v4P3.W, (PCube3.Z + 200) * 5) + PaperCenter;
        Vector3 P4 = new Vector3(v4P4.X * multiplier / v4P4.W, v4P4.Y * multiplier / v4P4.W, (PCube4.Z + 200) * 5) + PaperCenter;
        Vector3 P5 = new Vector3(v4P5.X * multiplier / v4P5.W, v4P5.Y * multiplier / v4P5.W, (PCube5.Z + 200) * 5) + PaperCenter;
        Vector3 P6 = new Vector3(v4P6.X * multiplier / v4P6.W, v4P6.Y * multiplier / v4P6.W, (PCube6.Z + 200) * 5) + PaperCenter;
        Vector3 P7 = new Vector3(v4P7.X * multiplier / v4P7.W, v4P7.Y * multiplier / v4P7.W, (PCube7.Z + 200) * 5) + PaperCenter;
        Vector3 P8 = new Vector3(v4P8.X * multiplier / v4P8.W, v4P8.Y * multiplier / v4P8.W, (PCube8.Z + 200) * 5) + PaperCenter;

        Line l;

        // Vector3 CameraPosition = new Vector3(0, 0, 0);

        bool draw1 = false;
        bool draw2 = false;
        bool draw3 = false;
        bool draw4 = false;
        bool draw5 = false;
        bool draw6 = false;


        // Plane1
        Vector3 worldNormal1 = Vector3.Transform(new Vector3(0, 0, 1), orientation);
        if (Vector3.Dot(Vector3.Normalize(PCube1), worldNormal1) < 0)
        {
            // draw plane 1
            Vector3[] points = { P1, P2, P3, P4 };

            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Single;

            // Draw Lines            
            for (int i = 0; i < points.Length; i++)
            {
                l.points = new Vector3[] { points[i], points[(i + 1) % points.Length] };
                DepthCheckAndAdd(l);
            }

            // float shadeStep = (6.0f / size)*2.0f*Math.Max(0,(1.0f-Vector3.Dot(worldNormal1,SpotLight)));
            // float shadeStep = (1.0f - 0.9f*Math.Max(0,Vector3.Dot(worldNormal1,SpotLight)));
            int shadeStep = Shade2(worldNormal1, size);
            for (int s = 1; s < shadeStep; s++)
            {
                l.points = new Vector3[] { interpolate(P1, P2, s / (float)shadeStep), interpolate(P4, P3, s / (float)shadeStep) };
                DepthCheckAndAdd(l);
            }

            // float shadeStart = (1.0f - (int)(1.0f / shadeStep) * shadeStep)*0.5f;
            // for (float t=shadeStart;t<1.0f;t+=shadeStep) {
            //     l.points = new Vector3[] { interpolate(P1,P2,t),interpolate(P4,P3,t), };
            //     DepthCheckAndAdd(l);
            // }
            draw1 = true;
        }


        // Plane2
        Vector3 worldNormal2 = Vector3.Transform(new Vector3(0, 0, -1), orientation);
        if (Vector3.Dot(Vector3.Normalize(PCube5), worldNormal2) < 0)
        {
            // draw plane 1
            Vector3[] points = { P6, P5, P8, P7 };

            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Single;

            for (int i = 0; i < points.Length; i++)
            {
                l.points = new Vector3[] { points[i], points[(i + 1) % points.Length] };
                DepthCheckAndAdd(l);
            }


            int shadeStep = Shade2(worldNormal2, size);
            for (int s = 1; s < shadeStep; s++)
            {
                l.points = new Vector3[] { interpolate(P6, P5, s / (float)shadeStep), interpolate(P7, P8, s / (float)shadeStep) };
                DepthCheckAndAdd(l);
            }

            // // float shadeStep = (1.0f / size)*2.0f;
            // float shadeStep = Shade(worldNormal2);        
            // float shadeStart = (1.0f - (int)(1.0f / shadeStep) * shadeStep)*0.5f;
            // for (float t=shadeStart;t<1.0f;t+=shadeStep) {
            //     l.points = new Vector3[] { interpolate(P6,P5,t),interpolate(P7,P8,t), };
            //     DepthCheckAndAdd(l);
            // }
            draw2 = true;
        }

        // Plane3
        Vector3 worldNormal3 = Vector3.Transform(new Vector3(1, 0, 0), orientation);
        if (Vector3.Dot(Vector3.Normalize(PCube2), worldNormal3) < 0)
        {
            // draw plane 1
            Vector3[] points = { P2, P6, P7, P3 };

            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Single;

            for (int i = 0; i < points.Length; i++)
            {
                l.points = new Vector3[] { points[i], points[(i + 1) % points.Length] };
                DepthCheckAndAdd(l);
            }

            int shadeStep = Shade2(worldNormal3, size);
            for (int s = 1; s < shadeStep; s++)
            {
                l.points = new Vector3[] { interpolate(P2, P3, s / (float)shadeStep), interpolate(P6, P7, s / (float)shadeStep) };
                DepthCheckAndAdd(l);
            }

            // float shadeStep = (1.0f / size)*2.0f;
            // float shadeStep = Shade(worldNormal3);        
            // float shadeStart = (1.0f - (int)(1.0f / shadeStep) * shadeStep)*0.5f;
            // for (float t=shadeStart;t<1.0f;t+=shadeStep) {
            //     l.points = new Vector3[] { interpolate(P2,P3,t),interpolate(P6,P7,t), };
            //     DepthCheckAndAdd(l);
            // }            
            draw3 = true;
        }


        // Plane4
        Vector3 worldNormal4 = Vector3.Transform(new Vector3(-1, 0, 0), orientation);
        if (Vector3.Dot(Vector3.Normalize(PCube1), worldNormal4) < 0)
        {
            // draw plane 1
            Vector3[] points = { P5, P1, P4, P8 };

            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Single;

            for (int i = 0; i < points.Length; i++)
            {
                l.points = new Vector3[] { points[i], points[(i + 1) % points.Length] };
                DepthCheckAndAdd(l);
            }

            int shadeStep = Shade2(worldNormal4, size);
            for (int s = 1; s < shadeStep; s++)
            {
                l.points = new Vector3[] { interpolate(P1, P4, s / (float)shadeStep), interpolate(P5, P8, s / (float)shadeStep) };
                DepthCheckAndAdd(l);
            }

            // // float shadeStep = (1.0f / size)*2.0f;
            // float shadeStep = Shade(worldNormal4);             
            // float shadeStart = (1.0f - (int)(1.0f / shadeStep) * shadeStep)*0.5f;
            // for (float t=shadeStart;t<1.0f;t+=shadeStep) {
            //     l.points = new Vector3[] { interpolate(P1,P4,t),interpolate(P5,P8,t), };
            //     DepthCheckAndAdd(l);
            // }              
            draw4 = true;
        }

        // Plane5
        Vector3 worldNormal5 = Vector3.Transform(new Vector3(0, -1, 0), orientation);
        if (Vector3.Dot(Vector3.Normalize(PCube1), worldNormal5) < 0)
        {
            // draw plane 1
            Vector3[] points = { P5, P6, P2, P1 };

            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Single;

            for (int i = 0; i < points.Length; i++)
            {

                l.points = new Vector3[] { points[i], points[(i + 1) % points.Length] };
                DepthCheckAndAdd(l);
            }


            int shadeStep = Shade2(worldNormal5, size);
            for (int s = 1; s < shadeStep; s++)
            {
                l.points = new Vector3[] { interpolate(P5, P1, s / (float)shadeStep), interpolate(P6, P2, s / (float)shadeStep) };
                DepthCheckAndAdd(l);
            }

            // float shadeStep = (1.0f / size)*2.0f;
            // float shadeStep = Shade(worldNormal5);      
            // float shadeStart = (1.0f - (int)(1.0f / shadeStep) * shadeStep)*0.5f;
            // for (float t=shadeStart;t<1.0f;t+=shadeStep) {
            //     l.points = new Vector3[] { interpolate(P5,P1,t),interpolate(P6,P2,t) };
            //     DepthCheckAndAdd(l);
            // }                 
            draw5 = true;
        }


        // Plane6
        Vector3 worldNormal6 = Vector3.Transform(new Vector3(0, 1, 0), orientation);
        if (Vector3.Dot(Vector3.Normalize(PCube4), worldNormal6) < 0)
        {
            // draw plane 1
            Vector3[] points = { P4, P3, P7, P8 };
            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Single;
            for (int i = 0; i < points.Length; i++)
            {
                l.points = new Vector3[] { points[i], points[(i + 1) % points.Length] };
                DepthCheckAndAdd(l);
            }

            int shadeStep = Shade2(worldNormal6, size);
            for (int s = 1; s < shadeStep; s++)
            {
                l.points = new Vector3[] { interpolate(P4, P8, s / (float)shadeStep), interpolate(P3, P7, s / (float)shadeStep) };
                DepthCheckAndAdd(l);
            }

            // float shadeStep = (1.0f / size)*2.0f;
            // float shadeStep = Shade(worldNormal6);            
            // float shadeStart = (1.0f - (int)(1.0f / shadeStep) * shadeStep)*0.5f;
            // for (float t=shadeStart;t<1.0f;t+=shadeStep) {
            //     l.points = new Vector3[] { interpolate(P4,P8,t),interpolate(P3,P7,t), };
            //     DepthCheckAndAdd(l);
            // }                
            draw6 = true;
        }


        if (draw1)
        {
            rasterizeToDepthBuffer(new Vector3[] { P1 / 50, P2 / 50, P3 / 50 });
            rasterizeToDepthBuffer(new Vector3[] { P1 / 50, P3 / 50, P4 / 50 });
        }

        if (draw2)
        {
            rasterizeToDepthBuffer(new Vector3[] { P6 / 50, P5 / 50, P8 / 50 });
            rasterizeToDepthBuffer(new Vector3[] { P6 / 50, P8 / 50, P7 / 50 });
        }
        if (draw3)
        {
            rasterizeToDepthBuffer(new Vector3[] { P2 / 50, P6 / 50, P7 / 50 });
            rasterizeToDepthBuffer(new Vector3[] { P2 / 50, P7 / 50, P3 / 50 });
        }
        if (draw4)
        {
            rasterizeToDepthBuffer(new Vector3[] { P5 / 50, P1 / 50, P4 / 50 });
            rasterizeToDepthBuffer(new Vector3[] { P5 / 50, P4 / 50, P8 / 50 });
        }
        if (draw5)
        {
            // Draw to Zbuffer
            rasterizeToDepthBuffer(new Vector3[] { P5 / 50, P6 / 50, P2 / 50 });
            rasterizeToDepthBuffer(new Vector3[] { P5 / 50, P2 / 50, P1 / 50 });
        }
        if (draw6)
        {
            rasterizeToDepthBuffer(new Vector3[] { P4 / 50, P3 / 50, P7 / 50 });
            rasterizeToDepthBuffer(new Vector3[] { P4 / 50, P7 / 50, P8 / 50 });
        }


    }



    public void Generate(int seed, CancellationToken token)
    {
        Data.lines.Clear();
        Data.dots.Clear();

        // Array.Clear(Data.depthMap,0,Data.depthMap.Length);
        Array.Fill(Data.depthMap, UInt16.MaxValue);

        _rand = new Random(seed);

        _rand = new Random(1729113091);

        // for (int i = 0; i < 35; i++) 
        // {
        //     float y = _rand.NextSingle() * 800 - 400;
        //     float z = _rand.NextSingle() * 1400;
        //     Cube(new Vector3(y * 0.25f, (_rand.NextSingle() * 100 - 50) * 0.2f, -z - 550), 25, Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(_rand.NextSingle(), _rand.NextSingle(), _rand.NextSingle())), (float)Math.PI*2*_rand.NextSingle()));
        // }

        // for (int i = 0; i < 115; i++)
        // {
        //     double x = MathNet.Numerics.Distributions.Triangular.Sample(_rand,-150,150,-100);
        //     double y = MathNet.Numerics.Distributions.Triangular.Sample(_rand,-50,50,00);
        //     float z = 0;
        //     Cube(new Vector3((float)x, (float)y * 0.2f, -750), _rand.NextSingle()*20+10, Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(_rand.NextSingle(), _rand.NextSingle(), _rand.NextSingle())),0));// (float)Math.PI*2*_rand.NextSingle()));
        // }
        // Vector3[] points = { P1, P2, P3, P4 };


        for (int i = 0; i < 15; i++)
        {
            // Cube(new Vector3(_rand.NextSingle()*210-105, _rand.NextSingle()*300-150, -750), _rand.NextSingle() * 50 + 10, Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(_rand.NextSingle(), _rand.NextSingle(), _rand.NextSingle())), (float)Math.PI * 2 * _rand.NextSingle()));
            Cube(new Vector3(_rand.NextSingle() * 150 - 75, _rand.NextSingle() * 200 - 100, -750), _rand.NextSingle() * 100 + 10, Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1, 0, 1)), (float)Math.PI * 2 * _rand.NextSingle()));
            // Cube(new Vector3(_rand.NextSingle()*210-105, _rand.NextSingle()*300-150, -750), _rand.NextSingle() * 50 + 10, Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(0,1,0)), 0.4f));
        }



        // float X1 = 0;
        // float X2 = 0;
        // float lstep = 10000;
        // Line l;

        // l = new Line();
        // l.type = LineType.Straight;
        // l.acceleration = Acceleration.Single;

        // for (int k = 0; k < 10; k++)
        // {
        //     float y = _rand.NextSingle() * 25 * Data.stepsPerMM + 35 * Data.stepsPerMM;
        //     l.points = new Vector3[] { new Vector3(20 * Data.stepsPerMM, y, 0), new Vector3(466 * Data.stepsPerMM, y, 0) };
        //     DepthCheckAndAdd(l);

        // }


        // l.points = new Vector3[] { new Vector3(30*Data.stepsPerMM,60800+25000,0), new Vector3(456*Data.stepsPerMM,60800-25000,0)};
        // DepthCheckAndAdd(l);


        // l.points = new Vector3[] { new Vector3(30*Data.stepsPerMM,60800+40000,0), new Vector3(456*Data.stepsPerMM,60800-20000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(30*Data.stepsPerMM,60800+000,0), new Vector3(456*Data.stepsPerMM,60800-40000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800+20000,0), new Vector3(486*Data.stepsPerMM,60800+10000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800-20000,0), new Vector3(486*Data.stepsPerMM,60800-10000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800+40000,0), new Vector3(486*Data.stepsPerMM,60800+20000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800-40000,0), new Vector3(486*Data.stepsPerMM,60800-20000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800+60000,0), new Vector3(486*Data.stepsPerMM,60800+30000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800-60000,0), new Vector3(486*Data.stepsPerMM,60800-30000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800+80000,0), new Vector3(486*Data.stepsPerMM,60800+40000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800-80000,0), new Vector3(486*Data.stepsPerMM,60800-40000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800+100000,0), new Vector3(486*Data.stepsPerMM,60800+50000,0)};
        // DepthCheckAndAdd(l);

        // l.points = new Vector3[] { new Vector3(0,60800-100000,0), new Vector3(486*Data.stepsPerMM,60800-50000,0)};
        // DepthCheckAndAdd(l);


    }
}