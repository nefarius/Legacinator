using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Legacinator;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Constants
{
    #region HidHide

    public const string HidHideUpdaterLegacyUrl = "https://updates.vigem.org/api/github/ViGEm/HidHide/updates";
    public const string HidHideUpdaterNewUrl = "https://aiu.api.nefarius.systems/api/github/ViGEm/HidHide/updates";
    public const string HidHideRegistryPartialKey = @"SOFTWARE\Nefarius Software Solutions e.U.\HidHide";
    public const string HidHideUpdaterConfigFileName = "HidHide_Updater.ini";
    public const string HidHideReleasesUri = @"https://github.com/nefarius/HidHide/releases/latest";
    public const string HidHideHardwareId = @"Root\HidHide";
    public static readonly Version HidHideDriverVersionLatest = new("1.2.98.0");

    #endregion

    #region BthPS3

    public const string BthPS3ProfileHardwareId = @"BTHENUM\{1cb831ea-79cd-4508-b0fc-85f7c85ae8e0}";
    public const string BthPS3FilterHardwareId = @"Nefarius\{a3dc6d41-9e10-46d9-8be2-9b4a279841df}";
    public const string BthPS3ProfileServiceName = "BthPS3";
    public const string BthPS3FilterServiceName = "BthPS3PSM";
    public const string BthPS3ProfileInfName = "BthPS3.inf";
    public const string BthPS3FilterInfName = "BthPS3PSM.inf";
    public const string BthPS3LegacyFilterInfName = "BthPS3PSM_Filter.inf";
    public const string BthPS3ProfileDriverFileName = "BthPS3.sys";
    public const string BthPS3FilterDriverFileName = "BthPS3PSM.sys";
    public static readonly Guid BthPS3NullPdoClassGuid = new("{409ED5DE-6FC0-4566-BDB6-4878D965D914}");
    public const string BthPS3NullPdoInfName = "BthPS3_PDO_NULL_Device.inf";
    public const string BthPS3NullPdoCatalogName = "BthPS3_PDO_NULL_Device.cat";

    public static readonly string[] BthPS3NullPdoHardwareIds =
    {
        DsHidMiniV1BluetoothHardwareId,
        @"BTHPS3BUS\{206F84FC-1615-4D9F-954D-21F5A5D388C5}",
        @"BTHPS3BUS\{84957238-D867-421F-89C1-67847A3B55B5}",
        @"BTHPS3BUS\{13D12A06-D0B0-4D7E-8D1F-F55914A2ED7C}"
    };

    // v1 and v2 share the IDs above. Only an installed driver/file version identifies the v1 line.
    public static readonly Version BthPS3Version2 = new("2.0.0.0");

    public const string BthPS3RegistryPartialKey =
        @"SOFTWARE\Nefarius Software Solutions e.U.\BthPS3 Bluetooth Drivers";

    public const string BthPS3UpdaterConfigFileName = "BthPS3Updater.ini";
    public const string BthPS3UpdaterLegacyUrl = "https://test.updates.vigem.org/nss/products/bthps3/updates.txt";
    public const string BthPS3LatestReleaseUri = @"https://github.com/nefarius/BthPS3/releases/latest";
    public const string BthPS3UpdaterScheduledTaskName = "BthPS3Updater";

    #endregion

    #region ViGEmBus

    public const string ViGEmBusUpdaterLegacyUrl = "https://updates.vigem.org/api/github/ViGEm/ViGEmBus/updates";
    public const string ViGEmBusUpdaterNewUrl = "https://aiu.api.nefarius.systems/api/github/ViGEm/ViGEmBus/updates";
    public const string ViGEmBusRegistryPartialKey = @"SOFTWARE\Nefarius Software Solutions e.U.\ViGEm Bus Driver";
    public const string ViGEmBusUpdaterConfigFileName = "ViGEmBus_Updater.ini";
    public const string ViGEmBusOldUpdaterConfigFileName = "ViGEmBusUpdater.ini";
    public const string ViGemBusVersion1_14HardwareId = @"Root\ViGEmBus";
    public const string ViGemBusVersion1_16HardwareId = @"Nefarius\ViGEmBus\Gen1";
    public const string ViGEmBusHPForkHardwareId = @"SWC\HPIC0004_ViGEmBus";
    public const string ViGEmBusInfName = "ViGEmBus.inf";
    public static readonly Version ViGEmBusVersionLatest = new("1.21.442.0");
    public static Guid ViGEmBusGen1InterfaceGuid = new("{96E42B22-F5E9-42F8-B043-ED0F932F014F}");
    public const string HPForkSolutionUri = @"https://github.com/nefarius/ViGEmBus/issues/99";

    #endregion

    #region Misc.

    public const string LegacinatorRepositoryUri = @"https://github.com/nefarius/Legacinator";

#if !DEBUG
    public const string LegacinatorReleasesUri = @"https://github.com/nefarius/Legacinator/releases/latest";
#endif

    #endregion

    #region DsHidMini

    public static readonly Guid DsHidMiniDeviceInterfaceGuid = new("{399ED672-E0BD-4FB3-AB0C-4955B56FB86A}");
    public static readonly Guid DsHidMiniDeviceClassGuid = new("{2B959C9D-A4ED-4464-809D-0F1118CF055D}");
    public const string DsHidMiniInfName = "dshidmini.inf";
    public const string DsHidMiniDriverFileName = "dshidmini.dll";
    public const string DsHidMiniUsbHardwareId = @"USB\VID_054C&PID_0268";
    public const string DsHidMiniV1BluetoothHardwareId = @"BTHPS3BUS\{53F88889-1AAF-4353-A047-556B69EC6DA6}";

    // The interface, class, wired hardware ID, and INF name continued into v2.
    public static readonly Version DsHidMiniVersion2 = new("2.0.0.0");

    #endregion

    #region HidGuardian

    public const string HidGuardianInfName = "HidGuardian.inf";
    public const string HidGuardianHardwareId = @"Root\HidGuardian";

    #endregion

    #region SCP

    public const string ScpDualShock3InfName = "ds3controller.inf";
    public const string ScpDualShock4InfName = "ds4controller.inf";
    public const string ScpBluetoothInfName = "bluetoothhost.inf";
    public const string ScpVBusInfName = "scpvbus.inf";
    public static Guid ScpToolkitBluetoothDongleInterfaceGuid = new("{2F87C733-60E0-4355-8515-95D6978418B2}");
    public static Guid ScpToolkitDualShock3InterfaceGuid = new("{E2824A09-DBAA-4407-85CA-C8E8FF5F6FFA}");
    public static Guid ScpToolkitDualShock4InterfaceGuid = new("{2ED90CE1-376F-4982-8F7F-E056CBC3CA71}");
    public static Guid ScpToolkitScpVBusInterfaceGuid = new("{f679f562-3164-42ce-a4db-e7ddbe723909}");
    public static Guid ScpToolkitLibusbKDeviceClassGuid = new("{ecfb0cfd-74c4-4f52-bbf7-343461cd72ac}");

    private static readonly List<string> ScpInfAllowedNames = new()
    {
        "oem",
        ScpDualShock3InfName,
        ScpDualShock4InfName,
        ScpBluetoothInfName,
        ScpVBusInfName
    };

    public static bool IsAllowedScpInf(string infName)
    {
        return ScpInfAllowedNames.Any(allowedName => infName.ToLower().Contains(allowedName.ToLower()));
    }

    #endregion
}