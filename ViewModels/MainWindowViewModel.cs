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

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        public bool _canLoad;

        [ObservableProperty]
        //[NotifyPropertyChangedFor(nameof(CanLoad))]
        //[NotifyCanExecuteChangedFor(nameof(LoadCSVCommand))]
        private bool _isLoading;
        [ObservableProperty]
        private bool _isDataAvailable;


        public MainWindowViewModel()
        {
            Context = new DatabaseContext();
            DataViewModel = new DataViewModel(LoadCSVCommand);
            //PostcodeAnalysisViewModel = new PostcodeAnalysisViewModel(new TestDataPostcodeLocationService(), Context);//Consider using Dependency Injection to configure and simplify setup
            PostcodeAnalysisViewModel = new PostcodeAnalysisViewModel(new OnlinePostcodeLocationService(), Context);//Consider using Dependency Injection to configure and simplify setup
            EmailAnalysisViewModel = new EmailAnalysisViewModel();
            CompanyAnalysisViewModel = new CompanyAnalysisViewModel();
        }

        partial void OnSelectedTabIndexChanging(int oldValue, int newValue)
        {

        }

        partial void OnSelectedTabIndexChanged(int oldValue, int newValue)
        {

        }
        internal void TabSelectionChanged()
        {
            if (SelectedTabIndex != 0 && (IsLoading || !IsDataAvailable))
            {
                SelectedTabIndex = 0;
                //_selectedTabIndex = 0;
            }
        }

        partial void OnIsLoadingChanged(bool value)
        {
            //CanLoad = !value;
            DataViewModel.IsDataLoading = value;
            CanLoad = !value;
            LoadCSVCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsDataAvailableChanged(bool value)
        {
            DataViewModel.IsDataAvailable = value;
        }

        [RelayCommand(CanExecute = nameof(CanLoad))]
        public async Task LoadCSVAsync()
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
                SelectedTabIndex = 0;
                string filepath = dialog.FileName;
                Debug.WriteLine($"Selected: {filepath}");
                try
                {
                    IsLoading = true;
                    DatabaseContext ctx = await Task.Run(()=> LoadNewDbContextWithCsv(filepath));
                    await SetNewDbContextAsync(ctx);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    MessageBox.Show(e.Message, nameof(LoadCSVAsync), MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }
            }
        }

        private async Task<DatabaseContext> LoadNewDbContextWithCsv(string filepath)
        {
            await Context.DisposeAsync();
            var ctx = new DatabaseContext();
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.EnsureCreatedAsync();

            Utils.CsvLoader loader = new(5000);
            await loader.LoadDataIntoDatabase(filepath, ctx);
            return ctx;
        }


        public async Task SetNewDbContextAsync(DatabaseContext dataEntryContext)
        {
            try
            {
                Debug.WriteLine("Started Setting Db context");
                IsDataAvailable = false;
                IsLoading = true;
                await Task.Run(() => SetNewDbContextAsyncRunner(dataEntryContext));
                await PostcodeAnalysisViewModel.SetNewDatabaseContextAsync(dataEntryContext);
                EmailAnalysisViewModel.SetNewDatabaseContext(dataEntryContext);
                CompanyAnalysisViewModel.SetNewDatabaseContext(dataEntryContext);
                IsDataAvailable = dataEntryContext.DataEntries.FirstOrDefault() != default;
                Debug.WriteLine("Ended Setting Db context");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, nameof(SetNewDbContextAsync), MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SetNewDbContextAsyncRunner(DatabaseContext dataEntryContext)
        {
            try
            {
                await dataEntryContext.Database.EnsureCreatedAsync();
                await Context.DisposeAsync();
                Context = dataEntryContext;
                await dataEntryContext.DataEntries.LoadAsync();
                DataViewModel.DataEntryViewSource = dataEntryContext.DataEntries.Local.ToBindingList() ?? new BindingList<DataEntry>();
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
