using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32;

using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

namespace Legacinator;

internal static class LegacyDriver
{
    private const uint ScManagerConnect = 0x0001;
    private const uint ServiceDeleteAccess = 0x00010000;
    private const int ErrorServiceDoesNotExist = 1060;
    private const int ErrorServiceMarkedForDelete = 1072;

    public static bool TryGetDriverVersion(PnPDevice device, out Version version)
    {
        version = null;
        string versionText = device.GetProperty<string>(DevicePropertyKey.Device_DriverVersion);

        return TryParseVersion(versionText, out version);
    }

    public static bool TryGetServiceFileVersion(string serviceName, out Version version)
    {
        version = null;

        using RegistryKey key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Services\{serviceName}");
        string imagePath = key?.GetValue("ImagePath") as string;

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return false;
        }

        return TryGetFileVersion(ResolveDriverImagePath(imagePath), out version);
    }

    public static bool TryGetDsHidMiniFileVersion(out Version version)
    {
        string driverPath = Path.Combine(NativeSystemDirectory, "drivers", "UMDF", Constants.DsHidMiniDriverFileName);

        return TryGetFileVersion(driverPath, out version);
    }

    public static bool PublishedInfReferences(string infName, string expectedDriverFileName)
    {
        if (string.IsNullOrWhiteSpace(infName) || string.IsNullOrWhiteSpace(expectedDriverFileName))
        {
            return false;
        }

        string infPath = Path.Combine(Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows", "INF", infName);

        if (!File.Exists(infPath))
        {
            Log.Warning("Bound INF {Inf} does not exist", infPath);
            return false;
        }

        string contents = ReadInfText(infPath);

        return contents.IndexOf(expectedDriverFileName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool DeleteService(string serviceName)
    {
        IntPtr manager = OpenSCManager(null, null, ScManagerConnect);

        if (manager == IntPtr.Zero)
        {
            Log.Error("Failed to open the service manager, Win32 error {Error}", Marshal.GetLastWin32Error());
            return false;
        }

        try
        {
            IntPtr service = OpenService(manager, serviceName, ServiceDeleteAccess);

            if (service == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();

                if (error == ErrorServiceDoesNotExist)
                {
                    Log.Information("Service {Service} is already absent", serviceName);
                    return true;
                }

                Log.Error("Failed to open service {Service}, Win32 error {Error}", serviceName, error);
                return false;
            }

            try
            {
                if (DeleteService(service))
                {
                    Log.Information("Deleted service {Service}", serviceName);
                    return true;
                }

                int error = Marshal.GetLastWin32Error();

                if (error == ErrorServiceMarkedForDelete)
                {
                    Log.Information("Service {Service} is already marked for deletion", serviceName);
                    return true;
                }

                Log.Error("Failed to delete service {Service}, Win32 error {Error}", serviceName, error);
                return false;
            }
            finally
            {
                CloseServiceHandle(service);
            }
        }
        finally
        {
            CloseServiceHandle(manager);
        }
    }

    private static bool TryParseVersion(string versionText, out Version version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(versionText) || !Version.TryParse(versionText.Trim(), out version))
        {
            return false;
        }

        return version > new Version(0, 0);
    }

    private static bool TryGetFileVersion(string path, out Version version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);

        if (info.FileMajorPart <= 0 && info.FileMinorPart <= 0 && info.FileBuildPart <= 0 && info.FilePrivatePart <= 0)
        {
            return false;
        }

        version = new Version(
            Math.Max(info.FileMajorPart, 0),
            Math.Max(info.FileMinorPart, 0),
            Math.Max(info.FileBuildPart, 0),
            Math.Max(info.FilePrivatePart, 0));

        return version > new Version(0, 0);
    }

    private static string ResolveDriverImagePath(string imagePath)
    {
        string path = imagePath.Trim().Trim('"');
        path = Environment.ExpandEnvironmentVariables(path);

        const string ntSystemRoot = @"\SystemRoot";

        if (path.StartsWith(ntSystemRoot, StringComparison.OrdinalIgnoreCase))
        {
            string windows = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
            path = Path.Combine(windows, path.Substring(ntSystemRoot.Length).TrimStart('\\'));
        }

        if (path.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            path = path.Substring(4);
        }

        if (!Path.IsPathRooted(path))
        {
            string windows = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
            path = Path.Combine(windows, path);
        }

        string system32 = Path.Combine(Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows", "System32");

        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess &&
            path.StartsWith(system32, StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(NativeSystemDirectory, path.Substring(system32.Length).TrimStart('\\'));
        }

        return path;
    }

    private static string NativeSystemDirectory
    {
        get
        {
            string windows = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                return Path.Combine(windows, "Sysnative");
            }

            return Path.Combine(windows, "System32");
        }
    }

    private static string ReadInfText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        int zeroCount = 0;
        int inspected = Math.Min(bytes.Length, 256);

        for (int index = 1; index < inspected; index += 2)
        {
            if (bytes[index] == 0)
            {
                zeroCount++;
            }
        }

        if (zeroCount > 16)
        {
            return Encoding.Unicode.GetString(bytes);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint desiredAccess);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenService(IntPtr database, string serviceName, uint desiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DeleteService(IntPtr service);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr handle);
}
