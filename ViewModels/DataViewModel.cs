using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class DataViewModel : ObservableObject
    {
        [ObservableProperty]
        private BindingList<DataEntry> _dataEntryViewSource = new BindingList<DataEntry>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDataGrid))]
        public bool _isDataAvailable;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDataGrid))]
        public bool _isDataLoading;

        [ObservableProperty]
        private int _progressPercentage;

        [ObservableProperty]
        private string _progressText;

        public DataViewModel(IAsyncRelayCommand loadCSVCommand)
        {
            LoadCSVCommand = loadCSVCommand;
        }

        public bool ShowDataGrid => IsDataAvailable && !IsDataLoading;

        public IAsyncRelayCommand LoadCSVCommand { get; }

        internal void SetProgress(string text, int percentage)
        {
            ProgressPercentage = percentage;
            ProgressText = text;
        }
    }
}
