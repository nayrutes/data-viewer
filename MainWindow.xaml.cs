using System.ComponentModel;
using System.Windows;
using WorldCompanyDataViewer.ViewModels;

namespace WorldCompanyDataViewer
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel MainWindowViewModel { get; set; } = new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = MainWindowViewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Autoload DB content
            await MainWindowViewModel.SetNewDbContextAsync(new Models.DatabaseContext());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            MainWindowViewModel.Closing();
            base.OnClosing(e);
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            MainWindowViewModel.TabSelectionChanged();
        }
    }

}