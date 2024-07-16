﻿using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Services;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public DataViewModel DataViewModel { get; }
        public PostcodeAnalysisViewModel PostcodeAnalysisViewModel { get; }
        public EmailAnalysisViewModel EmailAnalysisViewModel { get; }
        [ObservableProperty]
        private DatabaseContext? _context;

        public MainWindowViewModel()
        {
            DataViewModel = new DataViewModel();
            //PostcodeAnalysisViewModel = new PostcodeAnalysisViewModel(new TestDataPostcodeLocationService());//Consider using Dependency Injection to configure and simplify setup
            PostcodeAnalysisViewModel = new PostcodeAnalysisViewModel(new OnlinePostcodeLocationService());//Consider using Dependency Injection to configure and simplify setup
            EmailAnalysisViewModel = new EmailAnalysisViewModel();

        }

        partial void OnContextChanged(DatabaseContext? value)
        {
            Task.Run(() => OnContextChangedAsync(value));
        }

        private async Task OnContextChangedAsync(DatabaseContext? value)
        {
            if (value != null)
            {
                await value.DataEntries.LoadAsync();
            }
            DataViewModel.DataEntryViewSource = value?.DataEntries.Local.ToBindingList() ?? new BindingList<DataEntry>();
            PostcodeAnalysisViewModel.DatabaseContext = value;
            EmailAnalysisViewModel.DataEntryContext = value;
        }

        [RelayCommand]
        public void LoadCSV()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "uk-500.csv";
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV files (.csv)|*.csv"; // Filter files by extension

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filepath = dialog.FileName;
                Debug.WriteLine($"Selected: {filepath}");
                Task.Run(() => LoadCsvFile(filepath));
            }
        }

        //TODO catch errors
        private async Task LoadCsvFile(string filePath)
        {
            var ctx = new DatabaseContext();
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.EnsureCreatedAsync();
            using (var reader = new StreamReader(filePath))
            {

                string? line = reader.ReadLine();
                line = reader.ReadLine();//Directly reading next line to skip header line //TODO consider configuration or detection for header
                while (line != null)
                {
                    //TODO testing for unplanned data (empty entry, qutotes in data, ...). Consider using an external csv parsing package
                    Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");//Alternative Regex: "[,]{1}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))"
                    string[] entry = CSVParser.Split(line);
                    for (int i = 0; i < entry.Length; i++)
                    {
                        entry[i] = entry[i].Trim().TrimStart('"').TrimEnd('"');
                    }
                    DataEntry dataEntry = new DataEntry
                    {
                        FirstName = entry[0],
                        LastName = entry[1],
                        CompanyName = entry[2],
                        Address = entry[3],
                        City = entry[4],
                        Country = entry[5],
                        Postal = entry[6],
                        Phone1 = entry[7],
                        Phone2 = entry[8],
                        Email = entry[9],
                        Website = entry[10],
                    };
                    ctx.Add(dataEntry);
                    line = reader.ReadLine();
                }
            }
            await ctx.SaveChangesAsync();
            await SetNewDbContextAsync(ctx);
        }

        public async Task SetNewDbContextAsync(DatabaseContext? dataEntryContext = null)
        {
            DatabaseContext newContext = dataEntryContext ?? new DatabaseContext();
            await newContext.Database.EnsureCreatedAsync();

            Context?.Dispose();
            Context = newContext;
        }

        internal void Closing()
        {
            Context?.Dispose();
        }
    }
}
