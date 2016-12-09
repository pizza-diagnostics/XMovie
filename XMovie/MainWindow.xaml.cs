using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MahApps.Metro.Controls;
using XMovie.Common;

namespace XMovie
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Logger logger = Logger.Instace;

        public MainWindow()
        {
            // InitializeComponent前にDataContextを設定しておくとLoadedが呼ばれる?
            this.DataContext = new XMovie.ViewModels.MainWindowViewModel(new XMovie.Service.MetroDialogService());
            InitializeComponent();
        }
    }
}
