using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;

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
        if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid, Constants.HidGuardianHardwareId))
        {
            ResultsPanel.Children.Add(CreateNewTile("HidGuardian is installed", HidGuardianOnClicked));
        }
    }

    private async void HidGuardianOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Class\{745a17a0-74d3-11d0-b6fe-00a0c90f57da}", true);
                List<string> entries = key.GetValue("UpperFilters") is string[] filters
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
                    Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
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

        await Refresh();

        await controller.CloseAsync();
    }
}