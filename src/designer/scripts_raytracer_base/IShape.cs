using System.Numerics;

namespace RayTracer
{
    public interface IShape
    {
        Vector3 Center { get; }
        bool Hit(Ray ray, float tMin, float tMax, out RayHit hit);
    }
}