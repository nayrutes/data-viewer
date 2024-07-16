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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Autoload DB content
            Task.Run(() => MainWindowViewModel.SetNewDbContextAsync());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            MainWindowViewModel.Closing();
            base.OnClosing(e);
        }
    }

}