# \# Sproutoku

# 

# A cozy logic puzzle for Android and iOS, built with .NET MAUI: grow one plant in every region of the garden.

# 

# \## Projects

# \- `src/Sproutoku.App`: MAUI app (UI only)

# \- `src/Sproutoku.Core`: game rules, solver and generator (no UI)

# \- `tests/Sproutoku.Core.Tests`: xUnit tests for Core

# 

# \## Requirements

# \- Windows with Visual Studio and the \*\*.NET Multi-platform App UI development\*\* workload

# \- An Android emulator (Tools → Android → Android Device Manager) or an Android phone with USB debugging

# 

# \## Run

# In Visual Studio, set `Sproutoku.App` as the startup project, pick a target (Windows Machine or an Android device), and press F5.

# 

# From the command line:

# ```powershell

# dotnet build src/Sproutoku.App -t:Run -f net10.0-windows10.0.19041.0

# dotnet build src/Sproutoku.App -t:Run -f net10.0-android   # needs a running emulator or connected phone

# ```

# 

# \## Test

# ```powershell

# dotnet test tests/Sproutoku.Core.Tests

# ```

# 

# \## Build for release

# ```powershell

# dotnet publish src/Sproutoku.App -f net10.0-android -c Release

# ```

# Output: `src/Sproutoku.App/bin/Release/net10.0-android/publish/`

