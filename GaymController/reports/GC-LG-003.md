# GC-LG-003 — Replay & Compare (≤0.5% error)

## Summary
Added a minimal legacy mouse aim plugin and a Python harness to replay golden traces and compare output traces with mean absolute percentage error.

## Interfaces/Contracts
- `ILegacyMouseAim`: provides `Translate` to map deltas into output coordinates.

## Files Changed
- `plugins/LegacyMouseAim.Legacy/LegacyMouseAim.Legacy.csproj`: new class library targeting .NET 8.
- `plugins/LegacyMouseAim.Legacy/LegacyMouseAim.cs`: stub implementation of legacy mouse aim translator.
- `tools/LegacyAimHarness/compare_traces.py`: harness for comparing legacy traces to plugin output.

## Wiring Instructions
Build the plugin and run:
```
python tools/LegacyAimHarness/compare_traces.py <golden.csv> <replay.csv>
```

## Tests & Results
Sample comparison using included traces:
```
python tools/LegacyAimHarness/compare_traces.py tools/LegacyAimHarness/sample_golden.csv tools/LegacyAimHarness/sample_replay.csv
```

## Reference Used
- `docs/LEGACY_AIM_PORTING.md`
