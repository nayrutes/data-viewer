using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace WorldCompanyDataViewer.Views
{
    /// <summary>
    /// Interaction logic for PostcodeAnalysisView.xaml
    /// </summary>
    public partial class PostcodeAnalysisView : UserControl
    {
        public PostcodeAnalysisView()
        {
            InitializeComponent();
        }

        //https://stackoverflow.com/questions/1268552/how-do-i-get-a-textbox-to-only-accept-numeric-input-in-wpf
        private void IntegerValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
