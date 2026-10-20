using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MahApps.Metro.Controls.Dialogs;

using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    private void DetectDsHidMini()
    {
        Log.Logger.Information("Running DsHidMini detection");

        List<Version> versions = new();

        if (FindLegacyDsHidMiniInstances(versions).Count > 0)
        {
            ResultsPanel.Children.Add(CreateNewTile(LegacyDriverTitle("DsHidMini", versions), DsHidMiniLegacyOnClicked));
        }

        Log.Logger.Information("Done");
    }

    private async void DsHidMiniLegacyOnClicked()
    {
        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

        bool removed = false;
        bool cleanupFailed = false;

        await Task.Run(() =>
        {
            HashSet<string> infsToDelete = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> infsWithFailedRemoval = new(StringComparer.OrdinalIgnoreCase);

            foreach (string instanceId in FindLegacyDsHidMiniInstances(null))
            {
                try
                {
                    controller.SetMessage("Deleting device driver");

                    if (TryRemoveLegacyDevice(instanceId, Constants.DsHidMiniDriverFileName, Constants.DsHidMiniInfName,
                            infsToDelete, infsWithFailedRemoval))
                    {
                        removed = true;
                    }
                }
                catch (Exception ex)
                {
                    cleanupFailed = true;
                    Log.Error(ex, "Failed to remove DsHidMini instance {Instance}", instanceId);
                }
            }

            cleanupFailed |= infsWithFailedRemoval.Count > 0;
            cleanupFailed |= !DeleteBoundDriverPackages(infsToDelete, infsWithFailedRemoval);
        });

        if (removed)
        {
            await this.ShowMessageAsync("Reboot recommended",
                cleanupFailed
                    ? "DsHidMini v1 cleanup is incomplete, a reboot is highly recommended to finish the procedure."
                    : "DsHidMini v1 components got cleaned up, a reboot is highly recommended to finish the procedure.");
        }
        else if (cleanupFailed)
        {
            await this.ShowMessageAsync("Cleanup failed",
                "DsHidMini v1 components could not be removed, please check the log for details.");
        }

        await controller.CloseAsync();

        await Refresh();
    }

    private static List<string> FindLegacyDsHidMiniInstances(List<Version> versions)
    {
        HashSet<string> candidates = new(StringComparer.OrdinalIgnoreCase);
        int index = 0;

        while (Devcon.FindByInterfaceGuid(Constants.DsHidMiniDeviceInterfaceGuid, out _, out string instanceId, index,
                   false))
        {
            index++;
            candidates.Add(instanceId);
        }

        AddHardwareIdMatches(candidates, Constants.DsHidMiniDeviceClassGuid, Constants.DsHidMiniUsbHardwareId, true);
        AddHardwareIdMatches(candidates, Constants.DsHidMiniDeviceClassGuid, Constants.DsHidMiniV1BluetoothHardwareId,
            false);

        List<string> legacyInstances = new();

        foreach (string instanceId in candidates)
        {
            try
            {
                if (IsLegacyDsHidMiniInstance(instanceId, out Version version))
                {
                    legacyInstances.Add(instanceId);
                    versions?.Add(version);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to inspect DsHidMini instance {Instance}", instanceId);
            }
        }

        return legacyInstances;
    }

    private static bool IsLegacyDsHidMiniInstance(string instanceId, out Version version)
    {
        version = null;
        PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

        if (!HasDsHidMiniClass(device) || !HasDsHidMiniHardwareId(device))
        {
            return false;
        }

        if (!LegacyDriver.TryGetDriverVersion(device, out version))
        {
            Log.Warning("Skipping DsHidMini instance {Instance} because its driver version is unavailable", instanceId);
            return false;
        }

        if (LegacyDriver.TryGetDsHidMiniFileVersion(out Version fileVersion) &&
            fileVersion >= Constants.DsHidMiniVersion2)
        {
            Log.Information(
                "DsHidMini instance {Instance} reports driver v{DriverVersion}, but {DriverFile} is v{FileVersion}",
                instanceId, version, Constants.DsHidMiniDriverFileName, fileVersion);
            return false;
        }

        if (version >= Constants.DsHidMiniVersion2)
        {
            return false;
        }

        Log.Information("Confirmed DsHidMini v1 instance {Instance} at driver v{Version}", instanceId, version);
        return true;
    }

    private static bool HasDsHidMiniClass(PnPDevice device)
    {
        Guid classGuid = device.GetProperty<Guid>(DevicePropertyKey.Device_ClassGuid);

        return classGuid == Constants.DsHidMiniDeviceClassGuid;
    }

    private static bool HasDsHidMiniHardwareId(PnPDevice device)
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

            if (hardwareId.Equals(Constants.DsHidMiniV1BluetoothHardwareId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (hardwareId.Equals(Constants.DsHidMiniUsbHardwareId, StringComparison.OrdinalIgnoreCase) ||
                hardwareId.StartsWith(Constants.DsHidMiniUsbHardwareId + "&", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryRemoveLegacyDevice(string instanceId, string driverFileName, string packageName,
        ISet<string> infsToDelete, ISet<string> infsWithFailedRemoval)
    {
        PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);
        string infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);
        Guid classGuid = device.GetProperty<Guid>(DevicePropertyKey.Device_ClassGuid);

        if (classGuid == Guid.Empty)
        {
            Log.Warning("Skipping {Instance} because its device class is unavailable", instanceId);
            return false;
        }

        if (!LegacyDriver.PublishedInfReferences(infName, driverFileName))
        {
            Log.Warning("Skipping {Instance} because {Inf} does not reference {Driver}", instanceId, infName,
                driverFileName);
            return false;
        }

        Log.Information("Removing {Package} device {Instance}", packageName, instanceId);

        bool removedDevice;

        try
        {
            removedDevice = Devcon.Remove(classGuid, instanceId);
        }
        catch
        {
            infsWithFailedRemoval.Add(infName);
            throw;
        }

        if (!removedDevice)
        {
            Log.Warning("Failed to remove {Package} device {Instance}", packageName, instanceId);
            infsWithFailedRemoval.Add(infName);
            return false;
        }

        infsToDelete.Add(infName);
        return true;
    }

    private static bool DeleteBoundDriverPackages(ISet<string> infsToDelete, ISet<string> infsWithFailedRemoval)
    {
        bool deletedAll = true;

        foreach (string infName in infsToDelete)
        {
            if (infsWithFailedRemoval.Contains(infName))
            {
                Log.Warning("Keeping driver package {Inf} because a device is still bound to it", infName);
                deletedAll = false;
                continue;
            }

            try
            {
                Log.Information("Deleting bound driver package {Inf}", infName);
                Devcon.DeleteDriver(infName, Path.Combine(InfDir, infName), forceDelete: false);
            }
            catch (Exception ex)
            {
                deletedAll = false;
                Log.Error(ex, "Failed to delete driver package {Inf}", infName);
            }
        }

        return deletedAll;
    }

    private static void AddHardwareIdMatches(ISet<string> instanceIds, Guid classGuid, string hardwareId,
        bool allowPartial)
    {
        if (!Devcon.FindInDeviceClassByHardwareId(classGuid, hardwareId, out IEnumerable<string> matches, false,
                allowPartial) || matches is null)
        {
            return;
        }

        foreach (string instanceId in matches)
        {
            instanceIds.Add(instanceId);
        }
    }

    private static string LegacyDriverTitle(string product, IReadOnlyList<Version> versions)
    {
        Version first = versions[0];

        for (int index = 1; index < versions.Count; index++)
        {
            if (versions[index] != first)
            {
                return $"Deprecated {product} (v1) Driver found";
            }
        }

        return $"Deprecated {product} (v1) Driver found (v{first})";
    }
}
