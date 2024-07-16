using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class ClusterEntryViewModel: ObservableObject
    {
        [ObservableProperty]
        public int _id;
        //[ObservableProperty]
        public int PostcodeCount => PostcodeGeodataEntries.Count;//TODO investigate disappearing number
        [ObservableProperty]
        public int _peopleCount;
        public string ClostestPostcode { get; set; } = "";
        public string ClostestTown { get; set; } = "";
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PostcodeCount))]
        public ObservableCollection<PostcodeGeodataEntry> _postcodeGeodataEntries = new();
        [ObservableProperty]
        public ObservableCollection<DataEntry> _personCollection = new();

        public ClusterEntryViewModel(ClusterEntry entry, DatabaseContext db)
        {
            Id = entry.Id;
            ClostestPostcode = entry.ClostestPostcode;
            ClostestTown = entry.ClostestTown;
            Longitude = entry.Longitude;
            Latitude = entry.Latitude;
            PostcodeGeodataEntries = entry.PostcodeGeodataEntries;
            PersonCollection = new ObservableCollection<DataEntry>(
                db.DataEntries.Local
                .Where(x=>PostcodeGeodataEntries.Any(y=>x.Postal==y.Postcode)));
            PeopleCount = PersonCollection.Count;
        }
    }
}
