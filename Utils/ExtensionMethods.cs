using System.Windows;
using System.Windows.Media.Media3D;

namespace WorldCompanyDataViewer.Utils
{
    internal static class ExtensionMethods
    {

        //TODO Consider in-place conversion
        public static Vector RadToDegree(this Vector v)
        {
            return new Vector(double.RadiansToDegrees(v.X), double.RadiansToDegrees(v.Y));
        }

        //TODO Consider in-place conversion
        public static Vector DegreeToRad(this Vector v)
        {
            return new Vector(double.DegreesToRadians(v.X), double.DegreesToRadians(v.Y));
        }

        public static double DistanceSq(this Vector3D v1, Vector3D v2)
        {
            return (v2 - v1).LengthSquared;
        }

        public static Vector3D Average(this IEnumerable<Vector3D> vectors)
        {
            if (vectors == null || !vectors.Any())
            {
                throw new InvalidOperationException("Cannot compute the average of an empty collection.");
            }

            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            int count = 0;

            foreach (var vector in vectors)
            {
                sumX += vector.X;
                sumY += vector.Y;
                sumZ += vector.Z;
                count++;
            }

            return new Vector3D(sumX / count, sumY / count, sumZ / count);
        }
    }
}
