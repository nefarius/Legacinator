using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
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

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
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

            //
            // Look for SCP DS3 drivers
            // 
            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock3InterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Drivers for DualShock 3 found"
                };

                tile.Clicked += ScpDS3OnClicked;

                ResultsPanel.Children.Add(tile);
            }

            //
            // Look for SCP DS4 drivers
            // 
            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock4InterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Drivers for DualShock 4 found"
                };

                tile.Clicked += ScpDS4OnClicked;

                ResultsPanel.Children.Add(tile);
            }

            //
            // Look for SCP BTH drivers
            // 
            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitBluetoothDongleInterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Drivers for Bluetooth Host found"
                };

                tile.Clicked += ScpBthOnClicked;

                ResultsPanel.Children.Add(tile);
            }

            //
            // Look for SCPVBus
            // 
            if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitScpVBusInterfaceGuid, out _, out _, 0, false))
            {
                var tile = new ResultTile
                {
                    Title = "ScpToolkit Virtual Bus Driver found"
                };

                tile.Clicked += ScpVBusOnClicked;

                ResultsPanel.Children.Add(tile);
            }

            //
            // Look for old ViGEmBus (v1.14.x) virtual device
            // 
            if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                    Constants.ViGemBusVersion1_14HardwareId))
            {
                var tile = new ResultTile
                {
                    Title = "Deprecated ViGEmBus (pre-Gen1) Driver found"
                };

                tile.Clicked += ViGEmBusPreGen1OnClicked;

                ResultsPanel.Children.Add(tile);
            }

            if (ResultsPanel.Children.Count == 0)
                await this.ShowMessageAsync("All good",
                    "Congratulations, seems like this system is free of any known problematic legacy drivers!");
        }

        private void ViGEmBusPreGen1OnClicked()
        {
        }

        private void ScpBthOnClicked()
        {
            Process.Start(@"https://vigem.org/projects/ScpToolkit/ScpToolkit-Removal-Guide/");
        }

        private void ScpDS3OnClicked()
        {
            Process.Start(@"https://vigem.org/projects/ScpToolkit/ScpToolkit-Removal-Guide/");
        }

        private void ScpVBusOnClicked()
        {
            Process.Start(@"https://vigem.org/projects/ScpToolkit/ScpToolkit-Removal-Guide/");
        }

        private void ScpDS4OnClicked()
        {
            Process.Start(@"https://vigem.org/projects/ScpToolkit/ScpToolkit-Removal-Guide/");
        }

        private void HidGuardianOnClicked()
        {
            Process.Start(@"https://docs.ds4windows.app/guides/uninstalling-ds4windows/#legacy-drivers");
        }
    }
}