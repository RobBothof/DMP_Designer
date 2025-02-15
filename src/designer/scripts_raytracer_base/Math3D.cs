using System;
using System.Numerics;

namespace RayTracer
{
    public static class Math3D
    {
        public static Quaternion QuaternionFromDirection(Vector3 forward_direction)
        {
            Vector3 forward = Vector3.Normalize(forward_direction);
            Vector3 up = Vector3.UnitY;

            if (Vector3.Dot(forward, up) > 0.9999f)
            {
                up = Vector3.UnitZ;
            }

            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);

            Matrix4x4 rotationMatrix = new Matrix4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                forward.X, forward.Y, forward.Z, 0,
                0, 0, 0, 1
            );

            return Quaternion.CreateFromRotationMatrix(rotationMatrix);
        }
    }
}