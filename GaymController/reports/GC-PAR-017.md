# GC-PAR-017 — Mapping Graph Runtime (no-GC)

## Summary
Implemented allocation-free mapping graph runtime with builder and traversal logic.

## Interfaces/Contracts
- `INode`
- `InputEvent`

## Files Changed
- shared/Mapping/Graph.cs: graph runtime and builder
- tests/MappingGraph.Tests/MappingGraph.Tests.csproj: test project referencing shared library
- tests/MappingGraph.Tests/GraphTests.cs: unit tests for event propagation and ticking

## Wiring Instructions
Use `GraphBuilder` to add nodes and connections, build a `Graph`, feed inputs with `OnEvent`, and invoke `Tick` each update.

## Tests & Results
- `dotnet test tests/MappingGraph.Tests/MappingGraph.Tests.csproj`

## Reference Used
- None
