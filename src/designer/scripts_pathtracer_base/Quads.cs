using System;
using System.Numerics;
using MathNet.Numerics.Distributions;

namespace PathTracer
{
    public struct Quad : IShape
    {
        public Vector3 Center { get; set; }
        public float Radius { get; set; }

        public Vector3 Corner { get; set; } // Q bottom left
        public Vector3 sideHorizontal { get; set; } // u
        public Vector3 sideVertical { get; set; } // v

        public Vector3 normal { get; set; }
        public float D { get; set; }
        public Vector3 w { get; set; }

        public static Quad Create(Vector3 corner, Vector3 side_u, Vector3 side_v)
        {
            Quad q = new Quad();

            q.Corner = corner;
            q.sideHorizontal = side_u;
            q.sideVertical = side_v;

            Vector3 n = Vector3.Cross(q.sideHorizontal, q.sideVertical);
            q.normal = Vector3.Normalize(n);
            q.D = Vector3.Dot(q.Corner, q.normal);

            q.w = n / Vector3.Dot(n, n); // contant for the plane - frame equation 

            return q;
        }


        public bool Hit(Ray ray, float tMin, float tMax, out RayHit hit)
        {
            float denom = Vector3.Dot(normal, ray.Direction);

            hit = new RayHit();

            if (Math.Abs(denom) < 1e-8)
            {

                return false;
            }

            float t = (D - Vector3.Dot(normal, ray.Origin)) / denom;

            // Return false if the hit point parameter t is outside the ray interval.
            if (t < tMin || t > tMax)
            {

                return false;
            }

            // calculate the hit point with the plane and check if it is inside the quad
            Vector3 hitPoint = Ray.PointAt(ray, t);
            Vector3 planarTohitPointVector = hitPoint - Corner;
            float a = Vector3.Dot(w, Vector3.Cross(planarTohitPointVector, sideVertical));
            float b = Vector3.Dot(w, Vector3.Cross(sideHorizontal, planarTohitPointVector));

            if (a >= 0 && a <= 1 && b >= 0 && b <= 1)
            {
                hit.Position = hitPoint;
                hit.Normal = normal;
                hit.T = t;
                return true;
            }

            return false;

        }
    }
}