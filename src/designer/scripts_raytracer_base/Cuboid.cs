using System;
using System.Numerics;

namespace RayTracer
{
    public struct Cuboid
    {
        public Vector3 Center;
        public Vector3 Size;
        public Quaternion Orientation;
        public Material Material;
        public Quad[] Quads;
        public int ID;
        // public Vector3[] Vertices;

        public static Cuboid Create(Vector3 center, Vector3 size, Quaternion orientation, Material m, int ID=-1)
        {
            Cuboid c = new Cuboid();
            c.Center = center;
            c.Size = size;
            c.Orientation = orientation;
            c.Material = m;

            Vector3 P1 = Vector3.Transform(new Vector3(-size.X * 0.5f, -size.Y * 0.5f, size.Z * 0.5f), orientation) + center;
            Vector3 P2 = Vector3.Transform(new Vector3(size.X * 0.5f, -size.Y * 0.5f, size.Z * 0.5f), orientation) + center;
            Vector3 P3 = Vector3.Transform(new Vector3(size.X * 0.5f, size.Y * 0.5f, size.Z * 0.5f), orientation) + center;
            Vector3 P4 = Vector3.Transform(new Vector3(-size.X * 0.5f, size.Y * 0.5f, size.Z * 0.5f), orientation) + center;
            Vector3 P5 = Vector3.Transform(new Vector3(-size.X * 0.5f, -size.Y * 0.5f, -size.Z * 0.5f), orientation) + center;
            Vector3 P6 = Vector3.Transform(new Vector3(size.X * 0.5f, -size.Y * 0.5f, -size.Z * 0.5f), orientation) + center;
            Vector3 P7 = Vector3.Transform(new Vector3(size.X * 0.5f, size.Y * 0.5f, -size.Z * 0.5f), orientation) + center;
            Vector3 P8 = Vector3.Transform(new Vector3(-size.X * 0.5f, size.Y * 0.5f, -size.Z * 0.5f), orientation) + center;

            c.Quads = new Quad[6];

            // sides
            c.Quads[0] = Quad.Create(P1, P2-P1, P4-P1, m, ID);
            c.Quads[1] = Quad.Create(P3, P2-P3, P7-P3, m, ID);
            c.Quads[2] = Quad.Create(P6, P5-P6, P7-P6, m, ID);
            c.Quads[3] = Quad.Create(P8, P5-P8, P4-P8, m, ID);
            c.Quads[4] = Quad.Create(P3, P7-P3, P4-P3, m, ID);
            c.Quads[5] = Quad.Create(P1, P5-P1, P2-P1, m, ID);

            return c;    
        }    
    }
}
