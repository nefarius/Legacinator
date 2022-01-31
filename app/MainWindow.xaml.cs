using System;
using System.Diagnostics;
using System.Linq;
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

            //
            // Scan for ViGEmBus Gen1 and check version
            // 
            if (Devcon.FindByInterfaceGuid(Constants.ViGEmBusGen1InterfaceGuid, out _, out var instanceId, 0, false))
            {
                try
                {
                    var bus = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var hardwareId = bus.GetProperty<string[]>(DevicePropertyDevice.HardwareIds).ToList().First();
                    var driverVersion = new Version(bus.GetProperty<string>(DevicePropertyDevice.DriverVersion));

                    if (hardwareId.Equals(Constants.ViGemBusVersion1_16HardwareId, StringComparison.OrdinalIgnoreCase)
                        && driverVersion < Constants.ViGEmBusVersionLatest)
                    {
                        var tile = new ResultTile
                        {
                            Title = $"Outdated ViGEmBus (Gen1) Driver found (v{driverVersion})"
                        };

                        tile.Clicked += ViGEmBusGen1OutdatedOnClicked;

                        ResultsPanel.Children.Add(tile);
                    }
                }
                catch { }
            }

            //
            // Look for HP Fork of ViGEmBus (v1.14.x) virtual device
            // 
            if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                    Constants.ViGEmBusHPForkHardwareId))
            {
                var tile = new ResultTile
                {
                    Title = "HP Fork of ViGEmBus Driver found"
                };

                tile.Clicked += HPForkViGEmBusOnClicked;

                ResultsPanel.Children.Add(tile);
            }

            if (ResultsPanel.Children.Count == 0)
                await this.ShowMessageAsync("All good",
                    "Congratulations, seems like this system is free of any known problematic legacy drivers!");
        }

        private void HPForkViGEmBusOnClicked()
        {
            Process.Start(@"https://github.com/ViGEm/ViGEmBus/issues/99");
        }

        private void ViGEmBusGen1OutdatedOnClicked()
        {
            Process.Start(@"https://github.com/ViGEm/ViGEmBus/releases/latest");
        }

        private void ViGEmBusPreGen1OnClicked()
        {
            Process.Start(@"https://github.com/ViGEm/ViGEmBus/releases/latest");
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