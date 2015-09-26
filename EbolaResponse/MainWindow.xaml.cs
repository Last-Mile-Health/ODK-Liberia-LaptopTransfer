using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using LastMileHealth.ViewModels;
using LastMileHealth.Helpers;

namespace LastMileHealth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel mainViewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this.mainViewModel;
            this.Closing += MainWindow_Closing;
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            ((INotifyCollectionChanged)this.statusListView.Items).CollectionChanged += OnStatusListViewCollectionChanged;

            Logger.LogEvent(false, "App started!");
            Logger.LogEvent(false, mainViewModel.GetSystemInformation());
            Logger.LogEvent(false, mainViewModel.GetBluetoothDeviceInfo());
        }

        void OnStatusListViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                this.statusListView.ScrollIntoView(e.NewItems[0]);
            }
            catch (Exception ex)
            {
                this.mainViewModel.AddExceptionToLog(ex);
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Closing -= MainWindow_Closing;
            this.mainViewModel.ClearXMLsOnClose();
        }
    }
}
