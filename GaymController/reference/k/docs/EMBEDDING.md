# Embedding VPad Broker in another project

This broker exposes a simple Named Pipe server (`\\.\\pipe\\VPadBroker`) that forwards controller commands to the VirtualPad driver(s). It supports a fake mode for development/CI when drivers are not installed.

This guide shows how to reuse (embed) the broker into another .NET application or service (e.g., a ViGEm-based project).

## What you embed

From `src/service/VPadBroker/Program.cs` (namespace `VPad.Broker`):
- public interface `IVPadLogger`
- public class `VPadBrokerServer`
- public record `VPadBrokerOptions`
- public class `LoggerAdapter` (adapts Microsoft.Extensions.Logging)
- internal static class `Native` (driver P/Invoke + fake backend)
- internal enum `BrokerCommand` (protocol)

You can either reference the compiled project or copy these types into your solution.

## Option A: Reference the project

1) Add a project reference to your app:
   - Edit your app `.csproj`:
     ```xml
     <ItemGroup>
       <ProjectReference Include="..\..\src\service\VPadBroker\VPadBroker.csproj" />
     </ItemGroup>
     ```

2) Start the broker from your host app (minimal):
   ```csharp
   using System.Threading;
   using VPad.Broker;

   sealed class MyLogger : IVPadLogger
   {
       public void Information(string m, params object?[] a) => System.Console.WriteLine($"[INFO] {m}");
       public void Debug(string m, params object?[] a)       => System.Console.WriteLine($"[DBG ] {m}");
       public void Error(System.Exception ex, string m)      => System.Console.WriteLine($"[ERR ] {m}: {ex}");
       public void Fatal(System.Exception ex, string m)      => System.Console.WriteLine($"[FATL] {m}: {ex}");
       public void Dispose() {}
   }

   using var logger = new MyLogger();
   var server = new VPadBrokerServer(logger, new VPadBrokerOptions("VPadBroker") { MaxInstances = 50 });
   var cts = new CancellationTokenSource();
   var _ = System.Threading.Tasks.Task.Run(() => server.Run(cts.Token));
   // Later
   cts.Cancel();
   ```

3) Use the existing client (if any) to talk to the pipe `\\.\\pipe\\VPadBroker`.

### Option A.1: Generic Host setup with ILogger

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPad.Broker;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IVPadLogger>(sp => new LoggerAdapter(sp.GetRequiredService<ILoggerFactory>().CreateLogger("VPad.Broker")));
        services.AddSingleton(sp => new VPadBrokerServer(sp.GetRequiredService<IVPadLogger>(), new VPadBrokerOptions("VPadBroker")));
        services.AddHostedService<BrokerHostedService>();
    })
    .RunConsoleAsync();
```

## Option B: Copy the code

- Copy the following items from `src/service/VPadBroker/Program.cs` into your solution (ideally into a new file `VPadBroker.cs`):
  - namespace `VPad.Broker`
  - `IVPadLogger`, `VPadBrokerServer`, `VPadBrokerOptions`, `LoggerAdapter`, `Native`, `BrokerCommand`
  - Optionally copy `ConsoleVPadLogger` for quick testing.
- Ensure your project targets `net8.0-windows` and has `UseWindowsForms` or `UseWPF` disabled unless needed.

## Fake mode (no drivers required)

- Set environment variable `VPAD_FAKE=1` before starting the broker. In this mode, the broker emulates driver behavior:
  - `SetState` updates an internal state and makes `GetRumble` echo trigger values with an incrementing sequence.
  - LEDs are stored and can be retrieved with `GetLeds`.

## Protocol overview

Commands are exchanged over the pipe as little-endian binary:
- Write: [byte Command][int Index][payload...]
- Responses: command-dependent minimal payload

Important commands:
- `Version` (1): returns `uint 0x00010003`
- `Count` (2): returns `uint` number of pads
- `Create` (3), `Destroy` (4)
- `SetState` (5): buttons/axes/trigger payload
- `GetRumble` (6): returns [uint Sequence][byte Left][byte Right]
- `SetLeds` (7), `GetLeds` (8)
- Bus mgmt: `PadCountGet` (20), `PadCountSet` (21), `Rescan` (22)

See `tests/VPadBroker.Tests/BrokerTests.cs` for a minimal working client.

## Notes when embedding

- Threading: `VPadBrokerServer.Run` blocks the calling thread; run it on a background Task or a dedicated thread.
- Shutdown: pass a `CancellationToken` and cancel to stop accepting new connections. Existing clients finish naturally.
- Logging: implement `IVPadLogger` (IDisposable) to route logs to your framework (Serilog, NLog, ETW, etc.). Or use `LoggerAdapter` with `ILogger`.
- Options: use `VPadBrokerOptions` to set pipe name and max instances.
- Namespace: all public embed types live under `VPad.Broker` to avoid collisions.
- Security: Named pipes are local-machine only. If you need ACLs, wrap `NamedPipeServerStream` creation with appropriate `PipeSecurity` (not included here).

## Quick test

- Build solution, then run tests:
  ```powershell
  dotnet test .\tests\VPadBroker.Tests\VPadBroker.Tests.csproj -c Debug -v minimal
  ```
- Or run the broker directly in fake mode:
  ```powershell
  $env:VPAD_FAKE = '1'
  dotnet .\src\service\VPadBroker\bin\x64\Debug\net8.0-windows\VPadBroker.dll
  ```

You should see a log line indicating it is listening on `\\.\\pipe\\VPadBroker`.
