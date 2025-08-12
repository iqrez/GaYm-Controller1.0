# GC-PAR-002 — App Sink ↔ Mock Broker

## Summary
Implemented a minimal AppSink that speaks the wire protocol and a MockBroker used for integration tests.

## Interfaces/Contracts
- `interfaces/wire.json` message definitions
- `Shared.Contracts.GamepadState`

## Files Changed
- interfaces/AppSink/AppSink.cs: broker client with handshake, open and state send.
- interfaces/AppSink/AppSink.csproj: project file.
- mocks/MockBroker/MockBroker.cs: pipe-based broker emulator recording last state.
- mocks/MockBroker/MockBroker.csproj: project file.
- mocks/MockBroker.Tests/MockBroker.Tests.csproj: xUnit test project wiring sink and mock.
- mocks/MockBroker.Tests/AppSinkBrokerTests.cs: verifies state reaches mock broker.

## Wiring Instructions
Create a `NamedPipeClientStream`, wrap it with `AppSink`, call `HandshakeAsync` then `OpenAsync`, and push controller updates with `SetStateAsync`. For tests, `MockBroker` exposes a pipe server on the same name.

## Tests & Results
- `dotnet test mocks/MockBroker.Tests/MockBroker.Tests.csproj`

## Reference Used
- `reference/k/tests/VPadBroker.Tests/BrokerTests.cs`
