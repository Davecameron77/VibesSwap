using System.Windows.Controls;
using VibesSwap.ViewModel.Pages;

namespace VibesSwap.View.Pages
{
    /// <summary>
    /// Interaction logic for SetupView.xaml
    /// </summary>
    public partial class SetupView : UserControl
    {
        public SetupView()
        {
            InitializeComponent();
            DataContext = new SetupVm();
        }
    }
}
