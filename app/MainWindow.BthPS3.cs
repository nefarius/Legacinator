using System;
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

using Serilog;

namespace Legacinator;

public partial class MainWindow
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private void DetectBthPS3()
    {
        Log.Logger.Information("Running BthPS3 detection");
        
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
                            ResultsPanel.Children.Add(CreateNewTile("Outdated BthPS3 Updater Configuration found",
                                BthPS3UpdaterOutdatedOnClicked, true));
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
}