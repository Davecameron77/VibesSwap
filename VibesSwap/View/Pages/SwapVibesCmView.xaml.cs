using System.Windows.Controls;
using VibesSwap.ViewModel.Pages;

namespace VibesSwap.View.Pages
{
    /// <summary>
    /// Interaction logic for SwapVibesCmView.xaml
    /// </summary>
    public partial class SwapVibesCmView : UserControl
    {
        public SwapVibesCmView()
        {
            InitializeComponent();
            DataContext = new VibesCmSwapVm();
        }
    }
}
