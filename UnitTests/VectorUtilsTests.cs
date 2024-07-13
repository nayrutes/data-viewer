using System.Windows;
using System.Windows.Media.Media3D;
using WorldCompanyDataViewer.Utils;

namespace UnitTests
{
    public class VectorUtilsTests
    {
        //TODO add random range test with min max switched
        //TODO add test for polar-cartesian coordinate transformation around poles

        [Fact]
        public void RandomRangeTest()
        {
            for (int i = 0; i < 20; i++)
            {
                double minX = -10;
                double minY = -10;
                double maxX = 10;
                double maxY = 10;

                Vector randomVector = VectorUtils.RandomRange(minX, maxX, minY, maxY);
                Assert.True(randomVector.X > minX);
                Assert.True(randomVector.X < maxX);
                Assert.True(randomVector.Y > minY);
                Assert.True(randomVector.Y < maxY);
            }
        }
        [Fact]
        public void ConvertRadianPolarToCartesianAndBack()
        {
            double tolerance = 0.00001;
            Vector radiansVec = new Vector(-0.01819, 0.9343682);
            Vector3D cart = VectorUtils.PolarRadiansToCartesian(radiansVec);
            Vector restRadiansVec = VectorUtils.CartersianToPolarRadians(cart);
            Assert.True(AreAlmostEqual(radiansVec, restRadiansVec, tolerance));
        }
        [Fact]
        public void ConvertDegreePolarToCartesianAndBack()
        {
            double tolerance = 0.001;
            Vector degreesVec = new Vector(-1.042, 53.530);
            Vector3D cart = VectorUtils.PolarDegreesToCartesian(degreesVec);
            Vector restDegreesVec = VectorUtils.CartersianToPolarDegrees(cart);
            Assert.True(AreAlmostEqual(degreesVec, restDegreesVec, tolerance));

            tolerance = 0.00001;
            degreesVec = new Vector(-1.04242, 53.53535);
            cart = VectorUtils.PolarDegreesToCartesian(degreesVec);
            restDegreesVec = VectorUtils.CartersianToPolarDegrees(cart);
            Assert.True(AreAlmostEqual(degreesVec, restDegreesVec, tolerance));

            tolerance = 0.0000001;
            degreesVec = new Vector(-1.0424242, 53.5353535);
            cart = VectorUtils.PolarDegreesToCartesian(degreesVec);
            restDegreesVec = VectorUtils.CartersianToPolarDegrees(cart);
            Assert.True(AreAlmostEqual(degreesVec, restDegreesVec, tolerance));
        }
        [Fact]
        public void RandomConvertDegreePolarToCartesianAndBack()
        {
            double tolerance = 0.00001;
            for (int i = 0; i < 20; i++)
            {
                Vector randomLonLat = VectorUtils.RandomRange(-180, 180, -90, 90);
                Vector3D cart = VectorUtils.PolarDegreesToCartesian(randomLonLat);
                Vector restLonLat = VectorUtils.CartersianToPolarDegrees(cart);
                Assert.True(AreAlmostEqual(randomLonLat, restLonLat, tolerance));
            }
        }



        public static bool AreAlmostEqual(Vector3D expected, Vector3D actual, double tolerance)
        {
            return Math.Abs(expected.X - actual.X) < tolerance &&
                   Math.Abs(expected.Y - actual.Y) < tolerance &&
                   Math.Abs(expected.Z - actual.Z) < tolerance;
        }

        public static bool AreAlmostEqual(Vector expected, Vector actual, double tolerance)
        {
            return Math.Abs(expected.X - actual.X) < tolerance &&
                   Math.Abs(expected.Y - actual.Y) < tolerance;
        }
    }
}
