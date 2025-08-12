# GC-PAR-004 — Mapping Graph Kernel Skeleton

## Summary
Added a minimal kernel-style mapping graph interface and a mock in-memory
implementation for testing.

## Interfaces/Contracts
- interfaces/MappingGraphKernel.cs — defines `IMappingGraphKernel` for adding
  nodes, connecting them, dispatching events, and ticking.

## Files Changed
- interfaces/MappingGraphKernel.cs: new interface definition.
- mocks/MappingGraphKernel/MappingGraphKernel.csproj: mock library project.
- mocks/MappingGraphKernel/MappingGraphKernel.cs: mock implementation.
- mocks/MappingGraphKernel.Tests/MappingGraphKernel.Tests.csproj: xUnit test project.
- mocks/MappingGraphKernel.Tests/MappingGraphKernelTests.cs: basic dispatch/tick tests.
- tasks/parallel/GC-PAR-004.md: noted touched paths.

## Wiring Instructions
Reference `mocks/MappingGraphKernel` from components needing a graph engine or
replace with a real kernel implementation later.

## Tests & Results
- `dotnet test mocks/MappingGraphKernel.Tests` – verifies dispatch and tick logic.

## Reference Used
- reference/PERFECT/Mapping/InputRouter.cs
