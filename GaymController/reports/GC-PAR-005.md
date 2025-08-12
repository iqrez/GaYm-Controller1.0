# GC-PAR-005 — Broker Session Manager

## Summary
Implemented a session manager that performs the HELLO handshake and manages controller sessions over a named pipe. Supports OPEN_CONTROLLER, SET_STATE and CLOSE_CONTROLLER with ACK/Error replies.

## Interfaces/Contracts
- `interfaces/wire.json` message types: HELLO, HELLO_OK, OPEN_CONTROLLER, OPEN_OK, SET_STATE, ACK, ERROR
- `Shared.Contracts.GamepadState`

## Files Changed
- `src/GaymController.Broker/GaymController.Broker.csproj`: enable cross-platform builds for testing.
- `src/GaymController.Broker/Program.cs`: conditional entry point.
- `src/GaymController.Broker/GcService.cs`: hook SessionManager per connection.
- `src/GaymController.Broker/SessionManager.cs`: implement session logic.
- `tests/GaymController.Broker.Tests/*`: unit test for handshake and state flow.

## Wiring Instructions
`GcService` creates a `SessionManager` for each incoming pipe connection; the manager handles the protocol and driver calls.

## Tests & Results
- `dotnet test tests/GaymController.Broker.Tests/GaymController.Broker.Tests.csproj`

## Reference Used
None
