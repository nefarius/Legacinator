using System;
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
	private async void ScpBthOnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			var instance = 0;

			while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitBluetoothDongleInterfaceGuid, out _,
				       out var instanceId,
				       instance++, false))
			{
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

				Devcon.Remove(Constants.ScpToolkitBluetoothDongleInterfaceGuid, instanceId);

				try
				{
					controller.SetMessage("Deleting device driver store copies");

					foreach (var path in DriverStore.ExistingDrivers.Where(d =>
						         d.Contains(Constants.ScpBluetoothInfName)))
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

	private async void ScpDS3OnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			var instance = 0;

			while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock3InterfaceGuid, out _,
				       out var instanceId,
				       instance++, false))
			{
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

				Devcon.Remove(Constants.ScpToolkitDualShock3InterfaceGuid, instanceId);

				try
				{
					controller.SetMessage("Deleting device driver store copies");

					foreach (var path in DriverStore.ExistingDrivers.Where(d =>
						         d.Contains(Constants.ScpDualShock3InfName)))
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

	private async void ScpVBusOnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			var instance = 0;

			while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitScpVBusInterfaceGuid, out _, out var instanceId,
				       instance++, false))
			{
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

				Devcon.Remove(Constants.ScpToolkitScpVBusInterfaceGuid, instanceId);

				try
				{
					controller.SetMessage("Deleting device driver store copies");

					foreach (var path in DriverStore.ExistingDrivers.Where(d =>
						         d.Contains(Constants.ScpVBusInfName)))
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

	private async void ScpDS4OnClicked()
	{
		var controller = await this.ShowProgressAsync("Please wait...", "Attempting driver removal");

		await Task.Run(() =>
		{
			var instance = 0;

			while (Devcon.FindByInterfaceGuid(Constants.ScpToolkitDualShock4InterfaceGuid, out _,
				       out var instanceId,
				       instance++, false))
			{
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

				Devcon.Remove(Constants.ScpToolkitDualShock4InterfaceGuid, instanceId);

				try
				{
					controller.SetMessage("Deleting device driver store copies");

					foreach (var path in DriverStore.ExistingDrivers.Where(d =>
						         d.Contains(Constants.ScpDualShock4InfName)))
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