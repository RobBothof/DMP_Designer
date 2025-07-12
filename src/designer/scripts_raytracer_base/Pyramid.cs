using System;
using System.Numerics;

namespace RayTracer
{
    public struct Pyramid
    {
        public Vector3 Center;
        public Vector3 Size;
        // public Quaternion Orientation;
        public Material Material;
        public Triangle[] Triangles;
        // public float Height;
        // public Vector3[] Base;
        public int ID;
        public Vector3[] Vertices;

        public static Pyramid Create(Vector3 center, Vector3 size, Vector3 forward, Vector3 up, Material m, int ID = -1)
        {
            Pyramid p = new Pyramid();
            p.Center = center;
            p.Material = m;
            Vector3 PForward = Vector3.Normalize(forward);
            Vector3 PRight = Vector3.Normalize(Vector3.Cross(up, forward));
            Vector3 PUp = Vector3.Normalize(Vector3.Cross(PForward, PRight));

            Vector3 P1 = center + PUp * size[0];
            Vector3 P2 = center + PRight * size[1];
            Vector3 P3 = center - PUp * size[0];
            Vector3 P4 = center - PRight * size[1];
            Vector3 P5 = center + PForward * size[2];

            // Vector3 P1 = Vector3.Transform(baseVertices[0], p.Orientation ) + center;
            // Vector3 P2 = Vector3.Transform(baseVertices[1], p.Orientation ) + center;
            // Vector3 P3 = Vector3.Transform(baseVertices[2], p.Orientation ) + center;
            // Vector3 P4 = Vector3.Transform(baseVertices[3], p.Orientation ) + center;

            // Vector3 baseCenter = Vector3.Lerp(baseVertices[0],baseVertices[2],0.5f);
            // Vector3 baseNormal = Vector3.Normalize(Vector3.Cross(baseVertices[1]-baseVertices[0],baseVertices[0]-baseVertices[2]));
            // Vector3 tip = baseCenter + baseNormal * height;

            // Vector3 P5 = Vector3.Transform(tip, p.Orientation ) + center;

            p.Vertices = new Vector3[] { P1, P2, P3, P4, P5 };

            p.Triangles = new Triangle[6];

            // floor triangles
            p.Triangles[0] = Triangle.Create(P1, P4 - P1, P2 - P1, m, ID);
            p.Triangles[1] = Triangle.Create(P3, P2 - P3, P4 - P3, m, ID);

            // side triangles
            p.Triangles[2] = Triangle.Create(P5, P1 - P5, P2 - P5, m, ID);
            p.Triangles[3] = Triangle.Create(P5, P2 - P5, P3 - P5, m, ID);
            p.Triangles[4] = Triangle.Create(P5, P3 - P5, P4 - P5, m, ID);
            p.Triangles[5] = Triangle.Create(P5, P4 - P5, P1 - P5, m, ID);

            return p;
        }
    }
}
