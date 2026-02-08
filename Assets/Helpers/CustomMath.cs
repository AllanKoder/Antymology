using UnityEngine;

namespace Antymology.Helpers
{
    public static class CustomMath
    {
        /// <summary>
        /// Faster version of Int.Floor 
        /// </summary>
        public static int fastfloor(double x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }

        /// <summary>
        /// Returns the dot product of a vector4 with a vector3.
        /// </summary>
        public static double dot(Vector4 g, Vector3 prod)
        {
            return dot(g, prod.x, prod.y, prod.z);
        }

        /// <summary>
        /// Returns the dot product of a vector4 with an x, y, and z.
        /// </summary>
        public static double dot(Vector4 g, double x, double y, double z)
        {
            return g.x * x + g.y * y + g.z * z;
        }
    }
}
