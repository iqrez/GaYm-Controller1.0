# GC-PAR-018 — Macro/Turbo Scheduler

## Summary
Implemented a lightweight scheduler to drive macro/turbo nodes without allocations. Added unit test verifying TurboNode toggling based on tick timing.

## Interfaces/Contracts
- `shared/Mapping/INode.cs` – scheduler ticks `INode` instances via `OnTick`.

## Files Changed
- `shared/Mapping/Scheduler.cs`: new allocation-free scheduler.
- `tests/Shared.Tests/TurboSchedulerTests.cs`: verifies scheduler with `TurboNode`.

## Wiring Instructions
Instantiate `Scheduler`, register mapping nodes, and call `Tick` with elapsed milliseconds from the main loop.

## Tests & Results
- `dotnet test tests/Shared.Tests/Shared.Tests.csproj`

## Reference Used
- `reference/PERFECT/Macros/MacroEngine.cs`
