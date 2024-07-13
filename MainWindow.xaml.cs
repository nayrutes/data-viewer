using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WorldCompanyDataViewer.Models;
using WorldCompanyDataViewer.ViewModels;
using WorldCompanyDataViewer.Services;

namespace WorldCompanyDataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataEntryContext? _context;

        private CollectionViewSource dataEntryViewSource;

        private PostcodeAnalysis _postcodeAnalysis;

        public MainWindow()
        {
            //_postcodeAnalysis = new PostcodeAnalysis(new TestDataPostcodeLocationService());//Consider using Dependency Injection to configure and simplify setup
            _postcodeAnalysis = new PostcodeAnalysis(new OnlinePostcodeLocationService());//Consider using Dependency Injection to configure and simplify setup
            InitializeComponent();
            dataEntryViewSource = (CollectionViewSource)FindResource(nameof(dataEntryViewSource));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //TODO use Migration instead in outside of demo
            LoadContext();
        }

        //TODO consider loading Async
        private void LoadContext(DataEntryContext? dataEntryContext = null)
        {
            _context?.Dispose();
            if (dataEntryContext == null)
            {
                _context = new DataEntryContext();
            }
            else
            {
                _context = dataEntryContext;
            }
            _context.Database.EnsureCreated();
            _context.DataEntries.Load();
            dataEntryViewSource.Source = _context.DataEntries.Local.ToObservableCollection();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _context?.Dispose();
            base.OnClosing(e);
        }

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void LoadCSVCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "uk-500.csv";
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV files (.csv)|*.csv"; // Filter files by extension

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                Debug.WriteLine($"Selected: {filename}");
                LoadCSVFile(filename);
            }
        }

        //TODO consider loading async
        //TODO catch errors while parsing
        private void LoadCSVFile(string filePath)
        {
            //TODO add databaseLocationSelection
            var ctx = new DataEntryContext();
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
            using (var reader = new StreamReader(filePath))
            {

                string? line = reader.ReadLine();
                line = reader.ReadLine();//Directly reading next line to skip header line
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
                ctx.SaveChanges();
                LoadContext(ctx);

            }

        }


        private bool _postcodesRequestActive;
        private async void RequestPostcodeLocations_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _postcodesRequestActive = true;
            AnalysisStatusLabel.Text = "Processing";
            List<(decimal, decimal)> clusters = await _postcodeAnalysis.AnalyzePostcodes(_context);
            string clusterText = "";
            foreach (var cluster in clusters)
            {
                clusterText += $"({cluster.Item1},{cluster.Item2})";
            }
            AnalysisStatusLabel.Text = $"Analyzed {_postcodeAnalysis.PostcodeLocations.Count} postcode locations. " +
                $"Found clusters: {clusterText}";
            _postcodesRequestActive = false;
        }

        private void RequestPostcodeLocations_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !_postcodesRequestActive;
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand LoadCSV = new RoutedUICommand
            (
                "LoadCSV",
                "LoadCSV",
                typeof(MainWindow),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.F2, ModifierKeys.None)
                }
            );
        public static readonly RoutedUICommand RequestPostcodeLocations = new RoutedUICommand
            (
                "RequestPostcodeLocations",
                "RequestPostcodeLocations",
                typeof(MainWindow)
            );


    }
}