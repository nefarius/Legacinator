using System;
using System.Linq;
using System.Net;
using System.Reflection;

using Nefarius.Utilities.Assembly;

using Newtonsoft.Json;

using Serilog;

namespace Legacinator.Util.Web;

/// <summary>
///     Checks for updates via GitHub API.
/// </summary>
public static class Updater
{
    /// <summary>
    ///     Gets the control application version.
    /// </summary>
    private static Version AssemblyVersion => Assembly.GetEntryAssembly()!.GetFileVersion();
    
    /// <summary>
    ///     Fetches the latest release.
    /// </summary>
    private static Uri LatestReleaseUrl =>
        new("https://aiu.api.nefarius.systems/api/github/nefarius/Legacinator/updates?asJson=true&allowAny=true");

    /// <summary>
    ///     True if tag on latest GitHub release is newer than own assembly version, false otherwise.
    /// </summary>
    public static bool IsUpdateAvailable
    {
        get
        {
            try
            {
                // Query for releases/tags and store information
                using WebClient client = new();
                Log.Information("Checking for updates, preparing web request");

                string ua = $"{typeof(MainWindow).Assembly.GetName().Name}/{AssemblyVersion}";

                // Required or result is HTTP-403
                client.Headers["User-Agent"] = ua;

                // Get body
                string response = client.DownloadString(LatestReleaseUrl);

                // Get get latest release
                Release latest = JsonConvert.DeserializeObject<Release>(response);

                // No release found to compare to, bail out
                if (latest == null)
                {
                    return false;
                }

                Log.Debug("Latest tag name: {Tag}", latest.TagName);

                string tag = new(latest.TagName.Skip(1).ToArray());

                // Expected format e.g. "v1.2.3" so strip first character
                Version version = Version.Parse(tag);

                bool isOutdated = version.CompareTo(AssemblyVersion) > 0;

                return isOutdated;
            }
            catch (Exception ex)
            {
                Log.Error("Updated check failed: {Exception}", ex);

                // May happen on network issues, ignore
                return false;
            }
        }
    }
}