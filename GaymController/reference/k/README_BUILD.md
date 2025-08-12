
# Build & Package

## Quick
Open an elevated PowerShell in the repo root and run:

```powershell
build\build.ps1 -Configuration Release -Platform x64
```

This will:
1. Restore & build **VPadBroker**, **VPadCtl**, **VPadWin**.
2. Attempt to build **drivers** (bus/func) if your WDK/VS toolchain is present.
3. Build the **WiX MSI**, **harvesting any drivers present** under `src\drivers`.

MSI output:
```
src\installer\msi\bin\x64\Release\InHouse-VirtualPad.msi
```

## Notes
- You can skip driver build with:
  ```powershell
  build\build.ps1 -NoDrivers
  ```
- The UI’s *Install Drivers* button will run `pnputil` and print the full log. Run the app as **Administrator**.
- If driver build/signing isn’t ready, the MSI still ships the `drivers` folder so INF install works from the UI.
