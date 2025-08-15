# GC-PAR-025 — First-Run Wizard + Calibration

## Summary
Implemented a basic first-run calibration flow. A new `CalibrationWizard` collects calibration data through a `CalibrationService`, and `UserSettings` persists the results so the wizard runs only once.

## Interfaces/Contracts
- `UserSettings` holds `CalibrationData` and flags for first-run state.
- `CalibrationService` exposes `Calibrate()` returning `CalibrationData`.

## Files Changed
- `src/GaymController.App/UserSettings.cs`: load/save user settings and calibration.
- `src/GaymController.App/CalibrationService.cs`: stub calibration logic.
- `src/GaymController.App/UI/CalibrationWizard.cs`: simple wizard UI.
- `src/GaymController.App/UI/MainForm.cs`: triggers wizard on first launch.
- `src/GaymController.App.Tests/*`: unit tests for settings and calibration service.

## Wiring Instructions
`MainForm` automatically shows `CalibrationWizard` when `UserSettings` reports a first run. No additional wiring required.

## Tests & Results
- `dotnet test src/GaymController.App.Tests/GaymController.App.Tests.csproj`

## Reference Used
- `reference/PERFECT/OverlayForm.cs`
