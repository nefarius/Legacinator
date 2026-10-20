using System;
using System.Collections.Generic;
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

        return InfAssociatesDriverFile(contents, expectedDriverFileName);
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

    private static bool InfAssociatesDriverFile(string contents, string expectedFileName)
    {
        Dictionary<string, List<string>> sections = ParseInfSections(contents);
        HashSet<string> installSections = GetInfInstallSections(sections);
        HashSet<string> copySections = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, List<string>> section in sections)
        {
            bool isInstallSection = IsInfInstallSection(section.Key, installSections);

            foreach (string line in section.Value)
            {
                if (!TrySplitInfDirective(line, out string key, out string value))
                {
                    continue;
                }

                if (key.Equals("CopyFiles", StringComparison.OrdinalIgnoreCase))
                {
                    if (!isInstallSection)
                    {
                        continue;
                    }

                    foreach (string token in SplitInfList(value))
                    {
                        string entry = UnquoteInf(token);

                        if (entry.StartsWith("@", StringComparison.Ordinal))
                        {
                            if (InfFileNameEquals(entry.Substring(1), expectedFileName))
                            {
                                return true;
                            }
                        }
                        else if (entry.Length > 0)
                        {
                            copySections.Add(entry);
                        }
                    }
                }
                else if (key.Equals("ServiceBinary", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("CatalogFile", StringComparison.OrdinalIgnoreCase) ||
                         key.StartsWith("CatalogFile.", StringComparison.OrdinalIgnoreCase))
                {
                    if (InfFileNameEquals(UnquoteInf(value), expectedFileName))
                    {
                        return true;
                    }
                }
            }
        }

        foreach (string sectionName in copySections)
        {
            if (!sections.TryGetValue(sectionName, out List<string> lines))
            {
                continue;
            }

            foreach (string line in lines)
            {
                string source = UnquoteInf(SplitInfList(line)[0]);

                if (InfFileNameEquals(source, expectedFileName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static HashSet<string> GetInfInstallSections(Dictionary<string, List<string>> sections)
    {
        HashSet<string> installSections = new(StringComparer.OrdinalIgnoreCase);

        if (!sections.TryGetValue("Manufacturer", out List<string> manufacturers))
        {
            return installSections;
        }

        foreach (string manufacturer in manufacturers)
        {
            if (!TrySplitInfDirective(manufacturer, out _, out string value))
            {
                continue;
            }

            List<string> parts = SplitInfList(value);
            string modelsName = UnquoteInf(parts[0]);

            for (int index = -1; index < parts.Count - 1; index++)
            {
                string modelsSection = index < 0 ? modelsName : modelsName + "." + UnquoteInf(parts[index + 1]);

                if (!sections.TryGetValue(modelsSection, out List<string> models))
                {
                    continue;
                }

                foreach (string model in models)
                {
                    if (TrySplitInfDirective(model, out _, out string modelValue))
                    {
                        string install = UnquoteInf(SplitInfList(modelValue)[0]);

                        if (install.Length > 0)
                        {
                            installSections.Add(install);
                        }
                    }
                }
            }
        }

        return installSections;
    }

    private static bool IsInfInstallSection(string sectionName, HashSet<string> installSections)
    {
        foreach (string install in installSections)
        {
            if (sectionName.Equals(install, StringComparison.OrdinalIgnoreCase) ||
                sectionName.StartsWith(install + ".", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, List<string>> ParseInfSections(string contents)
    {
        Dictionary<string, List<string>> sections = new(StringComparer.OrdinalIgnoreCase);
        List<string> current = null;
        string normalized = contents.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('\uFEFF');

        foreach (string rawLine in normalized.Split('\n'))
        {
            string line = StripInfComment(rawLine).Trim();

            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                string name = line.Substring(1, line.Length - 2).Trim();

                if (!sections.TryGetValue(name, out current))
                {
                    current = new List<string>();
                    sections.Add(name, current);
                }

                continue;
            }

            current?.Add(line);
        }

        return sections;
    }

    private static string StripInfComment(string line)
    {
        bool inQuotes = false;

        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];

            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (character == ';' && !inQuotes)
            {
                return line.Substring(0, index);
            }
        }

        return line;
    }

    private static bool TrySplitInfDirective(string line, out string key, out string value)
    {
        key = null;
        value = null;
        int separator = line.IndexOf('=');

        if (separator <= 0)
        {
            return false;
        }

        key = line.Substring(0, separator).Trim();
        value = line.Substring(separator + 1).Trim();
        return key.Length > 0;
    }

    private static List<string> SplitInfList(string value)
    {
        List<string> parts = new();
        bool inQuotes = false;
        int start = 0;

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];

            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                parts.Add(value.Substring(start, index - start));
                start = index + 1;
            }
        }

        parts.Add(value.Substring(start));
        return parts;
    }

    private static string UnquoteInf(string value)
    {
        string trimmed = value.Trim();

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
        {
            return trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        return trimmed;
    }

    private static bool InfFileNameEquals(string candidate, string expectedFileName)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        string trimmed = candidate.Trim().Trim('"');
        int separator = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
        string fileName = separator < 0 ? trimmed : trimmed.Substring(separator + 1);

        return fileName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase);
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
