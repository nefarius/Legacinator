using System;
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
			    out var hhInstances))
			try
			{
				var virtualDevice =
					PnPDevice.GetDeviceByInstanceId(hhInstances.First(), DeviceLocationFlags.Phantom);

				var hardwareId = virtualDevice.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds).ToList()
					.First();
				var driverVersion =
					new Version(virtualDevice.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));

				if (hardwareId.Equals(Constants.HidHideHardwareId, StringComparison.OrdinalIgnoreCase)
				    && driverVersion < Constants.HidHideVersionLatest)
				{
					var tile = new ResultTile
					{
						Title = $"Outdated HidHide Driver found (v{driverVersion})"
					};

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