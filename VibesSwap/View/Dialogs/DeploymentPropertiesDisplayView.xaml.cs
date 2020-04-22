using System.Windows;
using System.Xml.Linq;

namespace VibesSwap.View.Dialogs
{
    /// <summary>
    /// Interaction logic for DeploymentPropertiesDisplayView.xaml
    /// </summary>
    public partial class DeploymentPropertiesDisplayView : Window
    {
        public DeploymentPropertiesDisplayView(XDocument fileToDisplay)
        {
            InitializeComponent();
        }
    }
}
