# GC-PAR-023 — Auto-Sprint Toggle Node

## Summary
Implemented an Auto-Sprint node that toggles a persistent sprint mode and
activates the sprint output whenever movement exceeds a threshold.

## Interfaces/Contracts
- `InputEvent` from `shared/Mapping/INode.cs`

## Files Changed
- `shared/Mapping/Nodes.cs`: added `AutoSprintNode` implementation.
 - `tests/Mapping.Tests/AutoSprintNodeTests.cs`: unit tests validating toggle behaviour.
 - `tests/Mapping.Tests/Mapping.Tests.csproj`: xUnit test project.
- `tasks/parallel/GC-PAR-023.*`: updated task metadata for Auto-Sprint node.

## Wiring Instructions
Instantiate `AutoSprintNode`, feed it `InputEvent` values with sources
`"Move"` and `"Toggle"`, and poll `Output()` for sprint button state.

## Tests & Results
 - `dotnet test tests/Mapping.Tests/Mapping.Tests.csproj`

## Reference Used
- None
