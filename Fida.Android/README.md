# Fida Android host

`Fida.Core` contains the shared rules, skills, state machine, campaign data models,
campaign loader, battle setup and automated campaign controllers. Desktop and Android
reference the same core assembly.

Platform-specific work belongs in this project: touch input, Android lifecycle handling,
mobile layout, and Android resource loading. The host is intentionally thin so gameplay
logic remains shared.

Build prerequisites:

```text
dotnet workload install android
dotnet restore Fida.Android/Fida.Android.csproj
dotnet build Fida.Android/Fida.Android.csproj -f net8.0-android
```

The current environment does not have the Android workload installed, so APK compilation
must be performed after installing it or from an Android-enabled Visual Studio setup.
