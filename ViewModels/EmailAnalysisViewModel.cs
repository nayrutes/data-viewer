using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
    public partial class EmailAnalysisViewModel : ObservableObject
    {
        public DatabaseContext? DataEntryContext { get; set; }

        [ObservableProperty]
        private ObservableCollection<MailDomainDisplayItems> _mailDomainData = new();
        [ObservableProperty]
        private ObservableCollection<PieChart.PieChartEntry> _pieChartEntries = new() { new PieChart.PieChartEntry
        {
            Value = 1,
            Brush = new SolidColorBrush(Colors.DarkBlue),
            Label = "No analytics run yet"
        }
        };
        [ObservableProperty]
        private string _topDomain0 = "";
        [ObservableProperty]
        private string _topDomain1 = "";
        [ObservableProperty]
        private string _topDomain2 = "";



        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task UpdateMailDomainsAsync()
        {
            if (DataEntryContext == null)
            {
                Debug.WriteLine("Datacontext was null when calling UpdateMailDomains");
                return;
            }
            await CountMailDomains();

            SetTop3Domains();

            const int cutoff = 1;//TODO make user accessable if needed
            SetPieChart(cutoff);
        }

        private async Task CountMailDomains()
        {
            List<string?> mailFields = await DataEntryContext.DataEntries.
                Select(x => x.Email)
                .ToListAsync();

            List<MailDomainDisplayItems> data = await Task.Run(() => mailFields
            .Select(s => s.Split('@').Last())
                .GroupBy(d => d)
                .Select(group => new MailDomainDisplayItems
                {
                    MailDomain = group.Key,
                    Count = group.Count()

                })
                .OrderByDescending(x => x.Count)
                .ToList());
            MailDomainData.Clear();
            data.ForEach(d => MailDomainData.Add(d));
        }

        private void SetTop3Domains()
        {
            //TODO consider updating as a reaction to MailDomainData change if it is manipulated elsewhere as well
            TopDomain0 = MailDomainData.Count > 0 ? MailDomainData[0].MailDomain : "";
            TopDomain1 = MailDomainData.Count > 1 ? MailDomainData[1].MailDomain : "";
            TopDomain2 = MailDomainData.Count > 2 ? MailDomainData[2].MailDomain : "";
        }

        private void SetPieChart(int cutoff)
        {
            ColorGenerator.ResetBrush();
            var mainEntries = MailDomainData.Where(x => x.Count > cutoff).Select(x => new PieChart.PieChartEntry
            {
                Value = x.Count,
                Label = x.MailDomain,
                Brush = ColorGenerator.GetNextBrush()
            });
            PieChartEntries.Clear();
            foreach (var entry in mainEntries)
            {
                PieChartEntries.Add(entry);
            }
            int elseCount = MailDomainData.Where(x => x.Count <= cutoff).Sum(x => x.Count);
            PieChart.PieChartEntry elseEntry = new PieChart.PieChartEntry()
            {
                Value = elseCount,
                Label = "other",
                Brush = new SolidColorBrush(Colors.Gray)
            };
            PieChartEntries.Add(elseEntry);
        }
    }

    public struct MailDomainDisplayItems
    {
        public string MailDomain { get; set; }
        public int Count { get; set; }
    }

}
