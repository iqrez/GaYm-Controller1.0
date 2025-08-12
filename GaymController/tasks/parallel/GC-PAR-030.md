# GC-PAR-030 — Backpressure

## Context
Work from **interfaces/** and **mocks/** only. Keep hot paths allocation-free. **Consult `reference/` first** to understand legacy behavior/feel.

## Paths to touch
- shared/BackpressureQueue.cs
- shared/Shared.csproj
- tests/Shared.Tests/BackpressureQueueTests.cs
- tests/Shared.Tests/Shared.Tests.csproj
- reports/GC-PAR-030.md
- reports/GC-PAR-030.json

## Reference guidelines
- Look for any related files in `reference/originals/*`, `reference/aim/*`, or `reference/traces/*`.
- If behavior is replicated, list the files in your report.

## Steps
1) Implement per the spec. 
2) Add unit/integration tests or a harness snippet.
3) Document wiring steps in your report.

## Deliverables
- Code, tests, and `reports/GC-PAR-030.json` + `.md`

## Acceptance tests
- As specified in spec and your brief; include perf targets where relevant.
