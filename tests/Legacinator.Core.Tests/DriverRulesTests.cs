using System;
using System.Collections.Generic;

using Legacinator.Core;

using Xunit;

namespace Legacinator.Core.Tests;

public class DriverRulesTests
{
    // Fragments mirror Constants.ScpInfAllowedNames.
    private static readonly string[] AllowedFragments =
    {
        "oem",
        "ds3controller.inf",
        "ds4controller.inf",
        "bluetoothhost.inf",
        "scpvbus.inf"
    };

    [Theory]
    [InlineData("1.21.441.0", "1.21.442.0", true)]
    [InlineData("1.21.442.0", "1.21.442.0", false)]
    [InlineData("1.21.442.1", "1.21.442.0", false)]
    [InlineData("1.2.97.0", "1.2.98.0", true)]
    [InlineData("1.2.98.0", "1.2.98.0", false)]
    [InlineData("1.21", "1.21.442.0", true)]
    public void IsOlderThan_uses_version_ordering(string version, string threshold, bool older)
    {
        Assert.Equal(older, DriverVersionRules.IsOlderThan(new Version(version), new Version(threshold)));
    }

    [Theory]
    [InlineData("2.0.0.0", "2.0.0.0", true)]
    [InlineData("2.0.0.1", "2.0.0.0", true)]
    [InlineData("1.9.9.9", "2.0.0.0", false)]
    [InlineData("2.0", "2.0.0.0", false)]
    public void IsAtLeast_uses_version_ordering(string version, string threshold, bool atLeast)
    {
        Assert.Equal(atLeast, DriverVersionRules.IsAtLeast(new Version(version), new Version(threshold)));
    }

    [Theory]
    [InlineData("oem123.inf", true)]
    [InlineData("OEM0.INF", true)]
    [InlineData("ds3controller.inf", true)]
    [InlineData("DS4CONTROLLER.INF", true)]
    [InlineData("prefix-bluetoothhost.inf.bak", true)]
    [InlineData("scpvbus.inf", true)]
    [InlineData("scpvbus", false)]
    [InlineData("unrelated.inf", false)]
    [InlineData("", false)]
    public void Scp_allow_list_matches_case_insensitive_fragments(string infName, bool allowed)
    {
        Assert.Equal(allowed, ScpInfRules.IsAllowed(infName, AllowedFragments));
    }

    [Fact]
    public void Scp_allow_list_preserves_null_rejection()
    {
        Assert.Throws<NullReferenceException>(() => ScpInfRules.IsAllowed(null!, AllowedFragments));
    }

    [Fact]
    public void Scp_allow_list_can_use_the_production_fragment_set()
    {
        IReadOnlyList<string> fragments = AllowedFragments;

        Assert.True(ScpInfRules.IsAllowed("oem42.inf", fragments));
        Assert.False(ScpInfRules.IsAllowed("vigembus.inf", fragments));
    }
}
