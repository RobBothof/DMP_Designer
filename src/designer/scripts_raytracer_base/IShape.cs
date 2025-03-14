using System.Numerics;

namespace RayTracer
{
    public interface IShape
    {
        Vector3 Center { get; }
        Vector3 Rotation { get; }
        Material Material { get; }
        int parentID { get; }
        bool Hit(Ray ray, float tMin, float tMax, out RayHit hit);
    }
}