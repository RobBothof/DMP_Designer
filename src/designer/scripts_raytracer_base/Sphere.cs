using System;
using System.Numerics;

namespace RayTracer
{
    public struct Sphere : IShape
    {
        public Vector3 Center { get; set; }
        public float Radius { get; set; }

        public static Sphere Create(Vector3 center, float radius)
        {
            return new Sphere
            {
                Center = center,
                Radius = radius
            };
        }

        public bool Hit(Ray ray, float tMin, float tMax, out RayHit hit)
        {
            Vector3 center = Center;
            Vector3 oc = ray.Origin - center;
            Vector3 rayDir = ray.Direction;
            float a = Vector3.Dot(rayDir, rayDir);
            float b = Vector3.Dot(oc, rayDir);
            float radius = Radius;
            float c = Vector3.Dot(oc, oc) - radius * radius;
            float discriminant = b * b - a * c;
            if (discriminant > 0)
            {
                float tmp = MathF.Sqrt(b * b - a * c);
                float t = (-b - tmp) / a;
                if (t < tMax && t > tMin)
                {
                    Vector3 position = Ray.PointAt(ray, t);
                    Vector3 normal = (position - center) / radius;
                    hit = RayHit.Create(Ray.PointAt(ray, t), t, normal);
                    return true;
                }
                t = (-b + tmp) / a;
                if (t < tMax && t > tMin)
                {
                    Vector3 position = Ray.PointAt(ray, t);
                    Vector3 normal = (position - center) / radius;
                    hit = RayHit.Create(position, t, normal);
                    return true;
                }
            }

            hit.Position = new Vector3();
            hit.Normal = new Vector3();
            hit.T = 0;
            return false;
        }
    }
}