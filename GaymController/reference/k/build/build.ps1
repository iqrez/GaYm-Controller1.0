Param(
    [ValidateSet("Debug","Release")]
    [string]$Configuration = "Release",
    [ValidateSet("x64")]
    [string]$Platform = "x64",
    [switch]$NoDrivers
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path "$RepoRoot\.."

Write-Host "=== Clean ==="
Get-ChildItem $RepoRoot -Recurse -Include bin,obj -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "=== Restore ==="
dotnet restore "$RepoRoot\InHouse-VirtualPad.sln"

Write-Host "=== Build managed ==="
dotnet build "$RepoRoot\src\service\VPadBroker\VPadBroker.csproj" -c $Configuration -p:Platform=$Platform
dotnet build "$RepoRoot\src\client\VPadCtl\VPadCtl.csproj"       -c $Configuration -p:Platform=$Platform
dotnet build "$RepoRoot\src\client\VPadWin\VPadWin.csproj"       -c $Configuration -p:Platform=$Platform

if (-not $NoDrivers) {
    Write-Host "=== Try build drivers (if WDK present) ==="
    $env:CL = "" # ensure no stale flags
    $MSBuild = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe" | Get-Item -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($MSBuild) {
        & $MSBuild "$RepoRoot\src\drivers\bus\VPadBus.vcxproj"  /p:Configuration=$Configuration /p:Platform=$Platform /m
        & $MSBuild "$RepoRoot\src\drivers\func\VPadFunc.vcxproj" /p:Configuration=$Configuration /p:Platform=$Platform /m
    } else {
        Write-Host "MSBuild not found. Skipping driver build."
    }
} else {
    Write-Host "Skipping driver build as requested."
}

Write-Host "=== Pack MSI (harvests drivers automatically) ==="
$MSBuild = "${env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MSBuild.exe"
if (-not (Test-Path $MSBuild)) { $MSBuild = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe" | Get-Item -ErrorAction SilentlyContinue | Select-Object -First 1 }
if (-not $MSBuild) { throw "MSBuild not found for WiX build." }

& $MSBuild "$RepoRoot\src\installer\msi\Installer.wixproj" /p:Configuration=$Configuration /p:Platform=$Platform

$MsiPath = "$RepoRoot\src\installer\msi\bin\$Platform\$Configuration\InHouse-VirtualPad.msi"
if (Test-Path $MsiPath) {
    Write-Host "MSI ready: $MsiPath"
} else {
    throw "MSI not produced."
}
