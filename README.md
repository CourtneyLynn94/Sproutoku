# Sproutoku

![CI](https://github.com/CourtneyLynn94/Sproutoku/actions/workflows/ci.yml/badge.svg?branch=develop)

A cozy logic puzzle for Android and iOS, built with .NET MAUI: grow one plant in every region of the garden.

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

```powershell
dotnet publish src/Sproutoku.App -f net10.0-android -c Release
```

Output: `src/Sproutoku.App/bin/Release/net10.0-android/publish/`

## Continuous integration

Every pull request and every push to `develop` or `master` runs the lint, build (warnings as errors) and test checks above on GitHub Actions. See `.github/workflows/ci.yml`.
