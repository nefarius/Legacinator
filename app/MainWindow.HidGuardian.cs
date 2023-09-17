using System;
using System.Collections.Generic;
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
    //
    // Look for HidGuardian virtual device
    // 
    private void DetectHidGuardian()
    {
        Log.Logger.Information("Running HidGuardian detection");

        if (Devcon.FindInDeviceClassByHardwareId(DeviceClassIds.System, Constants.HidGuardianHardwareId))
        {
            ResultsPanel.Children.Add(CreateNewTile("HidGuardian is installed", HidGuardianOnClicked));
        }

        if (DeviceClassFilters.GetUpper(DeviceClassIds.HumanInterfaceDevices)?.Contains("HidGuardian") ?? false)
        {
            ResultsPanel.Children.Add(CreateNewTile("Partial HidGuardian installation found", HidGuardianOnClicked,
                true));
        }

        Log.Logger.Information("Done");
    }

    private async void HidGuardianOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            try
            {
                Log.Logger.Information("Removing class filter entry, if present");

                DeviceClassFilters.RemoveUpper(DeviceClassIds.HumanInterfaceDevices, "HidGuardian");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while processing filters");
            }

            Devcon.FindInDeviceClassByHardwareId(DeviceClassIds.System, Constants.HidGuardianHardwareId,
                out IEnumerable<string> instances);

            foreach (string instanceId in instances)
            {
                Log.Information("Processing instance {Instance}", instanceId);

                PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

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
                    Devcon.Remove(DeviceClassIds.System, instanceId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to remove device");
                }

                try
                {
                    controller.SetMessage("Deleting device driver store copies");

                    foreach (string path in DriverStore.ExistingDrivers.Where(d =>
                                 d.Contains(Constants.HidGuardianInfName)))
                    {
                        DriverStore.RemoveDriver(path);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete driver store copy");
                }
            }
        });

        await this.ShowMessageAsync("Reboot recommended",
            "HidGuardian components got cleaned up, a reboot is highly recommended to finish the procedure.");

        await controller.CloseAsync();
        
        await Refresh();
    }
}