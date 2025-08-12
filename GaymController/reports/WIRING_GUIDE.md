# WIRING GUIDE

## GC-LG-001 — Extract Core Translator & Implement ILegacyMouseAim

**Component:** LegacyMouseAim.Legacy plugin  
**Reference consulted:** True  
**Reference files:** reference/PERFECT/Processing/CurveProcessor.cs, reference/PERFECT/Processing/StickMapper.cs  
### Wiring Instructions

Reference LegacyMouseAim.Legacy and call ILegacyMouseAim.ToStick with raw mouse deltas.

### Files

- `plugins/LegacyMouseAim.Legacy/ILegacyMouseAim.cs`  `32cd987745b7dafd40644c8046834bd8a22e545dcea92928f986a2b1f08360cb`
- `plugins/LegacyMouseAim.Legacy/LegacyMouseAim.cs`  `81df5a1f98ae136442317671ac77f88f156266ae20c67278158ac92f2a95e533`
- `plugins/LegacyMouseAim.Legacy/LegacyMouseAim.Legacy.csproj`  `ec9be9c556c13bfe9282a77bd6fa73e0cbceb5c791ae91d3ed084592fc7a1af0`
- `tools/LegacyAimHarness/BaselineCurve.cs`  `9b18adf7e0454481564d9d262ec80c27faa7f51403a3d53b41cf6c173457d53e`
- `tools/LegacyAimHarness/Program.cs`  `843e3f9a5fb17628ae7282d214737a98c6e004a704acac43b48b7b208ed0c18c`
- `tools/LegacyAimHarness/LegacyAimHarness.csproj`  `992ac0620dfcb6810a5d9dbb7e623004bcfa194695a7dacdf66d41b058925793`
- `tools/LegacyAimHarness/sample.csv`  `97cc26936993525eea6275bf076cce92f86dd8c264db75eef0d580fe1fb21eed`
- `reports/GC-LG-001.md`  `2db66adeb852c0f91b674852db328e182bfff2a8d1eacb2808d2d97524329b38`
- `reports/GC-LG-001.json`  `2ee4c68a40d98150cb18c61cb0e39b373163b15d0ca088ad8de342fe95a0f2f3`
