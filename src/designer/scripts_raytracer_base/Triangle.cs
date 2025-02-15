using System;
using System.Numerics;
using MathNet.Numerics.Distributions;

namespace RayTracer
{
    public struct Triangle : IShape
    {
        public Vector3 Center { get; set; }
        public Vector3 Rotation { get; set; }
        public Material Material { get; set; }

        public float Radius { get; set; }

        public Vector3 Corner { get; set; } // Q bottom left
        public Vector3 sideHorizontal { get; set; } // u
        public Vector3 sideVertical { get; set; } // v

        public Vector3 normal { get; set; }
        public float D { get; set; }
        public Vector3 w { get; set; }

        public Vector3[] Vertices;
        public int parentID { get; set; }

        public static Triangle Create(Vector3 corner, Vector3 side_u, Vector3 side_v, Material m, int parentID = -1)
        {
            Triangle t = new Triangle();

            t.Corner = corner;
            t.sideHorizontal = side_u;
            t.sideVertical = side_v;

            Vector3 n = Vector3.Cross( t.sideVertical, t.sideHorizontal);
            t.normal = Vector3.Normalize(n);
            t.D = Vector3.Dot(t.Corner, t.normal);

            t.w = n / Vector3.Dot(n, n); // constant for the plane - frame equation 

            t.Material = m;

            t.Vertices = new Vector3[3];

            t.Vertices[0] = t.Corner;
            t.Vertices[1] = t.Corner + t.sideHorizontal;
            t.Vertices[2] = t.Corner + t.sideVertical;

            t.Center = (t.Vertices[0] + t.Vertices[2]) * 0.5f;

            t.parentID = parentID;
            return t;
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

            // calculate the hit point with the plane and check if it is inside the triangle
            Vector3 hitPoint = Ray.PointAt(ray, t);

            Vector3 v0 = Vertices[1] - Vertices[0];
            Vector3 v1 = Vertices[2] - Vertices[0];
            Vector3 v2 = hitPoint - Vertices[0];

            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if the point is inside the triangle
            if (u >= 0 && v >= 0 && (u + v) <= 1)
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
        // Vector3 N = Vector3.Cross(B - A, C - A).normalized;
        // float dotRN = Vector3.Dot(R.direction, N);
        // if (dotRN >= 0) return false;

        // Vector3 P = R.origin + R.direction * Vector3.Dot(N, A - R.origin) / dotRN;

        // Vector3 v0 = C - A;
        // Vector3 v1 = B - A;
        // Vector3 v2 = P - A;

        // float dot00 = Vector3.Dot(v0, v0);
        // float dot01 = Vector3.Dot(v0, v1);
        // float dot02 = Vector3.Dot(v0, v2);
        // float dot11 = Vector3.Dot(v1, v1);
        // float dot12 = Vector3.Dot(v1, v2);

        // float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        // float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        // float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // return (u >= 0) && (v >= 0) && (u + v < 1);