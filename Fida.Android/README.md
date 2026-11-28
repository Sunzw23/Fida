# Fida Android host

The Android app now launches the same `Game1`, UI, campaign, battle, animation, online,
and state-machine sources as the desktop game. Files are linked into the Android project,
so changes to shared screens or gameplay immediately affect both targets without copies.

Only platform adapters live here:

- `MainActivity` owns the Android lifecycle and locks the game to a portrait host view.
- `AndroidAssetContent` exposes APK assets through the shared `ContentSource` API.
- `AndroidGumContent` and `AndroidGumFileHook` unpack the Gum layout XML into the app's
  private data directory and point Gum's file loader at that copy. Gum cannot read the
  project out of the APK: it resolves the project and every screen through
  `FileManager.CustomGetStreamFromFile`, and the relative paths it composes pick up a
  second `Content/` prefix (`./Content/Content/Gum/Screens/*.gusx`), so startup used to
  throw before the first frame.
- Lightweight AI and its embedded compressed Q table are shared through `Fida.Core`.
  `Content/agent-settings.json` controls strength on both platforms. ONNX is no longer
  required; `AndroidOnnxInference` is an unused legacy adapter.
- `UIPointer` (in the shared UI folder) maps a single Android touch to the same pointer
  contract used by desktop controls and card interactions.
- Android screens use a responsive, single-column layout derived from the existing JSON
  control names. Menus, ban/pick, campaign browsing, battle cards, and modal choices all
  use large touch targets; the desktop layout and fixed parchment theme remain unchanged.
- Android room-code entry uses MonoGame's native soft-keyboard dialog.

The APK includes all screen JSON, campaign chapters, legacy scene CSV files, serif fonts,
portraits, game text, and `server.csv`. Android requests Internet permission for online play.

Build prerequisites:

```text
dotnet workload install android
dotnet workload install wasm-tools-net8
dotnet restore Fida.Android/Fida.Android.csproj
dotnet build Fida.Android/Fida.Android.csproj -f net10.0-android
```

The Android host targets `net10.0-android`, matching the .NET 10 SDK and Android workload
installed on the development machine. The workload alone is not the Android toolchain: an
Android SDK (including platform-tools and build-tools) and a full JDK are also required.

For a Visual Studio-first setup, open **Visual Studio Installer > Modify** and enable
**Mobile development with .NET**. Make sure the Android SDK, platform-tools, build-tools,
and OpenJDK components are selected. Then open the project's Android settings and verify the
Android SDK location. The SDK directory must contain `platform-tools\adb.exe`.

This machine already has a full JDK supplied by Visual Studio at:
`C:\Program Files\Android\openjdk\jdk-21.0.8`.
The IntelliJ JBR found on PATH is not a valid replacement because it does not contain
`bin\jar.exe`.

A Debug build deploys through Visual Studio's fast deployment, so the assemblies are pushed
to the device separately from the APK. To produce an `adb install`-able Debug APK, embed them:

```text
dotnet build Fida.Android/Fida.Android.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true
```

Release builds embed the assemblies and sign the APK automatically.

Useful checks before building:

```powershell
dotnet --info
dotnet workload list
Test-Path "$env:ANDROID_SDK_ROOT\platform-tools\adb.exe"
Test-Path "$env:JAVA_HOME\bin\jar.exe"
```
