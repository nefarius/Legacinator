using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;
using Serilog;

namespace Legacinator;

public partial class MainWindow
{
	private void HPForkViGEmBusOnClicked()
	{
		Process.Start(Constants.HPForkSolutionUri);
	}

	private async void ViGEmBusGen1OutdatedOnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
				Constants.ViGemBusVersion1_16HardwareId,
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
						         d.Contains(Constants.ViGEmBusInfName)))
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

	private async void ViGEmBusPreGen1OnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
				Constants.ViGemBusVersion1_14HardwareId,
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
						         d.Contains(Constants.ViGEmBusInfName)))
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