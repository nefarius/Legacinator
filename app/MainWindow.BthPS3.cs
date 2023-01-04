using System;
using System.Collections.Generic;
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
	private void DetectBthPs3()
	{
		var drivers = DriverStore.ExistingDrivers.ToList();

		if (drivers.Any(d => d.Contains("bthps3")))
		{
			var tile = new ResultTile
			{
				Title = "BthPS3 Drivers found"
			};

			tile.Clicked += BthPS3FoundOnClicked;

			ResultsPanel.Children.Add(tile);
		}
	}

	private async void BthPS3FoundOnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			try
			{
				Log.Information("Looking for filter registry value");

				var key = Registry.LocalMachine.OpenSubKey(
					@"SYSTEM\CurrentControlSet\Control\Class\{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}", true);
				var entries = key.GetValue("LowerFilters") is string[] filters
					? new List<string>(filters)
					: new List<string>();
				if (entries.Contains("BthPS3PSM"))
				{
					Log.Information("BthPS3PSM found");

					entries.Remove("BthPS3PSM");
					key.SetValue("LowerFilters", entries.ToArray(), RegistryValueKind.MultiString);

					Log.Information("BthPS3PSM removed");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error while processing filters");
			}

			Log.Information("Looking for profile driver service device node");

			Devcon.FindInDeviceClassByHardwareId(Constants.BluetoothDeviceClassGuid, Constants.BthPS3HardwareId,
				out var instances);

			var nodes = instances.ToList();

			Log.Information("Found {Count} profile driver service device node", nodes.Count);

			foreach (var instanceId in nodes)
			{
				Log.Information("Processing instance {Instance}", instanceId);

				var device = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

				var infName = device.GetProperty<string>(DevicePropertyKey.Device_DriverInfPath);

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
					Devcon.Remove(Constants.BluetoothDeviceClassGuid, instanceId);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to remove device");
				}

				try
				{
					controller.SetMessage("Deleting device driver store copies");

					foreach (var path in DriverStore.ExistingDrivers.Where(d =>
						         d.Contains(Constants.BthPS3HardwareId)))
						DriverStore.RemoveDriver(path);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to delete driver store copy");
				}

				try
				{
					var drivers = DriverStore.ExistingDrivers.ToList();

					foreach (var path in drivers.Where(d => d.Contains("bthps3"))) DriverStore.RemoveDriver(path);
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