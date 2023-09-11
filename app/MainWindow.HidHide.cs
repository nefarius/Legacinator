using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using IniParser;
using IniParser.Model;

using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;

using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    private void DetectHidHide()
    {
        //
        // Scan for HidHide and check version
        // 
        if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid, Constants.HidHideHardwareId,
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
                    && driverVersion < Constants.HidHideVersionLatest)
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
                            ResultsPanel.Children.Add(CreateNewTile("Outdated HidHide Updater Configuration found",
                                HidHideUpdaterUrlOutdatedOnClicked, true));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during HidHide updater config file search");
        }
    }

    private async void HidHideUpdaterUrlOutdatedOnClicked()
    {
        RegistryKey hhRegKey = Registry.LocalMachine.OpenSubKey(Constants.HidHideRegistryPartialKey);

        string installPath = hhRegKey!.GetValue("Path") as string;

        string updaterIniFilePath = Path.Combine(installPath!, Constants.HidHideUpdaterConfigFileName);

        FileIniDataParser parser = new();
        IniData data = parser.ReadFile(updaterIniFilePath, new UTF8Encoding(false));

        data["General"]["URL"] = Constants.HidHideUpdaterNewUrl;
        parser.WriteFile(updaterIniFilePath, data, new UTF8Encoding(false));

        await Refresh();
    }

    private async void HidHideOutdatedOnClicked()
    {
        await this.ShowMessageAsync("Download update",
            "I will now take you to the latest setup, simply download it and follow the steps to be up to date!");

        Process.Start(Constants.HidHideReleasesUri);
    }
}