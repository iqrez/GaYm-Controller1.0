# GC-PAR-001 — Wire Pack (C#) + Golden Frames

## Summary
Implemented C# wire message packers for all protocol types and added golden frame unit tests.

## Interfaces/Contracts
- shared/Contracts/Wire.cs

## Files Changed
- shared/Contracts/Wire.cs: added `MsgType` enum and packing helpers.
- tests/WireTests/WirePackTests.cs: golden frame tests for hello, state, and rumble.
- tests/WireTests/WireTests.csproj: xUnit test project referencing Shared.

## Wiring Instructions
Call the appropriate `Wire.Pack*` method with a span buffer before sending frames over the broker pipe.

## Tests & Results
- `dotnet test tests/WireTests/WireTests.csproj`

## Reference Used
- reference/README.md
- interfaces/wire.json
