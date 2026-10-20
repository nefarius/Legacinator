using System;
using System.Collections.Generic;
using System.Linq;

namespace Legacinator.Core;

public static class DriverVersionRules
{
    public static bool IsOlderThan(Version version, Version threshold)
    {
        return version < threshold;
    }

    public static bool IsAtLeast(Version version, Version threshold)
    {
        return version >= threshold;
    }
}

public static class ScpInfRules
{
    public static bool IsAllowed(string infName, IEnumerable<string> allowedFragments)
    {
        return allowedFragments.Any(allowedName => infName.ToLower().Contains(allowedName.ToLower()));
    }
}
