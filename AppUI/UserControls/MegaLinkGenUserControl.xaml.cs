using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppUI.UserControls
{
    /// <summary>
    /// Interaction logic for MegaLinkGenUserControl.xaml
    /// </summary>
    public partial class MegaLinkGenUserControl : UserControl
    {
        MegaLinkGenViewModel ViewModel { get; set; }

        public MegaLinkGenUserControl()
        {
            InitializeComponent();

            ViewModel = new MegaLinkGenViewModel();
            this.DataContext = ViewModel;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GenerateLinksAsync();
        }
    }
}
