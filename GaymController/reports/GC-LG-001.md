# GC-LG-001 — Extract Core Translator & Implement ILegacyMouseAim

## Summary
Ported the legacy mouse aim curve processor into a standalone plugin and verified parity against the original math with a harness.

## Interfaces/Contracts
- ILegacyMouseAim: mouse delta to stick conversion with tunable parameters.

## Files Changed
- plugins/LegacyMouseAim.Legacy/ILegacyMouseAim.cs: defines the translator interface.
- plugins/LegacyMouseAim.Legacy/LegacyMouseAim.cs: legacy curve processor port.
- plugins/LegacyMouseAim.Legacy/LegacyMouseAim.Legacy.csproj: project file.
- tools/LegacyAimHarness/*: validation harness and sample traces.
- reports/GC-LG-001.*: task report metadata.

## Wiring Instructions
Reference `LegacyMouseAim.Legacy` from the mapping layer and call `LegacyMouseAim.ToStick` with raw mouse deltas to obtain stick values.

## Tests & Results
`dotnet run --project tools/LegacyAimHarness` compared plugin output against a baseline with zero error across sample traces.

## Reference Used
reference/PERFECT/Processing/CurveProcessor.cs
reference/PERFECT/Processing/StickMapper.cs
