# GC-PAR-003 — Broker Wire ↔ Mock App

## Summary
Implemented a mock broker and client app exercising the wire protocol over named pipes. Supports HELLO, OPEN_CONTROLLER, SET_STATE and CLOSE_CONTROLLER with allocation-free hot paths.

## Interfaces/Contracts
- interfaces/wire.json
- shared/Contracts/GamepadState.cs
- shared/Contracts/Wire.cs

## Files Changed
- mocks/BrokerWire/BrokerWire.csproj: project for mock wire components
- mocks/BrokerWire/MockBroker.cs: mock broker processing wire frames
- mocks/BrokerWire/MockApp.cs: client helper for talking to broker
- mocks/BrokerWire/WireIO.cs: pooled read/write helpers for frames
- mocks/BrokerWire.Tests/BrokerWire.Tests.csproj: test project
- mocks/BrokerWire.Tests/BrokerWireTests.cs: integration test verifying round-trip

## Wiring Instructions
Reference `mocks/BrokerWire` from components needing broker communication. Run `MockBroker` and connect via `MockApp` using the same pipe name to exercise the protocol.

## Tests & Results
- `dotnet test` : passed for BrokerWire.Tests

## Reference Used
- reference/k/docs/EMBEDDING.md
