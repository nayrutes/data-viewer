using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Services;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class PostcodeAnalysisViewModel : ObservableObject
    {
        
        public DatabaseContext DatabaseContext { get; private set; }
        public readonly IPostcodeLocationService postcodeLocationService;

        [ObservableProperty]
        private string _statusText = "Data Analysis Status - Not Started";
        [ObservableProperty]
        private int _clusterCount = 5;
        [ObservableProperty]
        private int _clusterIterations = 25;
        [ObservableProperty]
        private ObservableCollection<ClusterEntryViewModel> _clustersCollection = new();
        [ObservableProperty]
        private ObservableCollection<PostcodeGeodataEntry> _postcodesCollection = new();

        [ObservableProperty]
        private ClusterEntryViewModel? _selectetClusterViewModel;

        [ObservableProperty]
        private int _validPostcodesCount;
        [ObservableProperty]
        private int _notValidPostcodesCount;
        [ObservableProperty]
        private ObservableCollection<DataEntry> _invalidPostcodePeople;

        public PostcodeAnalysisViewModel()
        {
            this.DatabaseContext = new DatabaseContext();
            postcodeLocationService = new TestDataPostcodeLocationService();
        }

        public PostcodeAnalysisViewModel(IPostcodeLocationService postcodeLocationService, DatabaseContext context)
        {
            this.DatabaseContext = context;
            this.postcodeLocationService = postcodeLocationService;
        }


        internal void SetNewDatabaseContext(DatabaseContext context)
        {
            Debug.WriteLine("SettingDbContextOnPostcodesVM");
            DatabaseContext = context;
            PostcodesCollection = new ();
            ClustersCollection = new();

            if(context == null)
            {
                return;
            }
            context.PostcodeGeodataEntries.Load();
            context.ClusterEntries.Load();

            PostcodesCollection = context.PostcodeGeodataEntries.Local.ToObservableCollection();
            ClustersCollection = new ObservableCollection<ClusterEntryViewModel>(context.ClusterEntries.Local.Select(x => new ClusterEntryViewModel(x, context)).ToList());
            if (context != null)
            {
                context.ChangeTracker.Tracked += OnContextCheckTracker_Tracked; //Do not usubscribe the old context as it is already disposed!
                context.ChangeTracker.StateChanged += OnContextCheckTracker_StateChanged; //Do not usubscribe the old context as it is already disposed!
            }
            Task.Run(() => UpdateGeolocationInfosAsync());
        }

        //TODO think of a better way to keep db table and viewmodel collection in sync
        private void OnContextCheckTracker_Tracked(object? sender, EntityTrackedEventArgs e)
        {
            if (e.Entry.Entity is ClusterEntry entry && e.Entry.State == EntityState.Added)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ClustersCollection.Add(new ClusterEntryViewModel(entry, DatabaseContext)));
            }
        }
        //TODO think of a better way to keep db table and viewmodel collection in sync
        private void OnContextCheckTracker_StateChanged(object? sender, EntityStateChangedEventArgs e)
        {
            if (e.Entry.Entity is ClusterEntry entry)
            {
                if (e.NewState == EntityState.Deleted)
                {
                    var viewModel = ClustersCollection.FirstOrDefault(vm => vm.Id == entry.Id);
                    if (viewModel != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            ClustersCollection.Remove(viewModel));
                    }
                }
                else if (e.NewState == EntityState.Modified)
                {
                    var viewModel = ClustersCollection.FirstOrDefault(vm => vm.Id == entry.Id);
                    if (viewModel != null)
                    {
                        viewModel.Id = entry.Id;
                        // Update other properties as needed
                    }
                }

                if(e.OldState == EntityState.Added)
                {
                    var viewModel = ClustersCollection.ElementAt(entry.Id-1);
                    if (viewModel != null)
                    {
                        //Update Id as it was added by the database
                        viewModel.Id = entry.Id;
                    }
                }
            }
        }

        private async Task UpdateGeolocationInfosAsync()
        {
            //DatabaseContext.PostcodeGeodataEntries.

            IQueryable<DataEntry> data =
                from x in DatabaseContext.DataEntries
                join z in DatabaseContext.PostcodeGeodataEntries on x.Postal equals z.Postcode
                where z.IsNotAvailable == true
                select x;

            InvalidPostcodePeople = new ObservableCollection<DataEntry>(await data.ToListAsync());

            NotValidPostcodesCount = await DatabaseContext.PostcodeGeodataEntries.Where(x => x.IsNotAvailable).CountAsync();

            ValidPostcodesCount = await DatabaseContext.PostcodeGeodataEntries.CountAsync() - NotValidPostcodesCount;

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

            await DatabaseContext.PostcodeGeodataEntries.LoadAsync();
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
            await DatabaseContext.PostcodeGeodataEntries.LoadAsync();

            StatusText = $"Done! (Getting Postcodes from Person-Data)";
        }

        [RelayCommand]
        public async Task FetchGeolocationDataAsync()
        {
            try
            {
                if (DatabaseContext == null) throw new DatabaseConextNullException();

                StatusText = $"Fetching Geolocations from {postcodeLocationService.GetUrl()}";

                List<PostcodeGeodataEntry> toFetch = DatabaseContext.PostcodeGeodataEntries.Local.ToList();
                await postcodeLocationService.RequestPostcodeLocationsAsync(toFetch);
                DatabaseContext.PostcodeGeodataEntries.UpdateRange(toFetch);
                await DatabaseContext.SaveChangesAsync();
                await UpdateGeolocationInfosAsync();
                StatusText = $"Done! (Fetching Geolocations from {postcodeLocationService.GetUrl()})";
            }
            catch (DatabaseConextNullException)
            {
                MessageBox.Show("Database not initialized", nameof(FetchGeolocationDataAsync), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, nameof(FetchGeolocationDataAsync), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (HttpRequestException e)
            {
                MessageBox.Show(e.Message, nameof(FetchGeolocationDataAsync), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [RelayCommand]
        public async Task AnalyzePostcodesAsync()
        {
            try
            {
                if (DatabaseContext == null) throw new DatabaseConextNullException();

                StatusText = $"Running KMeans Clustering with {ClusterCount} clusters on {PostcodesCollection.Count} datapoints";

                List<PostcodeGeodataEntry> availablePostcodeLocations = DatabaseContext.PostcodeGeodataEntries.Local
                    .Where(x => x.IsNotAvailable == false)
                    .ToList();

                List<ClusterEntry> clusters = await Task.Run(() => KMeansClustering(ClusterCount, availablePostcodeLocations, ClusterIterations));
                foreach (var cluster in clusters)
                {
                    cluster.ClostestTown = await FetchClosestPlace(cluster.ClostestPostcode);
                }
                await DatabaseContext.ClusterEntries.AddRangeAsync(clusters);
                await DatabaseContext.SaveChangesAsync();

                StatusText = $"Found {clusters.Count} clusters";
            }
            catch (DatabaseConextNullException)
            {
                MessageBox.Show("Database not initialized", nameof(AnalyzePostcodesAsync), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, "Could not Find Cluster", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> FetchClosestPlace(string postcode)
        {
            return await postcodeLocationService.RequestClosestPlace(postcode);
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

                Vector lonLat = VectorUtils.CartersianToPolarDegrees(cp);
                var cluster = new ClusterEntry()
                {
                    Longitude = (decimal) lonLat.X,
                    Latitude = (decimal)lonLat.Y,
                    PostcodeGeodataEntries = new ObservableCollection<PostcodeGeodataEntry>(postcodeGeodataEntries),
                    ClostestPostcode = geodata[shortestDistanceIndex].Postcode
                };
                results.Add(cluster);
            }
            return results;

        }
    }
}
