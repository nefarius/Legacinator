using System;
using System.Text;

using Legacinator.Core;

using Xunit;

namespace Legacinator.Core.Tests;

public class InfAssociationTests
{
    private const string DriverFile = "driver.sys";

    [Fact]
    public void Decode_rejects_null_bytes()
    {
        Assert.Throws<ArgumentNullException>(() => InfAssociation.Decode(null!));
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-8-bom")]
    [InlineData("utf-16-le-bom")]
    [InlineData("utf-16-le")]
    public void Encoded_install_section_associates_driver_file(string encodingName)
    {
        byte[] encoded = Encode(DirectCopyInf(DriverFile), encodingName);

        Assert.True(InfAssociation.AssociatesDriverFile(encoded, DriverFile));
    }

    [Fact]
    public void Utf8_text_is_not_decoded_as_utf16()
    {
        string inf = DirectCopyInf(DriverFile);

        Assert.True(InfAssociation.AssociatesDriverFile(Encoding.UTF8.GetBytes(inf), DriverFile));
        Assert.False(InfAssociation.AssociatesDriverFile(Encoding.UTF8.GetBytes(inf), "unrelated.sys"));
    }

    [Fact]
    public void Decorated_install_section_associates_direct_copy()
    {
        const string inf = """
                           [Manufacturer]
                           %Mfg% = Models,NTamd64

                           [Models.NTamd64]
                           %Device% = Install, Root\Device

                           [Install.NTamd64]
                           CopyFiles = @driver.sys
                           """;

        Assert.True(InfAssociation.AssociatesDriverFile(inf, DriverFile));
    }

    [Fact]
    public void Copy_section_associates_listed_driver_file()
    {
        const string inf = """
                           [Manufacturer]
                           %Mfg% = Models

                           [Models]
                           %Device% = Install, Root\Device

                           [Install]
                           CopyFiles = Drivers

                           [Drivers]
                           driver.sys,,,0x00000040
                           """;

        Assert.True(InfAssociation.AssociatesDriverFile(inf, DriverFile));
    }

    [Fact]
    public void Quoted_copy_and_trailing_comment_are_honored()
    {
        const string inf = """
                           [Manufacturer]
                           %Mfg% = Models

                           [Models]
                           %Device% = Install, Root\Device

                           [Install]
                           CopyFiles = "@driver.sys" ; not a real comment target
                           """;

        Assert.True(InfAssociation.AssociatesDriverFile(inf, "DRIVER.SYS"));
    }

    [Fact]
    public void Semicolon_inside_quotes_is_not_a_comment()
    {
        const string inf = """
                           [Manufacturer]
                           %Mfg% = Models

                           [Models]
                           %Device% = Install, Root\Device

                           [Install]
                           CopyFiles = "@dri;ver.sys"
                           """;

        Assert.True(InfAssociation.AssociatesDriverFile(inf, "dri;ver.sys"));
        Assert.False(InfAssociation.AssociatesDriverFile(inf, "dri"));
    }

    [Theory]
    [InlineData("ServiceBinary", @"C:\Windows\System32\drivers\driver.sys")]
    [InlineData("CatalogFile", "driver.cat")]
    [InlineData("CatalogFile.NT", @"""driver.cat""")]
    [InlineData("CatalogFile.NTamd64", @"%12%\driver.cat")]
    public void Service_and_catalog_directives_match_file_name(string directive, string value)
    {
        string inf = $"""
                      [Version]
                      {directive} = {value}
                      """;
        string expected = value.Contains("driver.cat", StringComparison.OrdinalIgnoreCase) ? "driver.cat" : DriverFile;

        Assert.True(InfAssociation.AssociatesDriverFile(inf, expected));
    }

    [Fact]
    public void Comment_only_reference_is_not_an_association()
    {
        const string inf = """
                           [Manufacturer]
                           %Mfg% = Models

                           [Models]
                           %Device% = Install, Root\Device

                           [Install]
                           ; CopyFiles = @driver.sys
                           """;

        Assert.False(InfAssociation.AssociatesDriverFile(inf, DriverFile));
    }

    [Fact]
    public void CopyFiles_outside_install_section_is_not_an_association()
    {
        const string inf = """
                           [Version]
                           CopyFiles = @driver.sys
                           """;

        Assert.False(InfAssociation.AssociatesDriverFile(inf, DriverFile));
    }

    [Fact]
    public void Unreferenced_copy_section_is_not_an_association()
    {
        const string inf = """
                           [Manufacturer]
                           %Mfg% = Models

                           [Models]
                           %Device% = Install, Root\Device

                           [Install]
                           CopyFiles = Other

                           [Drivers]
                           driver.sys

                           [Other]
                           other.sys
                           """;

        Assert.False(InfAssociation.AssociatesDriverFile(inf, DriverFile));
        Assert.True(InfAssociation.AssociatesDriverFile(inf, "other.sys"));
    }

    [Theory]
    [InlineData("mydriver.sys")]
    [InlineData("driver.sys.bak")]
    [InlineData("driver.inf")]
    public void Similar_file_names_do_not_match(string candidate)
    {
        Assert.False(InfAssociation.AssociatesDriverFile(DirectCopyInf(candidate), DriverFile));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("this is not an inf")]
    [InlineData("[NoClosing\nCopyFiles = @driver.sys")]
    [InlineData("[Version]\nCatalogFile")]
    public void Malformed_input_does_not_associate(string? contents)
    {
        Assert.False(InfAssociation.AssociatesDriverFile(contents!, DriverFile));
        Assert.False(InfAssociation.AssociatesDriverFile(DirectCopyInf(DriverFile), " "));
    }

    [Fact]
    public void Null_encoded_inf_does_not_associate()
    {
        Assert.False(InfAssociation.AssociatesDriverFile((byte[])null!, DriverFile));
    }

    private static string DirectCopyInf(string fileName)
    {
        return $"""
                [Manufacturer]
                %Mfg% = Models,NTamd64

                [Models.NTamd64]
                %Device% = Install, Root\Device

                [Install.NTamd64]
                CopyFiles = @{fileName}
                """;
    }

    private static byte[] Encode(string contents, string encodingName)
    {
        return encodingName switch
        {
            "utf-8" => Encoding.UTF8.GetBytes(contents),
            "utf-8-bom" => Combine(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes(contents)),
            "utf-16-le-bom" => Combine(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes(contents)),
            "utf-16-le" => Encoding.Unicode.GetBytes(contents),
            _ => throw new ArgumentOutOfRangeException(nameof(encodingName), encodingName, null)
        };
    }

    private static byte[] Combine(byte[] prefix, byte[] content)
    {
        byte[] combined = new byte[prefix.Length + content.Length];
        Buffer.BlockCopy(prefix, 0, combined, 0, prefix.Length);
        Buffer.BlockCopy(content, 0, combined, prefix.Length, content.Length);
        return combined;
    }
}
