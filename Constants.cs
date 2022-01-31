using System;

namespace Legacinator
{
    public static class Constants
    {
        public const string HidGuardianHardwareId = @"Root\HidGuardian";

        public const string ViGemBusVersion1_14HardwareId = @"Root\ViGEmBus";

        public static Version ViGEmBusVersionLatest = new("1.17.333.0");

        public static Guid ScpToolkitBluetoothDongleInterfaceGuid = new("{2F87C733-60E0-4355-8515-95D6978418B2}");

        public static Guid ScpToolkitDualShock3InterfaceGuid = new("{E2824A09-DBAA-4407-85CA-C8E8FF5F6FFA}");

        public static Guid ScpToolkitDualShock4InterfaceGuid = new("{2ED90CE1-376F-4982-8F7F-E056CBC3CA71}");

        public static Guid ScpToolkitScpVBusInterfaceGuid = new("{f679f562-3164-42ce-a4db-e7ddbe723909}");

        public static Guid ScpToolkitLibusbKDeviceClassGuid = new("{ecfb0cfd-74c4-4f52-bbf7-343461cd72ac}");

        public static Guid SystemDeviceClassGuid = new("{4d36e97d-e325-11ce-bfc1-08002be10318}");
    }
}