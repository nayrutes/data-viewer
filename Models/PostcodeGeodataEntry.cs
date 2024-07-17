using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WorldCompanyDataViewer.Models
{
    public partial class PostcodeGeodataEntry : ObservableObject
    {
        [Key]
        [Required]
        public required string Postcode {  get; set; }
        [ObservableProperty]
        public decimal _longitude;
        [ObservableProperty]
        public decimal _latitude;
        [ObservableProperty]
        public int _count;
        [ObservableProperty]
        public bool _isNotAvailable = true;

        [ObservableProperty]
        public ObservableCollection<ClusterEntry> _clusterEntries = new();
    }
}
