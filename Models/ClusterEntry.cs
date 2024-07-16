using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WorldCompanyDataViewer.Models
{
    public partial class ClusterEntry : ObservableObject
    {
        [Key]
        [Required]
        public int Id { get; set; }
        //[ObservableProperty]
        public int PostcodeCount => PostcodeGeodataEntries.Count;//TODO investigate disappearing number
        public int PeopleCount { get; set; }
        public string ClostestPostcode { get; set; } = "";
        public string ClostestTown { get; set; } = "";
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PostcodeCount))]
        public ObservableCollection<PostcodeGeodataEntry> _postcodeGeodataEntries = new();
    }
}
