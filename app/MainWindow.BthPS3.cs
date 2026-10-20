using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using IniParser;
using IniParser.Exceptions;
using IniParser.Model;

using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private void DetectBthPS3()
    {
        Log.Logger.Information("Running BthPS3 detection");

        DetectLegacyBthPS3();

        //
        // Check for old update server URL in updater agent config file (<= v1.2.4)
        //

        try
        {
            using RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                RegistryView.Registry32);
            RegistryKey hhRegKey = view32.OpenSubKey(Constants.BthPS3RegistryPartialKey);

            if (hhRegKey is not null)
            {
                string installPath = hhRegKey.GetValue("Path") as string;

                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    string updaterIniFilePath = Path.Combine(installPath, Constants.BthPS3UpdaterConfigFileName);

                    if (File.Exists(updaterIniFilePath))
                    {
                        FileIniDataParser parser = new();
                        IniData data = parser.ReadFile(updaterIniFilePath, new UTF8Encoding(false));

                        string updaterUrl = data["General"]["URL"];

                        if (updaterUrl.Equals(Constants.BthPS3UpdaterLegacyUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_isInUpdaterMode)
                            {
                                ResultsPanel.Children.Add(CreateNewTile("Outdated BthPS3 Updater Configuration found",
                                    BthPS3UpdaterOutdatedOnClicked, true));
                            }

                            _actionsToRun.Add(FixBthPS3UpdaterOutdated);
                        }
                    }
                }
            }
        }
        catch (ParsingException)
        {
            Log.Warning("BthPS3 updater config file corrupt");

            ResultsPanel.Children.Add(CreateNewTile("Corrupted BthPS3 Updater Configuration found",
                BthPS3UpdaterOutdatedOnClicked, true));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during BthPS3 updater config file search");
        }

        Log.Logger.Information("Done");
    }

    private async void BthPS3UpdaterOutdatedOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Deleting automatic updater");

        FixBthPS3UpdaterOutdated();

        controller.SetMessage("Showing update notification");

        await this.ShowMessageAsync("Download update",
            "I will now take you to the latest setup, simply download it and follow the steps to be up to date!");

        Process.Start(Constants.BthPS3LatestReleaseUri);

        await controller.CloseAsync();

        await Refresh();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void FixBthPS3UpdaterOutdated()
    {
        try
        {
            TaskService.Instance.RootFolder.DeleteTask(Constants.BthPS3UpdaterScheduledTaskName, false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete scheduled task");
        }

        try
        {
            using RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                RegistryView.Registry32);
            RegistryKey hhRegKey = view32.OpenSubKey(Constants.BthPS3RegistryPartialKey);

            if (hhRegKey is not null)
            {
                string installPath = hhRegKey.GetValue("Path") as string;

                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    string updaterIniFilePath = Path.Combine(installPath, Constants.BthPS3UpdaterConfigFileName);

                    File.Delete(updaterIniFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete updater INI file");
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private void DetectLegacyBthPS3()
    {
        List<Version> versions = new();
        bool profileFound = FindLegacyBthPS3Instances(DeviceClassIds.Bluetooth, Constants.BthPS3ProfileHardwareId,
            true, Constants.BthPS3ProfileServiceName, versions).Count > 0;
        bool filterDeviceFound = FindLegacyBthPS3Instances(DeviceClassIds.System, Constants.BthPS3FilterHardwareId,
            false, Constants.BthPS3FilterServiceName, versions).Count > 0;
        bool filterServiceFound = IsLegacyBthPS3FilterService(out Version serviceVersion);
        bool nullPdoFound = FindLegacyBthPS3NullPdoInstances(versions).Count > 0;

        if (filterServiceFound)
        {
            versions.Add(serviceVersion);
        }

        if (profileFound || filterDeviceFound || filterServiceFound || nullPdoFound)
        {
            ResultsPanel.Children.Add(CreateNewTile(LegacyDriverTitle("BthPS3", versions), BthPS3LegacyOnClicked));
        }
    }

    private async void BthPS3LegacyOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        bool removed = false;
        bool cleanupFailed = false;

        await System.Threading.Tasks.Task.Run(() =>
        {
            bool legacyFilterDevice = FindLegacyBthPS3Instances(DeviceClassIds.System,
                Constants.BthPS3FilterHardwareId, false, Constants.BthPS3FilterServiceName, null).Count > 0;
            bool legacyFilterService = IsLegacyBthPS3FilterService(out _);

            if (legacyFilterDevice || legacyFilterService)
            {
                try
                {
                    controller.SetMessage("Removing Bluetooth class filter");
                    Log.Information("Removing {Service} from the Bluetooth class lower filters",
                        Constants.BthPS3FilterServiceName);
                    DeviceClassFilters.RemoveLower(DeviceClassIds.Bluetooth, Constants.BthPS3FilterServiceName);
                    removed = true;
                }
                catch (Exception ex)
                {
                    cleanupFailed = true;
                    Log.Error(ex, "Failed to remove the BthPS3PSM class filter");
                }
            }

            removed |= RemoveLegacyBthPS3Devices(controller, DeviceClassIds.System, Constants.BthPS3FilterHardwareId,
                false, Constants.BthPS3FilterServiceName, Constants.BthPS3FilterDriverFileName,
                Constants.BthPS3FilterInfName, ref cleanupFailed);
            removed |= RemoveLegacyBthPS3Devices(controller, DeviceClassIds.Bluetooth,
                Constants.BthPS3ProfileHardwareId, true, Constants.BthPS3ProfileServiceName,
                Constants.BthPS3ProfileDriverFileName, Constants.BthPS3ProfileInfName, ref cleanupFailed);

            HashSet<string> nullPdoInfs = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> nullPdoInfsWithFailedRemoval = new(StringComparer.OrdinalIgnoreCase);

            foreach (string instanceId in FindLegacyBthPS3NullPdoInstances(null))
            {
                try
                {
                    controller.SetMessage("Deleting device driver");

                    if (TryRemoveLegacyDevice(instanceId, Constants.BthPS3NullPdoCatalogName,
                            Constants.BthPS3NullPdoInfName, nullPdoInfs, nullPdoInfsWithFailedRemoval))
                    {
                        removed = true;
                    }
                }
                catch (Exception ex)
                {
                    cleanupFailed = true;
                    Log.Error(ex, "Failed to remove BthPS3 null PDO {Instance}", instanceId);
                }
            }

            cleanupFailed |= nullPdoInfsWithFailedRemoval.Count > 0;
            cleanupFailed |= !DeleteBoundDriverPackages(nullPdoInfs, nullPdoInfsWithFailedRemoval);

            if (IsLegacyBthPS3FilterService(out _))
            {
                try
                {
                    controller.SetMessage("Deleting legacy filter service");
                    Log.Information(
                        "Deleting manually installed {Service}; pre-v1.3 setups used {Inf} without a filter devnode",
                        Constants.BthPS3FilterServiceName, Constants.BthPS3LegacyFilterInfName);
                    if (LegacyDriver.DeleteService(Constants.BthPS3FilterServiceName))
                    {
                        removed = true;
                    }
                    else
                    {
                        cleanupFailed = true;
                    }
                }
                catch (Exception ex)
                {
                    cleanupFailed = true;
                    Log.Error(ex, "Failed to delete the BthPS3PSM service");
                }
            }
        });

        if (removed)
        {
            await this.ShowMessageAsync("Reboot recommended",
                cleanupFailed
                    ? "BthPS3 v1 cleanup is incomplete, a reboot is highly recommended to finish the procedure."
                    : "BthPS3 v1 components got cleaned up, a reboot is highly recommended to finish the procedure.");
        }

        await controller.CloseAsync();

        await Refresh();
    }

    private static bool RemoveLegacyBthPS3Devices(ProgressDialogController controller, Guid classGuid,
        string hardwareId, bool allowPartial, string serviceName, string driverFileName, string packageName,
        ref bool cleanupFailed)
    {
        bool removed = false;
        HashSet<string> infsToDelete = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> infsWithFailedRemoval = new(StringComparer.OrdinalIgnoreCase);

        foreach (string instanceId in FindLegacyBthPS3Instances(classGuid, hardwareId, allowPartial, serviceName,
                     null))
        {
            try
            {
                controller.SetMessage("Deleting device driver");

                if (TryRemoveLegacyDevice(instanceId, driverFileName, packageName, infsToDelete,
                        infsWithFailedRemoval))
                {
                    removed = true;
                }
            }
            catch (Exception ex)
            {
                cleanupFailed = true;
                Log.Error(ex, "Failed to remove BthPS3 instance {Instance}", instanceId);
            }
        }

        cleanupFailed |= infsWithFailedRemoval.Count > 0;
        cleanupFailed |= !DeleteBoundDriverPackages(infsToDelete, infsWithFailedRemoval);
        return removed;
    }

    private static List<string> FindLegacyBthPS3Instances(Guid classGuid, string hardwareId, bool allowPartial,
        string serviceName, List<Version> versions)
    {
        List<string> legacyInstances = new();

        if (!Devcon.FindInDeviceClassByHardwareId(classGuid, hardwareId, out IEnumerable<string> instances, false,
                allowPartial) || instances is null)
        {
            return legacyInstances;
        }

        foreach (string instanceId in instances)
        {
            try
            {
                if (IsLegacyBthPS3Instance(instanceId, serviceName, out Version version))
                {
                    legacyInstances.Add(instanceId);
                    versions?.Add(version);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to inspect BthPS3 instance {Instance}", instanceId);
            }
        }

        return legacyInstances;
    }

    private static bool IsLegacyBthPS3Instance(string instanceId, string serviceName, out Version version)
    {
        version = null;
        PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);
        string service = device.GetProperty<string>(DevicePropertyKey.Device_Service);

        if (!string.Equals(service, serviceName, StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("Skipping {Instance} because service {Service} is not {Expected}", instanceId, service,
                serviceName);
            return false;
        }

        if (!LegacyDriver.TryGetDriverVersion(device, out version))
        {
            Log.Warning("Skipping BthPS3 instance {Instance} because its driver version is unavailable", instanceId);
            return false;
        }

        if (LegacyDriver.TryGetServiceFileVersion(serviceName, out Version fileVersion) &&
            fileVersion >= Constants.BthPS3Version2)
        {
            Log.Information(
                "BthPS3 instance {Instance} reports driver v{DriverVersion}, but service {Service} is v{FileVersion}",
                instanceId, version, serviceName, fileVersion);
            return false;
        }

        if (version >= Constants.BthPS3Version2)
        {
            return false;
        }

        Log.Information("Confirmed BthPS3 v1 instance {Instance} ({Service}) at driver v{Version}", instanceId,
            serviceName, version);
        return true;
    }

    private static bool IsLegacyBthPS3FilterService(out Version version)
    {
        version = null;

        if (!LegacyDriver.TryGetServiceFileVersion(Constants.BthPS3FilterServiceName, out version))
        {
            return false;
        }

        if (version >= Constants.BthPS3Version2)
        {
            return false;
        }

        Log.Information("Confirmed BthPS3PSM v1 service binary v{Version}", version);
        return true;
    }

    private static List<string> FindLegacyBthPS3NullPdoInstances(List<Version> versions)
    {
        HashSet<string> candidates = new(StringComparer.OrdinalIgnoreCase);

        foreach (string hardwareId in Constants.BthPS3NullPdoHardwareIds)
        {
            AddHardwareIdMatches(candidates, Constants.BthPS3NullPdoClassGuid, hardwareId, false);
        }

        List<string> legacyInstances = new();

        foreach (string instanceId in candidates)
        {
            try
            {
                if (IsLegacyBthPS3NullPdo(instanceId, out Version version))
                {
                    legacyInstances.Add(instanceId);
                    versions?.Add(version);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to inspect BthPS3 null PDO {Instance}", instanceId);
            }
        }

        return legacyInstances;
    }

    private static bool IsLegacyBthPS3NullPdo(string instanceId, out Version version)
    {
        version = null;
        PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);
        Guid classGuid = device.GetProperty<Guid>(DevicePropertyKey.Device_ClassGuid);
        string service = device.GetProperty<string>(DevicePropertyKey.Device_Service);

        if (classGuid != Constants.BthPS3NullPdoClassGuid || !string.IsNullOrEmpty(service) ||
            !HasExactHardwareId(device, Constants.BthPS3NullPdoHardwareIds))
        {
            return false;
        }

        if (!LegacyDriver.TryGetDriverVersion(device, out version))
        {
            Log.Warning("Skipping BthPS3 null PDO {Instance} because its driver version is unavailable", instanceId);
            return false;
        }

        if (version >= Constants.BthPS3Version2)
        {
            return false;
        }

        Log.Information("Confirmed BthPS3 v1 null PDO {Instance} at driver v{Version}", instanceId, version);
        return true;
    }

    private static bool HasExactHardwareId(PnPDevice device, IEnumerable<string> expectedHardwareIds)
    {
        string[] hardwareIds = device.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds);

        if (hardwareIds is null)
        {
            return false;
        }

        foreach (string hardwareId in hardwareIds)
        {
            if (string.IsNullOrEmpty(hardwareId))
            {
                continue;
            }

            foreach (string expectedHardwareId in expectedHardwareIds)
            {
                if (hardwareId.Equals(expectedHardwareId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}