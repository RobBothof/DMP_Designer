﻿using System.Numerics;
using System.Runtime.InteropServices;

namespace RayTracer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RayHit
    {
        public Vector3 Position;
        public float T;
        public Vector3 Normal;

        public RayHit(Vector3 position, float t, Vector3 normal)
        {
            Position = position;
            T = t;
            Normal = normal;
        }
    }
}