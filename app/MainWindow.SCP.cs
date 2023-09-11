using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MahApps.Metro.Controls.Dialogs;

using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    private void DetectScpComponents()
    {
        //
        // Look for SCP DS3 drivers
        // 
        if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock3InterfaceGuid, out _, out _, 0, false))
        {
            ResultsPanel.Children.Add(CreateNewTile("ScpToolkit Drivers for DualShock 3 found", ScpDS3OnClicked));
        }

        //
        // Look for SCP DS4 drivers
        // 
        if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock4InterfaceGuid, out _, out _, 0, false))
        {
            ResultsPanel.Children.Add(CreateNewTile("ScpToolkit Drivers for DualShock 4 found", ScpDS4OnClicked));
        }

        //
        // Look for SCP BTH drivers
        // 
        if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitBluetoothDongleInterfaceGuid, out _, out _, 0, false))
        {
            ResultsPanel.Children.Add(CreateNewTile("ScpToolkit Drivers for Bluetooth Host found", ScpBthOnClicked));
        }

        //
        // Look for SCPVBus
        // 
        if (Devcon.FindByInterfaceGuid(Constants.ScpToolkitScpVBusInterfaceGuid, out _, out _, 0, false))
        {
            ResultsPanel.Children.Add(CreateNewTile("ScpToolkit Virtual Bus Driver found", ScpVBusOnClicked));
        }
    }

    private async void ScpBthOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            int instance = 0;

            while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitBluetoothDongleInterfaceGuid, out _,
                       out string instanceId,
                       instance++, false))
            {
                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

                try
                {
                    controller.SetMessage("Deleting device driver");
                    
                    Log.Information("Deleting INF file {File}", infName);

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

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.ScpBluetoothInfName)))
                    {
                        Log.Information("Deleting driver store copy {Path}", path);
                        
                        DriverStore.RemoveDriver(path);
                    }
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
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            int instance = 0;

            while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock3InterfaceGuid, out _,
                       out string instanceId,
                       instance++, false))
            {
                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

                try
                {
                    controller.SetMessage("Deleting device driver");
                    
                    Log.Information("Deleting INF file {File}", infName);

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

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.ScpDualShock3InfName)))
                    {
                        Log.Information("Deleting driver store copy {Path}", path);
                        
                        DriverStore.RemoveDriver(path);
                    }
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
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            int instance = 0;

            while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitScpVBusInterfaceGuid, out _, out string instanceId,
                       instance++, false))
            {
                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

                try
                {
                    controller.SetMessage("Deleting device driver");

                    Log.Information("Deleting INF file {File}", infName);
                    
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

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.ScpVBusInfName)))
                    {
                        Log.Information("Deleting driver store copy {Path}", path);
                        
                        DriverStore.RemoveDriver(path);
                    }
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
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            int instance = 0;

            while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock4InterfaceGuid, out _,
                       out string instanceId,
                       instance++, false))
            {
                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

                try
                {
                    controller.SetMessage("Deleting device driver");
                    
                    Log.Information("Deleting INF file {File}", infName);

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

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.ScpDualShock4InfName)))
                    {
                        Log.Information("Deleting driver store copy {Path}", path);
                        
                        DriverStore.RemoveDriver(path);
                    }
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