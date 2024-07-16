using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Services;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class PostcodeAnalysisViewModel : ObservableObject
    {
        [ObservableProperty]
        public DatabaseContext? _databaseContext;
        public readonly IPostcodeLocationService postcodeLocationService;

        [ObservableProperty]
        private string _statusText = "Data Analysis Status - Not Started";
        [ObservableProperty]
        private int _clusterCount = 5;
        [ObservableProperty]
        private int _clusterIterations = 25;
        [ObservableProperty]
        private ObservableCollection<ClusterEntry> _clusterBindingList = new();
        [ObservableProperty]
        private ObservableCollection<PostcodeGeodataEntry> _postcodesCollection = new();

        //Dictionary<string, int> _postcodesCounts = new();

        public PostcodeAnalysisViewModel()
        {
            postcodeLocationService = new TestDataPostcodeLocationService();
        }

        public PostcodeAnalysisViewModel(IPostcodeLocationService postcodeLocationService)
        {
            this.postcodeLocationService = postcodeLocationService;
        }

        partial void OnDatabaseContextChanged(DatabaseContext? value)
        {
            Task.Run(() => OnDatabaseContextChangedAsync(value));
        }

        private async Task OnDatabaseContextChangedAsync(DatabaseContext? context)
        {
            if (context != null)
            {
                await context.ClusterEntries.LoadAsync();
                await context.PostcodeGeodataEntries.LoadAsync();
            }
            PostcodesCollection = context?.PostcodeGeodataEntries.Local.ToObservableCollection() ?? new();
            ClusterBindingList = context?.ClusterEntries.Local.ToObservableCollection() ?? new();
        }

        [RelayCommand]
        public async Task AnalayzeDataForPostCodesAsync()
        {
            await AddOrUpdatePostcodesWithCountFromPersonDataAsync();
            await FetchGeolocationDataAsync();
            await AnalyzePostcodesAsync();
        }

        [RelayCommand]
        public async Task AddOrUpdatePostcodesWithCountFromPersonDataAsync()
        {
            if (DatabaseContext == null)
            {
                Debug.WriteLine("Datacontext was null when calling GetPostcodesWithCountFromPersonData");
                return;
            }
            StatusText = $"Getting Postcodes from Person-Data";

            List<PostcodeGeodataEntry> fromPerson = await DatabaseContext.DataEntries.GroupBy(t => t.Postal)
                .Select(g => new
                {
                    Value = g.Key,
                    Count = g.Count()
                }).Select(x => new PostcodeGeodataEntry()
                {
                    Postcode = x.Value,
                    Count = x.Count
                }).ToListAsync();

            //TODO rewrite to used AddRangeAsync
            foreach (var item in fromPerson)
            {
                var entry = DatabaseContext.PostcodeGeodataEntries.Local.FirstOrDefault(x => x.Postcode == item.Postcode);
                if (entry != null)
                {
                    entry.Count = item.Count;
                }
                else
                {
                    await DatabaseContext.PostcodeGeodataEntries.AddAsync(item);
                }
            }
            await DatabaseContext.SaveChangesAsync();

            StatusText = $"Done! (Getting Postcodes from Person-Data)";
        }

        [RelayCommand]
        public async Task FetchGeolocationDataAsync()
        {
            if (DatabaseContext == null)
            {
                Debug.WriteLine("Datacontext was null when calling FetchGeolocationData");
                return;
            }
            StatusText = $"Fetching Geolocations from {postcodeLocationService.GetUrl()}";

            List<PostcodeGeodataEntry> toFetch = DatabaseContext.PostcodeGeodataEntries.Local.ToList();
            await postcodeLocationService.RequestPostcodeLocationsAsync(toFetch);
            DatabaseContext.PostcodeGeodataEntries.UpdateRange(toFetch);
            await DatabaseContext.SaveChangesAsync();

            StatusText = $"Done! (Fetching Geolocations from {postcodeLocationService.GetUrl()})";
        }

        [RelayCommand]
        public async Task AnalyzePostcodesAsync()
        {
            if (DatabaseContext == null)
            {
                Debug.WriteLine("Datacontext was null when calling AnalyzePostcodes");
                return;
            }
            StatusText = $"Running KMeans Clustering with {ClusterCount} clusters on {PostcodesCollection.Count} datapoints";

            List<PostcodeGeodataEntry> availablePostcodeLocations = DatabaseContext.PostcodeGeodataEntries.Local
                .Where(x => x.IsNotAvailable == false)
                .ToList();

            List<ClusterEntry> clusters = await Task.Run(() => KMeansClustering(ClusterCount, availablePostcodeLocations, ClusterIterations));
            await DatabaseContext.ClusterEntries.AddRangeAsync(clusters);
            await DatabaseContext.SaveChangesAsync();

            StatusText = $"Found {clusters.Count} clusters";
        }


        //TODO consider using System.Numerics.Vector3 with float precision for faster calculation or decimal for more accuaracy
        //TODO use uint when applicable
        //TODO consider directly using database for large data sets (e.g. save cartesian data there as well as cluster ids)
        //TODO consider using KMeans algorithm optimized for large data sets (e.g. batched KMeans)
        //calculated using 3D Cartesian Coordinates to prevent problem e.g. at poles
        //https://gis.stackexchange.com/questions/7555/computing-an-averaged-latitude-and-longitude-coordinates
        private static List<ClusterEntry> KMeansClustering(int clusterCount, List<PostcodeGeodataEntry> geodata, int maxIterations)
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

                    IEnumerable<double> weights = geodata
                        .Where((value, index) => indexesOfAssignedDistanceData.Contains(index))
                        .Select(x => (double)x.Count);

                    if (collectionToAverage.Any())
                    {
                        Vector3D average = collectionToAverage.Average(weights);//TODO consider loss of precision or even overflowing - should not be the case with double and 1M entries
                        if (centroidsPos[centroidIndex] != average)
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

            //Assigning of results
            List<ClusterEntry> results = new List<ClusterEntry>();
            for (int centroidIndex = 0; centroidIndex < centroidsPos.Length; centroidIndex++)
            {
                if (centroidsPos[centroidIndex] == null)
                {
                    continue;
                }
                Vector3D cp = centroidsPos[centroidIndex]!.Value;

                int[] indexesOfAssignedDistanceData = centroidIdAssignments
                    .Select((x, i) => new { Item = x, Index = i })
                    .Where(x => x.Item == centroidIndex)
                    .Select(x => x.Index).ToArray();

                List<PostcodeGeodataEntry> postcodeGeodataEntries = indexesOfAssignedDistanceData.Select(index => geodata[index]).ToList();
                List<(Vector3D, int)> postcodeGeodataPositions = indexesOfAssignedDistanceData.Select(index => (geoDataPos[index], index)).ToList();
                //int indexShortest = postcodeGeodataPositions.MinBy((x,y)=>x.Item1<y.Item1)).S
                double shortestDistance = double.MaxValue;
                int shortestDistanceIndex = -1;
                for (int geoDataIndex = 0; geoDataIndex < geoDataPos.Length; geoDataIndex++)
                {
                    double currentDistance = geoDataPos[geoDataIndex].DistanceSq(centroidsPos[centroidIndex]!.Value);
                    if (currentDistance < shortestDistance)
                    {
                        shortestDistance = currentDistance;
                        shortestDistanceIndex = geoDataIndex;
                    }
                }

                var cluster = new ClusterEntry()
                {
                    Longitude = (decimal)cp.X,
                    Latitude = (decimal)cp.Y,
                    PostcodeGeodataEntries = new ObservableCollection<PostcodeGeodataEntry>(postcodeGeodataEntries),
                    ClostestPostcode = geodata[shortestDistanceIndex].Postcode
                };
                results.Add(cluster);
            }


            //var clusterResults = centroidsPos.Where(x => x != null).Select(x => VectorUtils.CartersianToPolarDegrees(x!.Value)).Select(v => (new ClusterEntry()
            //{
            //    Longitude = (decimal)v.X,
            //    Latitude = (decimal)v.Y,
            //})).ToList();
            return results;

        }


    }
}
