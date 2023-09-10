using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

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
    public static Version AssemblyVersion => Assembly.GetEntryAssembly().GetName().Version;

    /// <summary>
    ///     Gets the releases API URI.
    /// </summary>
    public static Uri ReleasesUri => new("https://api.github.com/repos/nefarius/Legacinator/releases");

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
                using (WebClient client = new WebClient())
                {
                    Log.Information("Checking for updates, preparing web request");

                    // Required or result is HTTP-403
                    client.Headers["User-Agent"] =
                        "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                        "(compatible; MSIE 6.0; Windows NT 5.1; " +
                        ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                    // Get body
                    string response = client.DownloadString(ReleasesUri);

                    // Get JSON objects
                    IList<Root> result = JsonConvert.DeserializeObject<IList<Root>>(response);

                    // Top release is latest of interest
                    Root latest = result.FirstOrDefault();

                    // No release found to compare to, bail out
                    if (latest == null)
                    {
                        return false;
                    }

                    Log.Debug("Latest tag name: {Tag}", latest.TagName);

                    string tag = new string(latest.TagName.Skip(1).ToArray());

                    // Expected format e.g. "v1.2.3" so strip first character
                    Version version = Version.Parse(tag);

                    bool isOutdated = version.CompareTo(AssemblyVersion) > 0;

                    return isOutdated;
                }
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