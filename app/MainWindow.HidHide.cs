using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using IniParser;
using IniParser.Exceptions;
using IniParser.Model;

using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;

using Nefarius.Drivers.HidHide;
using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    private void DetectHidHide()
    {
        Log.Logger.Information("Running HidHide detection");

        //
        // Scan for HidHide and check version
        // 
        if (Devcon.FindInDeviceClassByHardwareId(DeviceClassIds.System, Constants.HidHideHardwareId,
                out IEnumerable<string> hhInstances))
        {
            try
            {
                PnPDevice virtualDevice =
                    PnPDevice.GetDeviceByInstanceId(hhInstances.First(), DeviceLocationFlags.Phantom);

                string hardwareId = virtualDevice.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds).ToList()
                    .First();
                Version driverVersion = new(virtualDevice.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));

                if (hardwareId.Equals(Constants.HidHideHardwareId, StringComparison.OrdinalIgnoreCase)
                    && driverVersion < Constants.HidHideDriverVersionLatest)
                {
                    ResultsPanel.Children.Add(CreateNewTile($"Outdated HidHide Driver found (v{driverVersion})",
                        HidHideOutdatedOnClicked));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during HidHide detection");
            }
        }

        //
        // Check for old update server URL in updater agent config file
        //

        try
        {
            RegistryKey hhRegKey = Registry.LocalMachine.OpenSubKey(Constants.HidHideRegistryPartialKey);

            if (hhRegKey is not null)
            {
                string installPath = hhRegKey.GetValue("Path") as string;

                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    string updaterIniFilePath = Path.Combine(installPath, Constants.HidHideUpdaterConfigFileName);

                    if (File.Exists(updaterIniFilePath))
                    {
                        FileIniDataParser parser = new();
                        IniData data = parser.ReadFile(updaterIniFilePath, new UTF8Encoding(false));

                        string updaterUrl = data["General"]["URL"];

                        if (!updaterUrl.Equals(Constants.HidHideUpdaterNewUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_isInUpdaterMode)
                            {
                                ResultsPanel.Children.Add(CreateNewTile("Outdated HidHide Updater Configuration found",
                                    HidHideUpdaterUrlOutdatedOnClicked, true));
                            }

                            _actionsToRun.Add(FixHidHideUpdaterUrlOutdated);
                        }
                    }
                }
            }
        }
        catch (ParsingException)
        {
            Log.Warning("HidHide updater config file corrupt");

            ResultsPanel.Children.Add(CreateNewTile("Corrupted HidHide Updater Configuration found",
                HidHideBusUpdaterCorruptOnClicked, true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during HidHide updater config file search");
        }

        //
        // Offer user the ability to wipe potentially faulty configuration
        //

        HidHideControlService hh = new();

        if (hh.IsInstalled)
        {
            try
            {
                if (hh.ApplicationPaths.Any())
                {
                    ResultsPanel.Children.Add(
                        CreateNewTile("Clean/reset HidHide application list", HidHideCleanAppList));
                }

                if (hh.BlockedInstanceIds.Any())
                {
                    ResultsPanel.Children.Add(
                        CreateNewTile("Clean/reset HidHide devices list", HidHideCleanDevicesList));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fetching driver config failed, offering reset options to user");

                ResultsPanel.Children.Add(CreateNewTile("Clean/reset HidHide application list", HidHideCleanAppList));
                ResultsPanel.Children.Add(CreateNewTile("Clean/reset HidHide devices list", HidHideCleanDevicesList));
            }
        }

        Log.Logger.Information("Done");
    }

    private async void HidHideCleanDevicesList()
    {
        try
        {
            HidHideControlService hh = new();
            hh.ClearBlockedInstancesList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{nameof(HidHideCleanDevicesList)} failed");
        }

        await Refresh();
    }

    private async void HidHideCleanAppList()
    {
        try
        {
            HidHideControlService hh = new();
            hh.ClearApplicationsList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{nameof(HidHideCleanAppList)} failed");
        }

        await Refresh();
    }

    private async void HidHideBusUpdaterCorruptOnClicked()
    {
        Log.Information($"{nameof(HidHideBusUpdaterCorruptOnClicked)} invoked");

        RegistryKey hhRegKey = Registry.LocalMachine.OpenSubKey(Constants.HidHideRegistryPartialKey);

        string installPath = hhRegKey!.GetValue("Path") as string;

        string updaterIniFilePath = Path.Combine(installPath!, Constants.HidHideUpdaterConfigFileName);

        const string healthyIniContent = $$"""
                                           [General]
                                           Flags = PerMachine
                                           AppDir = C:\Program Files\Nefarius Software Solutions\HidHide\
                                           ApplicationName = HidHide
                                           CompanyName = Nefarius Software Solutions e.U.
                                           ApplicationVersion = 1.2.128
                                           DefaultCommandLine = /checknow
                                           CheckFrequency = 2
                                           DownloadsFolder = C:\ProgramData\Nefarius Software Solutions e.U.\HidHide\updates\
                                           ID = {8822CC70-E2A5-4CB7-8F14-E27101150A1D}
                                           URL = {{Constants.HidHideUpdaterNewUrl}}
                                           """;

        using MemoryStream healthyIniContentStream = new(Encoding.UTF8.GetBytes(healthyIniContent));
        using StreamReader streamReader = new(healthyIniContentStream);

        FileIniDataParser parser = new();
        IniData data = parser.ReadData(streamReader);

        parser.WriteFile(updaterIniFilePath, data, new UTF8Encoding(false));

        await Refresh();
    }

    private async void HidHideUpdaterUrlOutdatedOnClicked()
    {
        FixHidHideUpdaterUrlOutdated();

        await Refresh();
    }

    private static void FixHidHideUpdaterUrlOutdated()
    {
        RegistryKey hhRegKey = Registry.LocalMachine.OpenSubKey(Constants.HidHideRegistryPartialKey);

        string installPath = hhRegKey!.GetValue("Path") as string;

        string updaterIniFilePath = Path.Combine(installPath!, Constants.HidHideUpdaterConfigFileName);

        FileIniDataParser parser = new();
        IniData data = parser.ReadFile(updaterIniFilePath, new UTF8Encoding(false));

        data["General"]["URL"] = Constants.HidHideUpdaterNewUrl;
        parser.WriteFile(updaterIniFilePath, data, new UTF8Encoding(false));
    }

    private async void HidHideOutdatedOnClicked()
    {
        await this.ShowMessageAsync("Download update",
            "I will now take you to the latest setup, simply download it and follow the steps to be up to date!");

        Process.Start(Constants.HidHideReleasesUri);
    }
}