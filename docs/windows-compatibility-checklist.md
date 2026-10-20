# Windows compatibility checklist

Legacinator remains a .NET Framework 4.8 WPF application for Windows 10 or later. CI builds that executable on Windows, runs the portable rule tests on Linux and Windows, and uploads `Legacinator.exe` with `Legacinator.exe.config`. It cannot safely remove drivers on a shared runner. Use a disposable virtual machine and a snapshot for every destructive check below.

Take the snapshot after installing the fixture and before launching Legacinator. Restore it after each fix. Do not run two remediation cases on the same dirty machine.

## Every run

- Launch the signed or locally built executable and confirm the elevation prompt appears.
- Confirm the process targets .NET Framework 4.8 and does not require a separately installed modern .NET runtime.
- Let the first scan finish. A clean machine shows the "All good" dialog and creates no fix tiles.
- Repeat with the network disconnected. The scan still completes, and a failed update check does not report that an update is available.
- Run once from a normal parent process and once from a parent whose process name contains `updater`.

## Current-driver negatives

These installs must not produce a removal tile or change the driver:

- Current ViGEmBus
- Current HidHide
- BthPS3 version 2 or newer
- DsHidMini version 2 or newer

## Legacy fixtures

For each row, record the tile text, then test detection only, a manual tile click, and updater-parent mode where a fix is queued.

| Fixture | Expected result |
| --- | --- |
| ScpToolkit DS3, DS4, Bluetooth, and virtual bus leftovers | Each supported remnant is detected. Removal stays inside the known ScpToolkit INF set. |
| HidGuardian class filter and root device | The class filter and root device are detected and can be removed. |
| ViGEmBus pre-Gen1 | The deprecated driver tile appears and removal completes. |
| Outdated ViGEmBus Gen1 | The version is shown and only that outdated Gen1 driver is removed. |
| HP ViGEmBus fork | Detection offers the mitigation link and does not remove the device. |
| Outdated HidHide, stale updater INI, and populated HidHide lists | Driver, updater, and configuration-wipe results are separate and affect only HidHide. |
| BthPS3 version 1 profile, filter, null PDO, and legacy updater task | Only version 1 components and the legacy updater task or INI are removed. |
| DsHidMini version 1 USB and Bluetooth devices | Only version 1 devices and their bound packages are removed. |
| Stale ViGEmBus updater INI for both the current and old file names | Updater mode queues the ViGEmBus fix once, not twice. |

## Failure and rollback

- Force the phantom-device refresh to exceed its timeout once. Legacinator writes `SkipDeviceRefresh=true` to `Legacinator.ini`, exits with code `-1`, and skips that refresh on the next launch.
- If updater mode completes its queued fixes, it exits with code `0`. If one of those fixes throws, it exits with code `-2`.
- Restore the snapshot after every destructive case and confirm the non-target current drivers are unchanged.
