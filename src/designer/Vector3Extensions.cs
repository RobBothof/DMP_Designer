using System.Numerics;

public static class Vector3Extensions
{
    public static Vector2 XY(this Vector3 aVector)
    {
        return new Vector2(aVector.X, aVector.Y);
    }
    public static Vector2 XZ(this Vector3 aVector)
    {
        return new Vector2(aVector.X, aVector.Z);
    }
    public static Vector2 YZ(this Vector3 aVector)
    {
        return new Vector2(aVector.Y, aVector.Z);
    }
    public static Vector2 YX(this Vector3 aVector)
    {
        return new Vector2(aVector.Y, aVector.X);
    }
    public static Vector2 ZX(this Vector3 aVector)
    {
        return new Vector2(aVector.Z, aVector.X);
    }
    public static Vector2 ZY(this Vector3 aVector)
    {
        return new Vector2(aVector.Z, aVector.Y);
    }
}