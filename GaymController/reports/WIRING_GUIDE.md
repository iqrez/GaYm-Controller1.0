# WIRING GUIDE

## GC-PAR-030 — Backpressure

**Component:** parallel  
**Reference consulted:** False  
### Wiring Instructions

Instantiate BackpressureQueue with desired capacity and use EnqueueAsync/DequeueAsync to move items between producers and consumers.

### Files

- `shared/BackpressureQueue.cs`  `3c0bc2ff540a01a2ca1e8c62fabd6f737a878b0bb5b26c896d3a80316f1315b2`
- `shared/Shared.csproj`  `6bd69d60521dbe99657118aadb6db5cf51d34309434251827acf0054a00384df`
- `tests/Shared.Tests/BackpressureQueueTests.cs`  `e8f76ca0a4d0832d99fd0ba554ffafa2e2994414480e8203e7a0009eb13058ec`
- `tests/Shared.Tests/Shared.Tests.csproj`  `c4ff5e1fead70a67471e6ec7f91aa9ab9aee8621cb89478e066bcc5301390069`
