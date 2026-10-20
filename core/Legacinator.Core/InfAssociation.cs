using System;
using System.Collections.Generic;
using System.Text;

namespace Legacinator.Core;

public static class InfAssociation
{
    public static string Decode(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

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

    public static bool AssociatesDriverFile(string contents, string expectedFileName)
    {
        if (string.IsNullOrWhiteSpace(contents) || string.IsNullOrWhiteSpace(expectedFileName))
        {
            return false;
        }

        return InfAssociatesDriverFile(contents, expectedFileName);
    }

    public static bool AssociatesDriverFile(byte[] encodedInf, string expectedFileName)
    {
        if (encodedInf is null)
        {
            return false;
        }

        return AssociatesDriverFile(Decode(encodedInf), expectedFileName);
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
}
