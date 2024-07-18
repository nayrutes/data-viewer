using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.CustomControls;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class CompanyAnalysisViewModel : ObservableObject
    {
        private DatabaseContext DatabaseContext { get; set; }

        [ObservableProperty]
        private ObservableCollection<CountyCompanyCountEntry> _countyCompanyCountEntryCollection = new();

        [ObservableProperty]
        private ObservableCollection<PieChart.PieChartEntry> _pieChartEntries = new();

        Dispatcher UiDispatcher;

        public CompanyAnalysisViewModel()
        {
            DatabaseContext = new DatabaseContext();
            UiDispatcher = Dispatcher.CurrentDispatcher;
        }
        internal void SetNewDatabaseContext(DatabaseContext? value)
        {
            DatabaseContext = value;
        }

        [RelayCommand]
        public async Task AnalyseCompaniesAsync()
        {
            await CountCountyCompaniesAsync();

            const int cutoff = 1;//TODO make user accessable if needed
            
            await PieChart.SetPieChartAsync(cutoff, PieChartEntries, CountyCompanyCountEntryCollection, x=>x.Count, x=>x.County);
        }

        private async Task CountCountyCompaniesAsync()
        {
            IQueryable<CountyCompanyCountEntry> data =
            from x in DatabaseContext.DataEntries
            group x by x.County into g
            select new CountyCompanyCountEntry() { County = g.Key, Count = g.Count() }
            into grouped
            orderby grouped.Count descending
            select grouped;


            CountyCompanyCountEntryCollection = new ObservableCollection<CountyCompanyCountEntry>(await data.ToListAsync());
        }

    }
    public struct CountyCompanyCountEntry
    {
        public string County { get; set; }
        public int Count { get; set; }
    }
}
