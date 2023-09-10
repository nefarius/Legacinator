using System;
using System.Collections.Generic;
using System.Linq;

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
                Version driverVersion =
                    new Version(virtualDevice.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));

                if (hardwareId.Equals(Constants.HidHideHardwareId, StringComparison.OrdinalIgnoreCase)
                    && driverVersion < Constants.HidHideVersionLatest)
                {
                    ResultTile tile = new ResultTile { Title = $"Outdated HidHide Driver found (v{driverVersion})" };

                    tile.Clicked += HidHideOutdatedOnClicked;

                    ResultsPanel.Children.Add(tile);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during HidHide detection");
            }
        }
    }
}