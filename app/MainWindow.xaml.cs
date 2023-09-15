#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using IniParser;
using IniParser.Exceptions;
using IniParser.Model;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;

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
    private const string ConfigFileName = "Legacinator.ini";
    private static readonly string WinDir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";

    private static readonly string InfDir = Path.Combine(WinDir, "INF");

    private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(15);

    private readonly List<Action> _actionsToRun = new();

    private readonly bool _isInUpdaterMode;

    private readonly bool _skipDeviceRefresh;

    public MainWindow()
    {
        InitializeComponent();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("Legacinator.log")
            .CreateLogger();

        try
        {
            FileIniDataParser parser = new();
            IniData? config = parser.ReadFile(ConfigFileName);

            bool.TryParse(config["Detection"]["SkipDeviceRefresh"], out _skipDeviceRefresh);
        }
        catch (ParsingException)
        {
            Log.Logger.Warning("Config file {Config} not found or not readable", ConfigFileName);
        }

        try
        {
            Process? parent = ParentProcessUtilities.GetParentProcess();

            Log.Logger.Information("Running with parent process: {Parent}", parent);

            if (parent is not null && parent.ProcessName.ToLower().Contains("updater"))
            {
                _skipDeviceRefresh = _isInUpdaterMode = true;
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to get parent process information");
        }
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

        Log.Logger.Information("Starting complete scan");

        await Refresh();
    }

    /// <summary>
    ///     Runs all detection routines.
    /// </summary>
    private async Task Refresh()
    {
        CancellationTokenSource cts = new();

        ProgressDialogController controller =
            await this.ShowProgressAsync("Please wait...", "Running detection, this might take a moment");

        Log.Logger.Information("Starting refresh of all component detection");

        _actionsToRun.Clear();
        ResultsPanel.Children.Clear();

        // timeout to prevent endless hang
        cts.CancelAfter(RefreshTimeout);
        cts.Token.ThrowIfCancellationRequested();

        try
        {
            await Task.Run(() =>
            {
                if (_skipDeviceRefresh)
                {
                    return;
                }

                Log.Logger.Information("Refreshing phantom devices");

                Devcon.RefreshPhantom();

                Log.Logger.Information("Phantom device refreshing done");
            }, cts.Token).ContinueWith(async _ =>
            {
                // run this fun on UI thread
                await Dispatcher.Invoke(async () =>
                {
                    DetectHidGuardian();

                    DetectScpComponents();

                    DetectViGEmBus();

                    DetectHidHide();

                    DetectBthPS3();

                    if (ResultsPanel.Children.Count == 0)
                    {
                        await this.ShowMessageAsync("All good",
                            "Congratulations, seems like this system is free of any known problematic legacy drivers!");
                    }

                    Log.Logger.Information("Finished refresh of all component detection, found {Count} issues",
                        ResultsPanel.Children.Count);

                    if (!_isInUpdaterMode)
                    {
                        await controller.CloseAsync();
                        return;
                    }

                    try
                    {
                        controller.SetMessage("Running fixes for detected problems");

                        // run detected fixed automatically
                        foreach (Action fix in _actionsToRun)
                        {
                            fix.Invoke();
                        }

                        await controller.CloseAsync();

                        await this.ShowMessageAsync("Update finished",
                            "Everything finished successfully."
                        );
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Fatal(ex, "One or more fixes failed to apply");

                        await controller.CloseAsync();
                        
                        await this.ShowMessageAsync("Unexpected error",
                            "One or more operations failed, please try again or contact support. Sorry!"
                        );
                    }

                    Application.Current.Shutdown(0);
                });
            }, cts.Token);
        }
        catch (TaskCanceledException)
        {
            await controller.CloseAsync();

            await this.ShowMessageAsync("Device refresh failed",
                "Device refresh didn't finish successfully, please reboot your PC as soon as possible. " +
                "If it hangs on reboot, use the reset button or hold the power button for a couple seconds. " +
                "Next time you run Legacinator, device refresh will be skipped to avoid this problem. Sorry!"
            );

            FileIniDataParser parser = new();
            IniData? config = new() { ["Detection"] = { ["SkipDeviceRefresh"] = true.ToString() } };

            parser.WriteFile(ConfigFileName, config);

            Application.Current.Shutdown(-1);
        }
    }

    private void OpenGitHub(object sender, RoutedEventArgs e)
    {
        Process.Start(Constants.LegacinatorRepositoryUri);
    }

    private static CustomResultTile CreateNewTile(string title, Action onClicked, bool isCritical = false)
    {
        return new CustomResultTile(title, onClicked, isCritical
            ? PackIconForkAwesomeKind.Bomb
            : PackIconForkAwesomeKind.Info);
    }
}