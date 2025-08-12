# GC-PAR-019 — Curve Editor + LUT

## Summary
Implemented a CurveEditor control supported by a reusable CurveLutBuilder that handles exponential, Bezier, cubic, and custom point curves. The editor renders at ~60FPS and exports a 256-entry lookup table.

## Interfaces/Contracts
- `CurveMode` enum selects the curve type.
- `CurveLutBuilder.ExportLut()` returns a 256-byte LUT of the current curve.

## Files Changed
- shared/CurveLutBuilder.cs: core curve logic and LUT generation.
- src/GaymController.App/UI/CurveEditor.cs: control wrapper for editing and previewing curves.
- tests/GaymController.App.Tests/CurveEditorTests.cs: unit test validating LUT generation.

## Wiring Instructions
Instantiate `CurveEditor` within the UI and invoke `ExportLut()` to obtain the lookup table for the selected curve.

## Tests & Results
- `dotnet test` — verified LUT export for expo identity curve.

## Reference Used
- None
