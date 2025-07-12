using System.Numerics;

namespace RayTracer
{
    /// <summary>
    /// Represents a triangle polygon used for both line drawing and ray tracing.
    /// The vertices should be ordered counterclockwise when viewing the front face.
    /// <code>     
    /// 1 - 0
    /// | / 
    /// 2     
    /// </code>
    /// </summary>
    public struct Polygon
    {
        public Vector3 Center { get; set; }
        public Vector3[] Vertices { get; set; }
        public Vector3 Normal { get; set; }
        public bool[] HiddenEdges { get; set; }
        public int ParentID { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> struct with the specified vertices and parent ID.
        /// </summary>
        /// <param name="vertices">The vertices of the polygon (must be coplanar and ordered counterclockwise).</param>
        /// <param name="parentID">The parent ID. Defaults to -1 if not specified.</param>
        public Polygon(Vector3[] vertices, int parentID = -1)
        {
            if (vertices == null || vertices.Length < 3)
                throw new ArgumentException("There must be at least 3 vertices.", nameof(vertices));

            Vertices = vertices;
            ParentID = parentID;

            // Compute the center as the average of all vertices.
            Vector3 center = Vector3.Zero;
            foreach (var v in vertices)
                center += v;
            Center = center / vertices.Length;

            // Compute the normal using Newell's method.
            Vector3 normal = Vector3.Zero;
            int count = vertices.Length;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                normal.X += (vertices[i].Y - vertices[next].Y) * (vertices[i].Z + vertices[next].Z);
                normal.Y += (vertices[i].Z - vertices[next].Z) * (vertices[i].X + vertices[next].X);
                normal.Z += (vertices[i].X - vertices[next].X) * (vertices[i].Y + vertices[next].Y);
            }
            Normal = Vector3.Normalize(normal);
        }

        /// <summary>
        /// Checks if the given ray hits the polygon. The ray is first tested against the plane defined by the polygon,
        /// where tMin and tMax thresholds can be used to limit the ray's range.
        //  If the ray hits the plane, an inside-polygon test is performed.
        /// </summary>
        /// <param name="ray">The ray used for the intersection test.</param>
        /// <param name="tMin">Specifies the minimum distance along the ray for a valid hit.</param>
        /// <param name="tMax">Specifies the maximum distance along the ray for a valid hit.</param>
        /// <param name="hit">The hit information if an intersection occurs.</param>
        /// <returns><c>true</c> if the ray hits the polygon; otherwise, <c>false</c>.</returns>
        public bool Hit(Ray ray, float tMin, float tMax, out RayHit hit)
        {
            hit = new RayHit();

            // Plane intersection:
            float denom = Vector3.Dot(Normal, ray.Direction);
            if (Math.Abs(denom) < 1e-6) // Ray is parallel to the plane.
                return false;

            float t = Vector3.Dot(Center - ray.Origin, Normal) / denom;
            if (t < tMin || t > tMax)
                return false;

            Vector3 p = ray.Origin + ray.Direction * t;

            // Inside-polygon test using edge-cross products. Assumes convex polygon.
            for (int i = 0, count = Vertices.Length; i < count; i++)
            {
                int next = (i + 1) % count;
                Vector3 edge = Vertices[next] - Vertices[i];
                Vector3 toPoint = p - Vertices[i];
                Vector3 c = Vector3.Cross(edge, toPoint);
                // If point is to the right of any edge, it's outside (assuming CCW winding).
                if (Vector3.Dot(c, Normal) < 0)
                    return false;
            }

            hit = new RayHit(p, t, Normal);
            return true;
        }
    }
}