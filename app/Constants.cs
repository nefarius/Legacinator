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
    public const string HidHideReleasesUri = @"https://github.com/ViGEm/HidHide/releases/latest";

    #endregion

    #region BthPS3

    public const string BthPS3HardwareId = @"BTHENUM\{1cb831ea-79cd-4508-b0fc-85f7c85ae8e0}_LOCALMFG&0000";
    public const string BthPS3RegistryPartialKey = @"SOFTWARE\Nefarius Software Solutions e.U.\BthPS3 Bluetooth Drivers";
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
    public const string ViGemBusVersion1_14HardwareId = @"Root\ViGEmBus";
    public const string ViGemBusVersion1_16HardwareId = @"Nefarius\ViGEmBus\Gen1";
    public const string ViGEmBusHPForkHardwareId = @"SWC\HPIC0004_ViGEmBus";

    #endregion

    public const string LegacinatorRepositoryUri = @"https://github.com/nefarius/Legacinator";

#if !DEBUG
    public const string LegacinatorReleasesUri = @"https://github.com/nefarius/Legacinator/releases/latest";
#endif

    

    public const string HPForkSolutionUri = @"https://github.com/ViGEm/ViGEmBus/issues/99";

    public const string HidGuardianHardwareId = @"Root\HidGuardian";

    public const string HidHideHardwareId = @"Root\HidHide";

    public const string ScpDualShock3InfName = "ds3controller.inf";

    public const string ScpDualShock4InfName = "ds4controller.inf";

    public const string ScpBluetoothInfName = "bluetoothhost.inf";

    public const string ScpVBusInfName = "scpvbus.inf";

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

    public const string HidGuardianInfName = "HidGuardian.inf";

    public const string ViGEmBusInfName = "ViGEmBus.inf";

    public static readonly Version ViGEmBusVersionLatest = new("1.21.442.0");

    public static readonly Version HidHideDriverVersionLatest = new("1.2.98.0");

    public static Guid ViGEmBusGen1InterfaceGuid = new("{96E42B22-F5E9-42F8-B043-ED0F932F014F}");

    public static Guid ScpToolkitBluetoothDongleInterfaceGuid = new("{2F87C733-60E0-4355-8515-95D6978418B2}");

    public static Guid ScpToolkitDualShock3InterfaceGuid = new("{E2824A09-DBAA-4407-85CA-C8E8FF5F6FFA}");

    public static Guid ScpToolkitDualShock4InterfaceGuid = new("{2ED90CE1-376F-4982-8F7F-E056CBC3CA71}");

    public static Guid ScpToolkitScpVBusInterfaceGuid = new("{f679f562-3164-42ce-a4db-e7ddbe723909}");

    public static Guid ScpToolkitLibusbKDeviceClassGuid = new("{ecfb0cfd-74c4-4f52-bbf7-343461cd72ac}");

    public static Guid SystemDeviceClassGuid = new("{4d36e97d-e325-11ce-bfc1-08002be10318}");

    public static Guid BluetoothDeviceClassGuid = new("{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}");
}