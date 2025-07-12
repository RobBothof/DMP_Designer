using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RayTracer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Camera
    {
        public Vector3 Origin;
        public Vector3 LowerLeftCorner;
        public Vector3 Horizontal;
        public Vector3 Vertical;
        public Vector3 U;
        public float LensRadius;
        public Vector3 V;
        public Vector3 W;
        private float _padding0;
        private float _padding1;
        private float _padding2;
        private float _padding3;
        private float _padding4;
        private float _padding5;
        public Vector3 normal;
        public float focalLength;

        public static Camera Create(Vector3 origin, Vector3 lookAt, Vector3 up, float vfov, float aspect, float aperture, float focusDist)
        {
            Camera cam;
            cam.LensRadius = aperture / 2f;
            float theta = vfov * MathF.PI / 180f;
            float halfHeight = MathF.Tan(theta / 2f);
            float halfWidth = aspect * halfHeight;
            cam.Origin = origin;
            cam.W = Vector3.Normalize(origin - lookAt);
            cam.U = Vector3.Normalize(Vector3.Cross(up, cam.W));
            cam.V = Vector3.Cross(cam.W, cam.U);
            cam.LowerLeftCorner = cam.Origin - halfWidth * focusDist * cam.U - halfHeight * focusDist * cam.V - focusDist * cam.W;
            cam.Horizontal = 2 * halfWidth * focusDist * cam.U;
            cam.Vertical = 2 * halfHeight * focusDist * cam.V;

            cam._padding0 = cam._padding1 = cam._padding2 = cam._padding3 = cam._padding4 = cam._padding5 = 0;

            cam.focalLength = 1.0f / halfWidth;
            cam.normal = Vector3.Normalize(lookAt - origin);
            return cam;
        }

        public static Ray GetRay(Camera cam, float s, float t, Vector3 RandomVectorInUnitDisk)
        {
            Vector3 rd = cam.LensRadius * RandomVectorInUnitDisk;
            Vector3 offset = cam.U * rd.X + cam.V * rd.Y;
            return Ray.Create(cam.Origin + offset, cam.LowerLeftCorner + s * cam.Horizontal + t * cam.Vertical - cam.Origin - offset);
        }

        public static Vector3 Project(Camera cam, Vector3 p)
        {
            // Transform the world position to camera space
            Vector3 cameraSpacePos = p - cam.Origin;
            float x = Vector3.Dot(cameraSpacePos, cam.U);
            float y = Vector3.Dot(cameraSpacePos, cam.V);
            float z = Vector3.Dot(cameraSpacePos, cam.W);

            // Ensure z is not zero to avoid division by zero
            if (z == 0) return Vector3.Zero;

            float projectedX = (x / z) * cam.focalLength * -0.5f;
            float projectedY = (y / z) * cam.focalLength * -0.5f;

            // Map to the camera's 2D plane
            return new Vector3(projectedX, projectedY, 0);
            // return new Vector3(x, y, z);            
        }
    }
}
