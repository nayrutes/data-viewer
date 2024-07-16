using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WorldCompanyDataViewer.Models
{
    public partial class PostcodeGeodataEntry : ObservableObject
    {
        [Key]
        [Required]
        public string Postcode {  get; set; }
        [ObservableProperty]
        public decimal _longitude;
        [ObservableProperty]
        public decimal _latitude;
        [ObservableProperty]
        public int _count;
        [ObservableProperty]
        public bool _isNotAvailable;


        //public int? ClusterEntryId { get; set; }

        //[ForeignKey(nameof(ClusterEntryId))]
        //public ClusterEntry ClusterEntry { get; set; }

        [ObservableProperty]
        public ObservableCollection<ClusterEntry> _clusterEntries = new();
    }
}
