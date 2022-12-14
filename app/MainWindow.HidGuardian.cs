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
			var tile = new ResultTile
			{
				Title = "HidGuardian is installed"
			};

			tile.Clicked += HidGuardianOnClicked;

			ResultsPanel.Children.Add(tile);
		}
	}

	private async void HidHideOutdatedOnClicked()
	{
		await this.ShowMessageAsync("Download update",
			"I will now take you to the latest setup, simply download it and follow the steps to be up to date!");

		Process.Start(Constants.HidHideReleasesUri);
	}

	private async void HidGuardianOnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			try
			{
				var key = Registry.LocalMachine.OpenSubKey(
					@"SYSTEM\CurrentControlSet\Control\Class\{745a17a0-74d3-11d0-b6fe-00a0c90f57da}", true);
				var entries = key.GetValue("UpperFilters") is string[] filters
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
				out var instances);

			foreach (var instanceId in instances)
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
					Devcon.Remove(Constants.SystemDeviceClassGuid, instanceId);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to remove device");
				}

				try
				{
					controller.SetMessage("Deleting device driver store copies");

					foreach (var path in DriverStore.ExistingDrivers.Where(d =>
						         d.Contains(Constants.HidGuardianInfName)))
						DriverStore.RemoveDriver(path);
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