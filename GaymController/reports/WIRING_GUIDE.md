# WIRING GUIDE

## GC-W-002 — HID Enumeration & Path Selection

**Component:** wooting  
**Reference consulted:** False  
### Wiring Instructions

Instantiate RawHidProvider and call Start(); it selects the first supported device path via EnumerateDevicePaths.

### Files

- `src/GaymController.Wooting/GaymController.Wooting.csproj`  `2f24bbcf2c3d5124d1befdb43f55bedfa208fc8a1f74e7a5aabf09a9b438cee5`
- `src/GaymController.Wooting/RawHidProvider.cs`  `21b339f50132f99dd037e281ac53786920ec6dab00a135d4678bd2417bce137e`
- `src/GaymController.Wooting.Tests/RawHidProviderTests.cs`  `fce9872d03a86de684d59fa15ab40bfcdb4c310070431b5d2ee523b241bc7621`
- `reports/GC-W-002.md`  `a41368b7c6de6d2a8176bb65f85dfd0c920208000a964dd356af4ad1872168c5`
