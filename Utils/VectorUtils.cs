using System.Windows;
using System.Windows.Media.Media3D;

namespace WorldCompanyDataViewer.Utils
{
    public static class VectorUtils
    {
        public static Vector3D PolarDegreesToCartesian(Vector lonLat)
        {
            return PolarRadiansToCartesian(lonLat.DegreeToRad());
        }

        //TODO consider unit Tests to verfiy
        public static Vector3D PolarRadiansToCartesian(Vector lonLat)
        {
            double latCos = Math.Cos(lonLat.Y);
            return new Vector3D(
                latCos * Math.Cos(lonLat.X),
                latCos * Math.Sin(lonLat.X),
                Math.Sin(lonLat.Y)
                );
        }

        public static Vector CartersianToPolarDegrees(Vector3D lonLat)
        {
            return CartersianToPolarRadians(lonLat).RadToDegree();
        }


        //TODO consider unit Tests to verfiy
        public static Vector CartersianToPolarRadians(Vector3D cart)
        {
            double x = cart.X;
            double y = cart.Y;
            double z = cart.Z;
            double r = Math.Sqrt(x * x + y * y + z * z);
            return new Vector(
                Math.Atan2(y, x),
                Math.Asin(z / r)
                );
        }

        //TODO consider min and max switched
        public static Vector RandomRange(double minX, double maxX, double minY, double maxY)
        {
            double scaleX = maxX - minX;
            double scaleY = maxY - minY;
            return new Vector(
                Random.Shared.NextDouble() * scaleX + minX,
                Random.Shared.NextDouble() * scaleY + minY
                );
        }
    }
}
