using System.Windows;
using System.Windows.Controls;

namespace WorldCompanyDataViewer.Views
{
    /// <summary>
    /// Interaction logic for LabeledProperty.xaml
    /// </summary>
    public partial class LabeledProperty : UserControl
    {
        public LabeledProperty()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
            "LabelText", typeof(string), typeof(LabeledProperty), new PropertyMetadata("-Default", OnLabelTextChanged));

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(object), typeof(LabeledProperty), new PropertyMetadata(null, OnContentChanged));

        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        private static void OnLabelTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LabeledProperty)d;
            control.UpdateLabelText();
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LabeledProperty)d;
            control.UpdateLabelText();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateLabelText();
        }

        private void UpdateLabelText()
        {
            if (!string.IsNullOrEmpty(LabelText) && LabelText != "-Default")
            {
                label.Content = LabelText+":";
            }
            else
            {
                var bindingExpression = GetBindingExpression(ContentProperty);
                if (bindingExpression != null)
                {
                    var bindingPath = bindingExpression.ParentBinding.Path.Path;
                    label.Content = bindingPath + ":";
                }
                else
                {
                    label.Content = "Unnamed";
                }
            }
        }
    }

    public class SampleData
    {
        public string LabelText { get; set; } = "Sample Label:";
        public string Content { get; set; } = "Sample Content";
    }
}
