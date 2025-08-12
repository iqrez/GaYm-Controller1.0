# GC-PAR-003 — Broker Wire ↔ Mock App

## Context
Work from **interfaces/** and **mocks/** only. Keep hot paths allocation-free. **Consult `reference/` first** to understand legacy behavior/feel.

## Paths to touch
- mocks/BrokerWire/BrokerWire.csproj
- mocks/BrokerWire/MockBroker.cs
- mocks/BrokerWire/MockApp.cs
- mocks/BrokerWire/WireIO.cs
- mocks/BrokerWire.Tests/BrokerWire.Tests.csproj
- mocks/BrokerWire.Tests/BrokerWireTests.cs

## Reference guidelines
- Look for any related files in `reference/originals/*`, `reference/aim/*`, or `reference/traces/*`.
- If behavior is replicated, list the files in your report.

## Steps
1) Implement per the spec.
2) Add unit/integration tests or a harness snippet.
3) Document wiring steps in your report.

## Deliverables
- Code, tests, and `reports/GC-PAR-003.json` + `.md`

## Acceptance tests
- As specified in spec and your brief; include perf targets where relevant.
