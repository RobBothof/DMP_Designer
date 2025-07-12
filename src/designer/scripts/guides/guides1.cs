using System;
using System.Numerics;
using System.Collections.Generic;
using Designer;
using MathNet.Numerics;
using RayTracer;

using System.Threading;
using System.Threading.Tasks;

public class Guides : IGenerator
{
    public const uint NumSamples = 1;

    private List<IShape> _shapes;
    private List<Light> _lights;
    private List<Cuboid> _cuboids;
    // private Material[] _materials;

    Vector3 paper;
    Vector3 paperCenter;

    private RandomRobber _rng;

    public void Generate(int seed, CancellationToken token)
    {
        paper = new Vector3(Data.paperSize.X * Data.stepsPerMM, Data.paperSize.Y * Data.stepsPerMM, 0);
        paperCenter = paper * 0.5f;

        Data.lines.Clear();
        Data.dots.Clear();

        Line l;

        for (int y = 0; y < 121; y += 5)
        {
            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Start;
            l.points = new Vector3[] { new Vector3(0, y * 12800, 0), new Vector3(paper.X, y * 12800, 0) };
            Data.lines.Add(l);
        }

        for (int x = 0; x < 86; x += 5)
        {
            l = new Line();
            l.type = LineType.Straight;
            l.acceleration = Acceleration.Start;
            l.points = new Vector3[] { new Vector3(x * 12800, 0, 0), new Vector3(x * 12800, paper.Y, 0) };
            Data.lines.Add(l);
        }



    }
}