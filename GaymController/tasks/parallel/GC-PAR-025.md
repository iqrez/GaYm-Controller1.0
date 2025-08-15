# GC-PAR-025 — First-Run Wizard + Calibration

## Context
Work from **interfaces/** and **mocks/** only. Keep hot paths allocation-free. **Consult `reference/` first** to understand legacy behavior/feel.

## Paths to touch
- src/GaymController.App/UserSettings.cs
- src/GaymController.App/CalibrationService.cs
- src/GaymController.App/UI/CalibrationWizard.cs
- src/GaymController.App/UI/MainForm.cs
- src/GaymController.App.Tests/GaymController.App.Tests.csproj
- src/GaymController.App.Tests/UserSettingsTests.cs
- src/GaymController.App.Tests/CalibrationServiceTests.cs

## Reference guidelines
- Look for any related files in `reference/originals/*`, `reference/aim/*`, or `reference/traces/*`.
- If behavior is replicated, list the files in your report.

## Steps
1) Implement per the spec. 
2) Add unit/integration tests or a harness snippet.
3) Document wiring steps in your report.

## Deliverables
- Code, tests, and `reports/GC-PAR-025.json` + `.md`

## Acceptance tests
- As specified in spec and your brief; include perf targets where relevant.
