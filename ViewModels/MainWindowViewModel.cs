using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.Services;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        //[ObservableProperty]
        private DatabaseContext Context;

        public DataViewModel DataViewModel { get; }
        public PostcodeAnalysisViewModel PostcodeAnalysisViewModel { get; }
        public EmailAnalysisViewModel EmailAnalysisViewModel { get; }
        public CompanyAnalysisViewModel CompanyAnalysisViewModel { get; }


        public MainWindowViewModel()
        {
            Context = new DatabaseContext();
            DataViewModel = new DataViewModel();
            //PostcodeAnalysisViewModel = new PostcodeAnalysisViewModel(new TestDataPostcodeLocationService(), Context);//Consider using Dependency Injection to configure and simplify setup
            PostcodeAnalysisViewModel = new PostcodeAnalysisViewModel(new OnlinePostcodeLocationService(), Context);//Consider using Dependency Injection to configure and simplify setup
            EmailAnalysisViewModel = new EmailAnalysisViewModel();
            CompanyAnalysisViewModel = new CompanyAnalysisViewModel();
        }

        [RelayCommand]
        public void LoadCSV()
        {
            if (MessageBox.Show("Loading a CSV file will delete all database entries. Are you sure you want to continue?", "Load csv", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "uk-500.csv";
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV files (.csv)|*.csv"; // Filter files by extension

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filepath = dialog.FileName;
                Debug.WriteLine($"Selected: {filepath}");
                Task.Run(() => LoadNewDbContextWithCsv(filepath));
            }
        }

        private async Task LoadNewDbContextWithCsv(string filePath)
        {
            try
            {
                await Context.DisposeAsync();
                var ctx = new DatabaseContext();
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();

                Utils.CsvLoader loader = new (5000);
                await loader.LoadDataIntoDatabase(filePath, ctx);

                await SetNewDbContextAsync(ctx);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message, nameof(LoadNewDbContextWithCsv), MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task SetNewDbContextAsync(DatabaseContext dataEntryContext)
        {
            try
            {
                await dataEntryContext.Database.EnsureCreatedAsync();
                await Context.DisposeAsync();
                Context = dataEntryContext;
                await dataEntryContext.DataEntries.LoadAsync();
                DataViewModel.DataEntryViewSource = dataEntryContext.DataEntries.Local.ToBindingList() ?? new BindingList<DataEntry>();
                PostcodeAnalysisViewModel.SetNewDatabaseContext(dataEntryContext);
                EmailAnalysisViewModel.SetNewDatabaseContext(dataEntryContext);
                CompanyAnalysisViewModel.SetNewDatabaseContext(dataEntryContext);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, nameof(SetNewDbContextAsync), MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        internal void Closing()
        {
            Context.Dispose();
        }
    }

    
}
