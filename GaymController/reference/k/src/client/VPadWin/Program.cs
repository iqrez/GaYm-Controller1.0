using System;
using System.Threading;
using System.Windows.Forms;
using VPad.Broker;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var logger = new VPad.Broker.ConsoleVPadLogger();
        var server = new VPadBrokerServer(logger, "VPadBroker");
        var cts = new CancellationTokenSource();
         _ = System.Threading.Tasks.Task.Run(() => server.Run(cts.Token));
        Application.Run(new VPadWin.MainForm(cts));
    }
}
