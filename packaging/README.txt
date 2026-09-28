Fida Windows installer package

1. Extract the complete ZIP to a folder.
2. Run Install-Fida.ps1 from that folder (right-click > Run with PowerShell).
3. The installer creates a Start Menu shortcut and launches Fida.

Microsoft .NET 8 Desktop Runtime is required. If it is missing, the installer
opens the official .NET download page. The Windows package currently uses the
existing Release build because this machine's .NET SDK workload resolver is
incomplete and cannot rebuild the desktop project here.
