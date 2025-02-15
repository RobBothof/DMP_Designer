using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Material
    {
        public float Albedo;
        public float Emission;
        public MaterialType Type;
        public float FuzzOrRefIndex;
        public float _padding0;
        public float _padding1;
        public float _padding2;

        public static Material Lambertian(float albedo)
        {
            Material m = new Material(); 
            m.Type = MaterialType.Lambertian;
            m.Albedo = albedo;
            m.Emission = 0;
            m.FuzzOrRefIndex = 0;
            m._padding0 = m._padding1 = m._padding2 = 0;
            return m;
        }

        public static Material Metal(float albedo, float fuzz)
        {
            Material m = new Material(); 
            m.Type = MaterialType.Metal;
            m.Albedo = albedo;
            m.Emission = 0;
            m.FuzzOrRefIndex = fuzz;
            m._padding0 = m._padding1 = m._padding2 = 0;
            return m;
        }

        public static Material Dielectric(float refIndex)
        {
            Material m = new Material(); 
            m.Type = MaterialType.Dielectric;
            m.Albedo = 0;
            m.Emission = 0;
            m.FuzzOrRefIndex = refIndex;
            m._padding0 = m._padding1 = m._padding2 = 0;
            return m;
        }

        public static Material Emissive(float emission)
        {
            Material m = new Material(); 
            m.Type = MaterialType.Emissive;
            m.Albedo = 0;
            m.Emission = emission;
            m.FuzzOrRefIndex = 0;
            m._padding0 = m._padding1 = m._padding2 = 0;
            return m;
        }
    }
}
