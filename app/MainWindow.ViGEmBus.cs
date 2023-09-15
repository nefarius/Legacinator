using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IniParser;
using IniParser.Exceptions;
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
        Log.Logger.Information("Running ViGEmBus detection");

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
                        IniData data = parser.ReadFile(updaterIniFilePath, new UTF8Encoding(false));

                        string updaterUrl = data["General"]["URL"];

                        if (!updaterUrl.Equals(Constants.ViGEmBusUpdaterNewUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_isInUpdaterMode)
                            {
                                ResultsPanel.Children.Add(CreateNewTile("Outdated ViGEmBus Updater Configuration found",
                                    ViGEmBusUpdaterUrlOutdatedOnClicked, true));
                            }

                            _actionsToRun.Add(FixViGEmBusUpdaterUrlOutdated);
                        }
                    }
                }
            }
        }
        catch (ParsingException)
        {
            Log.Warning("ViGEmBus updater config file corrupt");

            ResultsPanel.Children.Add(CreateNewTile("Corrupted ViGEmBus Updater Configuration found",
                ViGEmBusUpdaterCorruptOnClicked, true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during ViGEmBus updater config file search");
        }

        Log.Logger.Information("Done");
    }

    private async void ViGEmBusUpdaterCorruptOnClicked()
    {
        Log.Information($"{nameof(ViGEmBusUpdaterCorruptOnClicked)} invoked");

        using RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
            RegistryView.Registry32);
        RegistryKey hhRegKey = view32.OpenSubKey(Constants.ViGEmBusRegistryPartialKey);

        string installPath = hhRegKey!.GetValue("Path") as string;

        string updaterIniFilePath = Path.Combine(installPath!, Constants.ViGEmBusUpdaterConfigFileName);

        const string healthyIniContent = $$"""
                                           [General]
                                           Flags=PerMachine|ShowConfigOptionsButton
                                           AppDir=C:\Program Files\Nefarius Software Solutions\ViGEm Bus Driver\
                                           ApplicationName=ViGEm Bus Driver
                                           CompanyName=Nefarius Software Solutions e.U.
                                           ApplicationVersion=1.21.442
                                           DefaultCommandLine=/checknow
                                           CheckFrequency=2
                                           DownloadsFolder=C:\ProgramData\Nefarius Software Solutions e.U.\ViGEm Bus Driver\updates\
                                           ID={67175F6C-AA18-43A7-AE60-2FC3FD10BF79}
                                           URL={{Constants.ViGEmBusUpdaterNewUrl}}
                                           """;

        using MemoryStream healthyIniContentStream = new(Encoding.UTF8.GetBytes(healthyIniContent));
        using StreamReader streamReader = new(healthyIniContentStream);

        FileIniDataParser parser = new();
        IniData data = parser.ReadData(streamReader);

        parser.WriteFile(updaterIniFilePath, data, new UTF8Encoding(false));

        await Refresh();
    }

    private async void ViGEmBusUpdaterUrlOutdatedOnClicked()
    {
        FixViGEmBusUpdaterUrlOutdated();

        await Refresh();
    }

    private void FixViGEmBusUpdaterUrlOutdated()
    {
        Log.Information($"{nameof(ViGEmBusUpdaterUrlOutdatedOnClicked)} invoked");

        using RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
            RegistryView.Registry32);
        RegistryKey hhRegKey = view32.OpenSubKey(Constants.ViGEmBusRegistryPartialKey);

        string installPath = hhRegKey!.GetValue("Path") as string;

        string updaterIniFilePath = Path.Combine(installPath!, Constants.ViGEmBusUpdaterConfigFileName);

        FileIniDataParser parser = new();
        IniData data = parser.ReadFile(updaterIniFilePath, new UTF8Encoding(false));

        data["General"]["URL"] = Constants.ViGEmBusUpdaterNewUrl;
        parser.WriteFile(updaterIniFilePath, data, new UTF8Encoding(false));
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

        await controller.CloseAsync();

        await Refresh();
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

        await controller.CloseAsync();

        await Refresh();
    }
}