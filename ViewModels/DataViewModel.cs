using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WorldCompanyDataViewer.Models;

namespace WorldCompanyDataViewer.ViewModels
{
    public partial class DataViewModel : ObservableObject
    {
        [ObservableProperty]
        private BindingList<DataEntry> _dataEntryViewSource = new BindingList<DataEntry>();
    }
}
