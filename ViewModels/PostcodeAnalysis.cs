using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
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
        //TODO change signature or split into seperate methods but don't change pushpins here (only for quick testing)
        public async Task<List<(decimal, decimal)>> AnalyzePostcodes(DataEntryContext context, List<MapControl.Pushpin> pushpins)
        {
            Dictionary<string, int> postcodesCounts = await GetAllPostCodesWithCount(context);

            List<string> postcodes = postcodesCounts.Keys.ToList();

            PostcodeLocations = await postcodeLocationService.RequestPostcodeLocationsAsync(postcodes);
            PostcodeLocations.ForEach(x => pushpins.Add(new MapControl.Pushpin()
            {
                AutoCollapse = true,
                Content = x.Postcode,
                Location = new MapControl.Location(decimal.ToDouble(x.Latitude), decimal.ToDouble(x.Longitude))
            }));
            List<(decimal, decimal)> clusterResult = KMeansClustering(10, PostcodeLocations, 100);
            clusterResult.ForEach(x => pushpins.Add(new MapControl.Pushpin()
            {
                AutoCollapse = true,
                Content = "<cluster>",
                Location = new MapControl.Location(decimal.ToDouble(x.Item2), decimal.ToDouble(x.Item1)),
                Background = new SolidColorBrush(Colors.DarkRed),
                
            }));
            return clusterResult;
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
            bool assignmentsChanged = true;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                if (!assignmentsChanged)
                {
                    Debug.WriteLine($"Early exit of KMeans with {iter} iterations of max {maxIterations}, cluster Count {clusterCount}, and {geodata.Count} data entries");
                    break;
                }
                assignmentsChanged = false;
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
                            if (centroidIdAssignments[geoDataIndex] != clusterId)
                            {
                                assignmentsChanged = true;
                            }
                        }
                    }
                    centroidIdAssignments[geoDataIndex] = clusterId;
                }//TODO check spread of clusters and maybe restart?
                //Center clusters
                for (int centroidIndex = 0; centroidIndex < centroidsPos.Length; centroidIndex++)
                {
                    int[] indexesOfAssignedDistanceData = centroidIdAssignments.Where(x => x == centroidIndex).Select((x, index) => index).ToArray();
                    IEnumerable<Vector3D> collectionToAverage = geoDataPos
                        .Where((value, index) => indexesOfAssignedDistanceData.Contains(index));
                    if (collectionToAverage.Any())
                    {
                        Vector3D average = collectionToAverage.Average();//TODO consider loss of precision or even overflowing - should not be the case with double and 1M entries
                        centroidsPos[centroidIndex] = average;
                    }
                    else
                    {
                        centroidsPos[centroidIndex] = null;
                    }
                }
            }
            return centroidsPos.Where(x => x != null).Select(x => VectorUtils.CartersianToPolarDegrees(x!.Value)).Select(y => (((decimal)y.X), ((decimal)y.Y))).ToList();
        }


    }
}
