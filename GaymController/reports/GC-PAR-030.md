# GC-PAR-030 — Backpressure

## Summary
Implemented a bounded asynchronous queue using `System.Threading.Channels` to apply backpressure when producers outpace consumers.

## Interfaces/Contracts
- `BackpressureQueue<T>`: provides `EnqueueAsync` and `DequeueAsync` operations with built-in backpressure.

## Files Changed
- `shared/BackpressureQueue.cs`: new backpressure-aware queue implementation.
- `shared/Shared.csproj`: references `System.Threading.Channels` package.
- `tests/Shared.Tests/BackpressureQueueTests.cs`: verifies writers block when queue is full.
- `tests/Shared.Tests/Shared.Tests.csproj`: xUnit test project referencing shared library.

## Wiring Instructions
Instantiate `BackpressureQueue` with a bounded capacity and use `EnqueueAsync`/`DequeueAsync` between producer and consumer components.

## Tests & Results
- `dotnet test tests/Shared.Tests/Shared.Tests.csproj`

## Reference Used
None
