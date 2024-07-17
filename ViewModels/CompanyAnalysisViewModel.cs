using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.CustomControls;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class CompanyAnalysisViewModel : ObservableObject
    {
        private DatabaseContext DatabaseContext { get; set; }

        [ObservableProperty]
        private ObservableCollection<CountyCompanyCountEntry> _countyCompanyCountEntryCollection = new();

        [ObservableProperty]
        private ObservableCollection<PieChart.PieChartEntry> _pieChartEntries = new()
        {
            new PieChart.PieChartEntry
            {
                Value = 1,
                Brush = new SolidColorBrush(Colors.DarkBlue),
                Label = "No analytics run yet"
            }
        };

        public CompanyAnalysisViewModel()
        {
            DatabaseContext = new DatabaseContext();
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
            //await Task.Run(() => SetPieChart(cutoff));
            await SetPieChart(cutoff);
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

        private Task SetPieChart(int cutoff)
        {
            ColorGenerator.ResetBrush();
            PieChartEntries.Clear();
            var mainEntries = CountyCompanyCountEntryCollection.Where(x => x.Count > cutoff).Select(x => new PieChart.PieChartEntry
            {
                Value = x.Count,
                Label = x.County,
                Brush = ColorGenerator.GetNextBrush()
            });
            foreach (var entry in mainEntries)
            {
                PieChartEntries.Add(entry);
            }
            int elseCount = CountyCompanyCountEntryCollection.Where(x => x.Count <= cutoff).Sum(x => x.Count);
            PieChart.PieChartEntry elseEntry = new PieChart.PieChartEntry()
            {
                Value = elseCount,
                Label = "other",
                Brush = new SolidColorBrush(Colors.Gray)
            };
            PieChartEntries.Add(elseEntry);
            return Task.CompletedTask;
        }
    }
    public struct CountyCompanyCountEntry
    {
        public string County { get; set; }
        public int Count { get; set; }
    }
}
