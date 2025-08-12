# InHouse Virtual Pad — Repository (M0 Baseline)

This repo contains:
- KMDF bus and function drivers (HID via VHF).
- Broker service with fake backend mode for CI (`VPAD_FAKE=1`).
- CLI tool.
- Unit tests (xUnit) exercising the broker/pipe protocol without drivers.
- WiX MSI packaging.
- Tools to install drivers and service.

## Build
```pwsh
# .NET apps
pwsh -NoProfile -c "dotnet build .\src\service\VPadBroker -c Release"
pwsh -NoProfile -c "dotnet build .\src\client\VPadCtl -c Release"

# Drivers (requires MSBuild + WDK toolset OR custom fallback headers). If building with only fallback
# headers (no WDK), ensure vcxproj has <TargetExt>.sys</TargetExt> and <Link><Driver>Driver</Driver></Link>
# as already configured in repo to emit .sys binaries.
msbuild .\InHouse-VirtualPad.sln -t:Build -p:Configuration=Release -p:Platform=x64
```

## Run broker in FAKE mode (no drivers required)
```pwsh
$env:VPAD_FAKE=1
.\src\service\VPadBroker\bin\Release\net8.0-windows\VPadBroker.exe
```

## Use CLI
```pwsh
.\src\client\VPadCtl\bin\Release\net8.0-windows\VPadCtl.exe version
.\src\client\VPadCtl\bin\Release\net8.0-windows\VPadCtl.exe count
.\src\client\VPadCtl\bin\Release\net8.0-windows\VPadCtl.exe demo 0 5
```

## MSI packaging
The MSI includes drivers (.inf/.sys), the broker service, CLI, and tools.
Build the MSI:
```pwsh
msbuild .\src\installer\msi\Installer.wixproj -p:Configuration=Release -p:Platform=x64
```
MSI expects Release binaries located at:
- Drivers: `src/drivers/*/x64/Release/*.sys`
- Broker: `src/service/VPadBroker/bin/Release/net8.0-windows/VPadBroker.exe`
- CLI: `src/client/VPadCtl/bin/Release/net8.0-windows/VPadCtl.exe`

## Install/test scripts
```pwsh
# Install drivers (uses pnputil by default)
.\tools\install-drivers.ps1
# Or with devcon
.\tools\install-drivers.ps1 -UseDevcon

# Install broker service
.\tools\install-service.ps1

# Uninstall drivers
.\tools\uninstall-drivers.ps1
```

## Driver signing (test cert example)
```pwsh
# Create a test certificate and export to PFX (one-time)
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=InHouse Test Driver" -KeyUsage DigitalSignature -KeyExportPolicy Exportable -CertStoreLocation Cert:\CurrentUser\My
$mypwd = Read-Host -AsSecureString "PFX password"
Export-PfxCertificate -Cert $cert -FilePath "$env:USERPROFILE\certs\testdriver.pfx" -Password $mypwd

# Sign binaries
.\tools\signing.ps1 -File .\src\drivers\bus\x64\Release\VPadBus.sys -CertPfx "$env:USERPROFILE\certs\testdriver.pfx" -CertPassword $mypwd
.\tools\signing.ps1 -File .\src\drivers\func\x64\Release\VPadFunc.sys -CertPfx "$env:USERPROFILE\certs\testdriver.pfx" -CertPassword $mypwd
.\tools\signing.ps1 -File .\src\service\VPadBroker\bin\Release\net8.0-windows\VPadBroker.exe -CertPfx "$env:USERPROFILE\certs\testdriver.pfx" -CertPassword $mypwd
.\tools\signing.ps1 -File .\src\client\VPadCtl\bin\Release\net8.0-windows\VPadCtl.exe -CertPfx "$env:USERPROFILE\certs\testdriver.pfx" -CertPassword $mypwd
```

Notes:
- For production, use an EV code signing certificate and Hardware Dev Center submission for Windows driver attestation.
- Enable test signing for local installs if using a test certificate: `bcdedit /set testsigning on` then reboot.
