using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IniParser;
using IniParser.Model;

using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;

using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    private void DetectViGEmBus()
    {
        //
        // Look for old ViGEmBus (v1.14.x) virtual device
        // 
        if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGemBusVersion1_14HardwareId))
        {
            ResultsPanel.Children.Add(CreateNewTile("Deprecated ViGEmBus (pre-Gen1) Driver found",
                ViGEmBusPreGen1OnClicked));
        }

        //
        // Scan for ViGEmBus Gen1 and check version
        // 
        if (Devcon.FindByInterfaceGuid(Constants.ViGEmBusGen1InterfaceGuid, out _, out string instanceId, 0, false))
        {
            try
            {
                PnPDevice bus = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

                string hardwareId = bus.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds).ToList().First();
                Version driverVersion = new(bus.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));

                if (hardwareId.Equals(Constants.ViGemBusVersion1_16HardwareId, StringComparison.OrdinalIgnoreCase)
                    && driverVersion < Constants.ViGEmBusVersionLatest)
                {
                    ResultsPanel.Children.Add(CreateNewTile($"Outdated ViGEmBus (Gen1) Driver found (v{driverVersion})",
                        ViGEmBusGen1OutdatedOnClicked));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during ViGEmBus Gen1 detection");
            }
        }

        //
        // Look for HP Fork of ViGEmBus (v1.14.x) virtual device
        // 
        if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGEmBusHPForkHardwareId))
        {
            ResultsPanel.Children.Add(CreateNewTile("HP Fork of ViGEmBus Driver found", HPForkViGEmBusOnClicked));
        }

        //
        // Check for old update server URL in updater agent config file
        //

        try
        {
            using RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                RegistryView.Registry32);
            RegistryKey hhRegKey = view32.OpenSubKey(Constants.ViGEmBusRegistryPartialKey);

            if (hhRegKey is not null)
            {
                string installPath = hhRegKey.GetValue("Path") as string;

                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    string updaterIniFilePath = Path.Combine(installPath, Constants.ViGEmBusUpdaterConfigFileName);

                    if (File.Exists(updaterIniFilePath))
                    {
                        FileIniDataParser parser = new();
                        IniData data = parser.ReadFile(updaterIniFilePath);

                        string updaterUrl = data["General"]["URL"];

                        if (!updaterUrl.Equals(Constants.ViGEmBusUpdaterNewUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            ResultsPanel.Children.Add(CreateNewTile("Outdated ViGEmBus Updater Configuration found",
                                ViGEmBusUpdaterUrlOutdatedOnClicked, true));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during ViGEmBus updater config file search");
        }
    }

    private async void ViGEmBusUpdaterUrlOutdatedOnClicked()
    {
        using RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
            RegistryView.Registry32);
        RegistryKey hhRegKey = view32.OpenSubKey(Constants.ViGEmBusRegistryPartialKey);

        string installPath = hhRegKey!.GetValue("Path") as string;

        string updaterIniFilePath = Path.Combine(installPath!, Constants.ViGEmBusUpdaterConfigFileName);

        FileIniDataParser parser = new();
        IniData data = parser.ReadFile(updaterIniFilePath);

        data["General"]["URL"] = Constants.ViGEmBusUpdaterNewUrl;
        parser.WriteFile(updaterIniFilePath, data);

        await Refresh();
    }

    private static void HPForkViGEmBusOnClicked()
    {
        Process.Start(Constants.HPForkSolutionUri);
    }

    private async void ViGEmBusGen1OutdatedOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGemBusVersion1_16HardwareId,
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
                                 d.Contains(Constants.ViGEmBusInfName)))
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

    private async void ViGEmBusPreGen1OnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        await Task.Run(() =>
        {
            Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
                Constants.ViGemBusVersion1_14HardwareId,
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
                                 d.Contains(Constants.ViGEmBusInfName)))
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