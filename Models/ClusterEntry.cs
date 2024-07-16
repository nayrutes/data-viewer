using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WorldCompanyDataViewer.Models
{
    public partial class ClusterEntry : ObservableObject
    {
        [property: Key]
        [property: Required]
        [ObservableProperty]
        public int _id;
        public string ClostestPostcode { get; set; } = "";
        public string ClostestTown { get; set; } = "";
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        [ObservableProperty]
        public ObservableCollection<PostcodeGeodataEntry> _postcodeGeodataEntries = new();
    }
}
