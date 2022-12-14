using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Nefarius.Utilities.DeviceManagement.PnP;
using Serilog;
#if !DEBUG
using Legacinator.Util.Web;
#endif

namespace Legacinator;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
	private static readonly string WinDir = Environment.GetEnvironmentVariable("WINDIR");

	private static readonly string InfDir = Path.Combine(WinDir, "INF");

	public MainWindow()
	{
		InitializeComponent();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File("Legacinator.log")
			.CreateLogger();
	}

	private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
	{
#if !DEBUG
            if (Updater.IsUpdateAvailable)
            {
                await this.ShowMessageAsync("Update available",
                    "A newer version of the Legacinator is available, I'll now take you to the download site!");
                Process.Start(Constants.LegacinatorReleasesUri);
            }
#endif

		await Refresh();
	}

	/// <summary>
	///     Runs all detection routines.
	/// </summary>
	private async Task Refresh()
	{
		ResultsPanel.Children.Clear();

		Devcon.RefreshPhantom();

		//
		// Look for HidGuardian virtual device
		// 
		DetectHidGuardian();

		DetectScpComponents();

		//
		// Look for old ViGEmBus (v1.14.x) virtual device
		// 
		if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
			    Constants.ViGemBusVersion1_14HardwareId))
		{
			var tile = new ResultTile
			{
				Title = "Deprecated ViGEmBus (pre-Gen1) Driver found"
			};

			tile.Clicked += ViGEmBusPreGen1OnClicked;

			ResultsPanel.Children.Add(tile);
		}

		//
		// Scan for ViGEmBus Gen1 and check version
		// 
		if (Devcon.FindByInterfaceGuid(Constants.ViGEmBusGen1InterfaceGuid, out _, out var instanceId, 0, false))
			try
			{
				var bus = PnPDevice.GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Phantom);

				var hardwareId = bus.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds).ToList().First();
				var driverVersion = new Version(bus.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));

				if (hardwareId.Equals(Constants.ViGemBusVersion1_16HardwareId, StringComparison.OrdinalIgnoreCase)
				    && driverVersion < Constants.ViGEmBusVersionLatest)
				{
					var tile = new ResultTile
					{
						Title = $"Outdated ViGEmBus (Gen1) Driver found (v{driverVersion})"
					};

					tile.Clicked += ViGEmBusGen1OutdatedOnClicked;

					ResultsPanel.Children.Add(tile);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error during ViGEmBus Gen1 detection");
			}

		//
		// Look for HP Fork of ViGEmBus (v1.14.x) virtual device
		// 
		if (Devcon.FindInDeviceClassByHardwareId(Constants.SystemDeviceClassGuid,
			    Constants.ViGEmBusHPForkHardwareId))
		{
			var tile = new ResultTile
			{
				Title = "HP Fork of ViGEmBus Driver found"
			};

			tile.Clicked += HPForkViGEmBusOnClicked;

			ResultsPanel.Children.Add(tile);
		}

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

		if (ResultsPanel.Children.Count == 0)
			await this.ShowMessageAsync("All good",
				"Congratulations, seems like this system is free of any known problematic legacy drivers!");
	}


	private void OpenGitHub(object sender, RoutedEventArgs e)
	{
		Process.Start(Constants.LegacinatorRepositoryUri);
	}
}