using System.Numerics;

namespace PathTracer
{
    public class RandomRobber
    {
        private uint _state;
        public RandomRobber(uint seed)
        {
            _state = seed;
        }
    
        public void SetSeed(uint seed)
        {
            _state = seed;
        }

        public void XorShift()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 15;
        }

        public float RandomFloat()
        {
            XorShift();
            return _state * (1f / 4294967296f);
        }

        public Vector3 RandomInUnitDisk()
        {
            Vector3 p;
            do
            {
                p = 2f * new Vector3(RandomFloat(), RandomFloat(), 0) - new Vector3(1, 1, 0);
            } while (Vector3.Dot(p, p) >= 1f);
            return p;
        }

        public Vector3 RandomInUnitSphere()
        {
            Vector3 ret;
            do
            {
                ret = 2f * new Vector3(RandomFloat(), RandomFloat(), RandomFloat()) - Vector3.One;
            } while (ret.LengthSquared() >= 1f);
            return ret;
        }

        public Vector3 RandomUnitVector()
        {
            Vector3 v =  2f * new Vector3(RandomFloat(), RandomFloat(), RandomFloat()) - Vector3.One;
            return Vector3.Normalize(v);
        }

        public Vector3 RandomOnHemisphere(Vector3 normal)
        {
            Vector3 on_unit_sphere = RandomUnitVector();
            if (Vector3.Dot(on_unit_sphere, normal) > 0)
            {
                return on_unit_sphere;
            }
            else
            {
                return -on_unit_sphere;
            }
        }

    }
}
