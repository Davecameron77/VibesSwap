using System.Windows;

namespace VibesSwap.View.Dialogs
{
    /// <summary>
    /// Interaction logic for HostsFileDisplayView.xaml
    /// </summary>
    public partial class HostsFileDisplayView : Window
    {
        public HostsFileDisplayView(string fileToDisplay)
        {
            InitializeComponent();
            HostFileTextBox.Text = fileToDisplay;
        }
    }
}
