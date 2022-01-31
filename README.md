<img src="assets/NSS-128x128.png" align="right" />

# Nefarius' Legacinator

The one and only Legacinator.

## About

Ever wondered why that darn game controller just won't show up in games, Steam or your favourite emulator? Machine crashes once you plug in a DualShock 3/4? Struggling with getting that lid off of your pickles jar? Well, I can't help you with that, but for the game controller issues, read on!

The Legacinator is a simple self-contained .NET 4.8 application that has one task: it scans your machine for traces of known legacy (outdated, potentially faulty) device drivers (leftovers from [ScpToolkit](https://github.com/nefarius/ScpToolkit), outdated [ViGEmBus](https://github.com/ViGEm/ViGEmBus) copies etc.) and provide a solution to the user.

## Usage examples

If you run the tool on a machine with legacy components, the window will fill up with one or more tiles listing components found:

![vmware_YnimgAtbM5.png](assets/vmware_YnimgAtbM5.png)

Clicking them will open a link to online articles on how to get rid of them in a safe and supported way.

On a "clean" machine you simply get a success dialog:

![explorer_IQFdg4ziCh.png](assets/explorer_IQFdg4ziCh.png)

## Download

[Get the lastes build from here](https://ci.appveyor.com/api/projects/nefarius/legacinator/artifacts/bin%2FRelease%2FLegacinator.exe). If this doesn't work, [check the Rleases page](releases/latest).
