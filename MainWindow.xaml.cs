using System.Windows;
using MahApps.Metro.Controls;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Legacinator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //
            // Look for HidGuardian virtual device
            // 
            if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid, Constants.HidGuardianHardwareId))
            {
                var tile = new ResultTile
                {
                    Title = "HidGuardian is installed"
                };

                tile.Clicked += HidGuardianOnClicked;

                ResultsPanel.Children.Add(tile);
            }

            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock3InterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Drivers for DualShock 3 found"
                };

                tile.Clicked += ScpDS3OnClicked;

                ResultsPanel.Children.Add(tile);
            }

            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock4InterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Drivers for DualShock 4 found"
                };

                tile.Clicked += ScpDS4OnClicked;

                ResultsPanel.Children.Add(tile);
            }

            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitBluetoothDongleInterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Drivers for Bluetooth Host found"
                };

                tile.Clicked += ScpBthOnClicked;

                ResultsPanel.Children.Add(tile);
            }

            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitScpVBusInterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Virtual Bus Driver found"
                };

                tile.Clicked += ScpVBusOnClicked;

                ResultsPanel.Children.Add(tile);
            }
        }

        private void ScpBthOnClicked()
        {
            
        }

        private void ScpDS3OnClicked()
        {
            
        }

        private void ScpVBusOnClicked()
        {
            
        }

        private void ScpDS4OnClicked()
        {
            
        }

        private void HidGuardianOnClicked()
        {
        }
    }
}