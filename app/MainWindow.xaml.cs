using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Legacinator.Util.Web;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;
using Serilog;

namespace Legacinator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static readonly string WinDir = Environment.GetEnvironmentVariable("WINDIR");

        private static readonly string InfDir = Path.Combine(WinDir, "INF");

        public MainWindow()
        {
            InitializeComponent();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("Legacinator.log")
                .CreateLogger();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Updater.IsUpdateAvailable)
            {
                await this.ShowMessageAsync("Update available",
                    "A newer version of the Legacinator is available, I'll now take you to the download site!");
                Process.Start(@"https://github.com/nefarius/Legacinator/releases/latest");
            }

            await Refresh();
        }

        private async Task Refresh()
        {
            ResultsPanel.Children.Clear();

            Devcon.RefreshPhantom();

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
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during ViGEmBus Gen1 detection");
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

            //
            // Scan for HidHide and check version
            // 
            if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid, Constants.HidHideHardwareId, out var hhInstances))
            {
                try
                {
                    var virtualDevice = PnPDevice.GetDeviceByInstanceId(hhInstances.First(), DeviceLocationFlags.Phantom);

                    var hardwareId = virtualDevice.GetProperty<string[]>(DevicePropertyDevice.HardwareIds).ToList().First();
                    var driverVersion = new Version(virtualDevice.GetProperty<string>(DevicePropertyDevice.DriverVersion));

                    if (hardwareId.Equals(Constants.HidHideHardwareId, StringComparison.OrdinalIgnoreCase)
                        && driverVersion < Constants.HidHideVersionLatest)
                    {
                        var tile = new ResultTile
                        {
                            Title = $"Outdated HidHide Driver found (v{driverVersion})"
                        };

                        tile.Clicked += HidHideOutdatedOnClicked;

                        ResultsPanel.Children.Add(tile);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during HidHide detection");
                }
            }

            if (ResultsPanel.Children.Count == 0)
                await this.ShowMessageAsync("All good",
                    "Congratulations, seems like this system is free of any known problematic legacy drivers!");
        }

        private async void HidHideOutdatedOnClicked()
        {
            await this.ShowMessageAsync("Download update",
                "I will now take you to the latest setup, simply download it and follow the steps to be up to date!");

            Process.Start(@"https://github.com/ViGEm/HidHide/releases/latest");
        }

        private void HPForkViGEmBusOnClicked()
        {
            Process.Start(@"https://github.com/ViGEm/ViGEmBus/issues/99");
        }

        private async void ViGEmBusGen1OutdatedOnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                    Constants.ViGemBusVersion1_16HardwareId,
                    out var instances);

                foreach (var instanceId in instances)
                {
                    Log.Information("Processing instance {Instance}", instanceId);

                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    try
                    {
                        Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to remove device");
                    }

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.ViGEmBusInfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }

        private async void ViGEmBusPreGen1OnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                    Constants.ViGemBusVersion1_14HardwareId,
                    out var instances);

                foreach (var instanceId in instances)
                {
                    Log.Information("Processing instance {Instance}", instanceId);

                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    try
                    {
                        Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to remove device");
                    }

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.ViGEmBusInfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }

        private async void ScpBthOnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                var instance = 0;

                while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitBluetoothDongleInterfaceGuid, out _,
                           out var instanceId,
                           instance++, false))
                {
                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    Devcon.Remove(Constants.ScpToolkitBluetoothDongleInterfaceGuid, instanceId);

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.ScpBluetoothInfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }

        private async void ScpDS3OnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                var instance = 0;

                while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock3InterfaceGuid, out _,
                           out var instanceId,
                           instance++, false))
                {
                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    Devcon.Remove(Constants.ScpToolkitDualShock3InterfaceGuid, instanceId);

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.ScpDualShock3InfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }

        private async void ScpVBusOnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                var instance = 0;

                while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitScpVBusInterfaceGuid, out _, out var instanceId,
                           instance++, false))
                {
                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    Devcon.Remove(Constants.ScpToolkitScpVBusInterfaceGuid, instanceId);

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.ScpVBusInfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }

        private async void ScpDS4OnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                var instance = 0;

                while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock4InterfaceGuid, out _,
                           out var instanceId,
                           instance++, false))
                {
                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    Devcon.Remove(Constants.ScpToolkitDualShock4InterfaceGuid, instanceId);

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.ScpDualShock4InfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }

        private async void HidGuardianOnClicked()
        {
            var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

            await Task.Run(() =>
            {
                try
                {
                    var key = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\Class\{745a17a0-74d3-11d0-b6fe-00a0c90f57da}", true);
                    var entries = key.GetValue("UpperFilters") is string[] filters
                        ? new List<string>(filters)
                        : new List<string>();
                    if (entries.Contains("HidGuardian"))
                    {
                        entries.Remove("HidGuardian");
                        key.SetValue("UpperFilters", entries.ToArray(), RegistryValueKind.MultiString);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while processing filters");
                }

                Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid, Constants.HidGuardianHardwareId,
                    out var instances);

                foreach (var instanceId in instances)
                {
                    Log.Information("Processing instance {Instance}", instanceId);

                    var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                    var infName = device.GetProperty<string>(DevicePropertyDevice.DriverInfPath);

                    try
                    {
                        controller.SetMessage("Deleting device driver");

                        Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver");
                    }

                    controller.SetMessage("Removing device");

                    try
                    {
                        Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to remove device");
                    }

                    try
                    {
                        controller.SetMessage("Deleting device driver store copies");

                        foreach (var path in DriverStore.ExistingDrivers.Where(d =>
                                     d.Contains(Constants.HidGuardianInfName)))
                            DriverStore.RemoveDriver(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to delete driver store copy");
                    }
                }
            });

            await Refresh();

            await controller.CloseAsync();
        }
    }
}