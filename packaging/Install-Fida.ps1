param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\Fida')
)

$ErrorActionPreference = 'Stop'
$payload = Join-Path $PSScriptRoot 'payload'

if (-not (Test-Path -LiteralPath (Join-Path $payload 'Fida.exe'))) {
    throw 'Installation files are missing. Extract the complete package before running this installer.'
}

$runtimeInstalled = (& dotnet --list-runtimes 2>$null | Where-Object { $_ -match '^Microsoft\.NETCore\.App 8\.' })
if (-not $runtimeInstalled) {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        'Fida requires the Microsoft .NET 8 Desktop Runtime. Install it, then run this installer again.',
        'Fida setup', 'OK', 'Information'
    ) | Out-Null
    Start-Process 'https://dotnet.microsoft.com/download/dotnet/8.0'
    exit 1
}

New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $payload '*') -Destination $InstallDirectory -Recurse -Force

$shell = New-Object -ComObject WScript.Shell
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Fida.lnk'
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $InstallDirectory 'Fida.exe'
$shortcut.WorkingDirectory = $InstallDirectory
$shortcut.IconLocation = Join-Path $InstallDirectory 'Fida.exe'
$shortcut.Save()

Start-Process -FilePath (Join-Path $InstallDirectory 'Fida.exe') -WorkingDirectory $InstallDirectory
