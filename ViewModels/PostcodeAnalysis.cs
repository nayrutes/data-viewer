using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Services;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.ViewModels
{
    internal class PostcodeAnalysis
    {
        public List<PostcodeGeodata> PostcodeLocations { get; private set; } = new();
        public readonly IPostcodeLocationService postcodeLocationService;

        public PostcodeAnalysis(IPostcodeLocationService postcodeLocationService)
        {
            this.postcodeLocationService = postcodeLocationService;
        }

        //TODO consider caching PostcodeLocations
        public async Task<List<(decimal, decimal)>> AnalyzePostcodes(DataEntryContext context)
        {
            Dictionary<string, int> postcodesCounts = await GetAllPostCodesWithCount(context);

            List<string> postcodes = postcodesCounts.Keys.ToList();

            PostcodeLocations = await postcodeLocationService.RequestPostcodeLocationsAsync(postcodes);
            return KMeansClustering(3, PostcodeLocations, 20);
        }

        private async Task<Dictionary<string, int>> GetAllPostCodesWithCount(DataEntryContext context)
        {
            if (context == null)
            {
                return null;
            }
            Dictionary<string, int> result = await context.DataEntries.GroupBy(t => t.Postal)
                .Select(g => new
                {
                    Value = g.Key,
                    Count = g.Count()
                }).ToDictionaryAsync(x => x.Value!, x => x.Count);
            return result;
        }


        //TODO investigate if inaccuracy of Haversine Distance matters (assumption the earth is a spehere and not an ellipoid)
        //public static double DistanceBetweenGeodata(PostcodeGeodata d1, PostcodeGeodata d2)
        //{
        //    return HaversineDistance.DistanceBetweenPlaces(decimal.ToDouble(d1.Longitude), decimal.ToDouble(d1.Latitude), decimal.ToDouble(d2.Longitude), decimal.ToDouble(d2.Latitude));
        //}

        //TODO use weighted postcodes
        //TODO consider using System.Numerics.Vector3 with float precision for faster calculation or decimal for more accuaracy
        //TODO use uint when applicable
        //TODO consider directly using database for large data sets (e.g. save cartesian data there as well as cluster ids)
        //TODO consider using KMeans algorithm optimized for large data sets (e.g. batched KMeans)
        //calculate using 3D Cartesian Coordinates to prevent problem e.g. at poles
        //https://gis.stackexchange.com/questions/7555/computing-an-averaged-latitude-and-longitude-coordinates
        private static List<(decimal, decimal)> KMeansClustering(int clusterCount, List<PostcodeGeodata> geodata, int maxIterations)
        {
            if (clusterCount <= 0 || clusterCount > geodata.Count) { throw new ArgumentException($"cluster count must be larger than 0 and smaller or equal than the count of datapoints"); }
            Vector3D?[] centroidsPos = new Vector3D?[clusterCount];
            Vector3D[] geoDataPos = new Vector3D[geodata.Count];
            int[] centroidIdAssignments = new int[geoDataPos.Length];
            //double[] distanceToCluster = new double[geoDataPos.Length];
            //TODO consider picking values from the list insead of generating random ones
            //Initialize random clusters (get bounding "box" and initialize inside)
            double lonMin = geodata.Min(x => decimal.ToDouble(x.Longitude));
            double lonMax = geodata.Max(x => decimal.ToDouble(x.Longitude));
            double latMin = geodata.Min(x => decimal.ToDouble(x.Latitude));
            double latMax = geodata.Max(x => decimal.ToDouble(x.Latitude));
            for (int i = 0; i < clusterCount; i++)
            {
                centroidsPos[i] = VectorUtils.PolarDegreesToCartesian(VectorUtils.RandomRange(lonMin, lonMax, latMin, latMax));
            }
            //Convert geodata to Cartesian
            for (int i = 0; i < geodata.Count; i++)
            {
                geoDataPos[i] = VectorUtils.PolarDegreesToCartesian(new Vector(
                    decimal.ToDouble(geodata[i].Longitude),
                    decimal.ToDouble(geodata[i].Latitude))
                    );
            }

            //Loop
            bool earlyExit = false;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                if (earlyExit)
                {
                    Debug.WriteLine($"Early exit of KMeans with {iter} iterations of max {maxIterations}, cluster Count {clusterCount}, and {geodata.Count} data entries");
                    break;
                }
                //Calulate which locations fall to the clusters
                for (int geoDataIndex = 0; geoDataIndex < geoDataPos.Length; geoDataIndex++)
                {
                    double shortestDistance = double.MaxValue;
                    int clusterId = -1;
                    for (int centroidIndex = 0; centroidIndex < centroidsPos.Length; centroidIndex++)
                    {
                        if (centroidsPos[centroidIndex] == null)
                        {
                            continue;
                        }
                        double currentDistance = geoDataPos[geoDataIndex].DistanceSq(centroidsPos[centroidIndex]!.Value);
                        if (currentDistance < shortestDistance)
                        {
                            shortestDistance = currentDistance;
                            clusterId = centroidIndex;
                        }
                    }
                    centroidIdAssignments[geoDataIndex] = clusterId;
                }//TODO check spread of clusters and maybe restart?
                //Center clusters
                //Vector3D?[] newClusterPos = new Vector3D?[clusterCount];
                bool valueChanged = false;
                for (int centroidIndex = 0; centroidIndex < centroidsPos.Length; centroidIndex++)
                {
                    int[] indexesOfAssignedDistanceData = centroidIdAssignments
                        .Select((x, i) => new { Item = x, Index = i })
                        .Where(x => x.Item == centroidIndex)
                        .Select(x => x.Index).ToArray();

                    IEnumerable<Vector3D> collectionToAverage = geoDataPos
                        .Where((value, index) => indexesOfAssignedDistanceData.Contains(index));

                    if (collectionToAverage.Any())
                    {
                        Vector3D average = collectionToAverage.Average();//TODO consider loss of precision or even overflowing - should not be the case with double and 1M entries
                        if(centroidsPos[centroidIndex] != average)
                        {
                            centroidsPos[centroidIndex] = average;
                            valueChanged = true;
                        }
                    }
                    else
                    {
                        centroidsPos[centroidIndex] = null;
                    }
                }
                if (!valueChanged)
                {
                    earlyExit = true;
                }
            }
            return centroidsPos.Where(x => x != null).Select(x => VectorUtils.CartersianToPolarDegrees(x!.Value)).Select(y => (((decimal)y.X), ((decimal)y.Y))).ToList();
        }


    }
}
