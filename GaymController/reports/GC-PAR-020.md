# GC-PAR-020 — Anti-Recoil Node

## Summary
Implemented an opt-in anti-recoil mapping node with exponential decay and unit tests validating activation and decay behaviour.

## Interfaces/Contracts
- `AntiRecoilNode` implementing `INode`
- `InputEvent` from `Shared.Mapping`

## Files Changed
- `shared/Mapping/Nodes.cs`: added opt-in flag and state management.
- `tests/Mapping.Tests/Mapping.Tests.csproj`: xUnit test project.
- `tests/Mapping.Tests/AntiRecoilNodeTests.cs`: unit tests for anti-recoil logic.

## Wiring Instructions
Instantiate `AntiRecoilNode` in the mapping graph. Set `Enabled` to `true` when the user opts in and feed `Fire` events to drive compensation.

## Tests & Results
- `dotnet test` – all tests pass.

## Reference Used
None found in `reference/`.
