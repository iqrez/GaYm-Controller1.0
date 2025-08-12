# WIRING GUIDE

## GC-PAR-005 — Broker Session Manager

**Component:** parallel  
**Reference consulted:** False  
### Wiring Instructions

GcService creates SessionManager for each pipe connection

### Files

- `src/GaymController.Broker/GaymController.Broker.csproj`  `335d5a040ee150e3a1b795891619ad07746501f5d92544f74fc579b150247cb1`
- `src/GaymController.Broker/Program.cs`  `315bdc6453841ba765d93f44fa363874ede054da5f1956e71c94fc5019ae6077`
- `src/GaymController.Broker/GcService.cs`  `25120bef1093176aa00ef431649bc43711fd74374e90e85bbf734c30ba5eb03b`
- `src/GaymController.Broker/SessionManager.cs`  `854807f43b25d77dbd767b02fb4db637d1f8439f6082841030550874d4b77ed2`
- `tests/GaymController.Broker.Tests/GaymController.Broker.Tests.csproj`  `8f804f54020850e1ac82fa6fda1c9c75e818f73e1256f36960afb8e86cf151c8`
- `tests/GaymController.Broker.Tests/BrokerSessionManagerTests.cs`  `f5a7284b0cc2181288a3dac86e8a65df285a087aa33068d5097cfa38c78d0fcd`
