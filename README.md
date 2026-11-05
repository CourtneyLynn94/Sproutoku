# Sproutoku

![CI](https://github.com/CourtneyLynn94/Sproutoku/actions/workflows/ci.yml/badge.svg?branch=develop)

A cozy logic puzzle for Android phones and Windows desktops, built with .NET MAUI: grow one plant in every bed of the garden.

## Projects

- `src/Sproutoku.App`: MAUI app (UI only)
- `src/Sproutoku.Core`: game rules, solver and generator (no UI)
- `tests/Sproutoku.Core.Tests`: xUnit tests for Core

## Requirements

- Windows with Visual Studio and the **.NET Multi-platform App UI development** workload
- An Android phone with USB debugging (Developer options → USB debugging), or an Android emulator (Tools → Android → Android Device Manager)

## Run

In Visual Studio, set `Sproutoku.App` as the startup project, pick a target (Windows Machine or an Android device), and press F5.

From the command line:

```powershell
dotnet build src/Sproutoku.App -t:Run -f net10.0-windows10.0.19041.0
dotnet build src/Sproutoku.App -t:Run -f net10.0-android   # needs a connected phone or running emulator
```

## Test

```powershell
dotnet test tests/Sproutoku.Core.Tests
```

## Lint

```powershell
dotnet format src/Sproutoku.Core/Sproutoku.Core.csproj --verify-no-changes
dotnet format tests/Sproutoku.Core.Tests/Sproutoku.Core.Tests.csproj --verify-no-changes
```

Run the same commands without `--verify-no-changes` to fix formatting automatically.

## Build for release

**Android** (signed APK; from v0.3.0 on, each release installs as an update):
```powershell
.\scripts\publish-android.ps1
```
It asks for the keystore password. Output: `src/Sproutoku.App/bin/Release/net10.0-android/publish/` (use the `-Signed.apk`).

**Windows** (self-contained folder; zip it and run `Sproutoku.App.exe`):
```powershell
dotnet publish src/Sproutoku.App -f net10.0-windows10.0.19041.0 -c Release
```
Output: `src/Sproutoku.App/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/`

### Signing releases

Release APKs are signed with one permanent key, so each release installs as an update. **If the key is lost, players can't update and must reinstall.**

- v0.3.0 is the first release signed with this key. Releases before it used the debug key, so moving from them means uninstalling once, which clears saved progress. Every release after that installs in place.
- The keystore lives outside the repo; the `SPROUTOKU_KEYSTORE` environment variable holds its full path. The password is never stored in the repo.
- The keystore and its password are backed up in a password manager and a second location.
- Before each release, raise `ApplicationVersion` in `Sproutoku.App.csproj` (major × 10000 + minor × 100 + patch) and set `ApplicationDisplayVersion` to match.
- Without `SPROUTOKU_KEYSTORE`, Release builds fall back to the debug key, so the project still builds on machines that don't have the release key.

## Continuous integration

GitHub Actions (`.github/workflows/ci.yml`):
- **Every pull request** into `develop` or `master`: Core checks (formatting, build with warnings as errors, unit tests) and app builds for Android and Windows.
- **Every push** to `develop` or `master`: Core checks only, since the pull request already built the app. (Branch rules require pull requests, so these pushes come from merges.)

## License & Copyright

© 2026 Courtney Phillips. All rights reserved.

This project is a personal portfolio piece developed for showcase and evaluation purposes. 

* **Code and Assets:** The source code, design assets, and documentation are proprietary. No part of this repository may be reproduced, distributed, or transmitted in any form or by any means without prior written permission.
* **Review and Evaluation:** Prospective employers, recruiters, and collaborators are explicitly permitted to download, build, and run this project locally for evaluation purposes.
