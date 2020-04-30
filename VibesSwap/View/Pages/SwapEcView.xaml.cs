using System.Windows.Controls;
using VibesSwap.ViewModel.Pages;

namespace VibesSwap.View.Pages
{
    /// <summary>
    /// Interaction logic for SwapEc.xaml
    /// </summary>
    public partial class SwapEcView : UserControl
    {
        public SwapEcView()
        {
            InitializeComponent();
            DataContext = new EcSwapVm();
        }
    }
}
