
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VPadWin
{
    public sealed class MainForm : Form
    {
        private readonly CancellationTokenSource _cts;
        private readonly Button _installBtn = new() { Text = "Install Drivers", AutoSize = true };
        private readonly Button _startBtn   = new() { Text = "Start Broker",   AutoSize = true };
        private readonly Button _stopBtn    = new() { Text = "Stop Broker",    AutoSize = true, Enabled = false };
        private readonly Label  _status     = new() { Text = "Idle",           AutoSize = true };
        private readonly TextBox _log       = new() { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Width = 560, Height = 220 };

        public MainForm(CancellationTokenSource cts)
        {
            _cts = cts;
            Text = "VPad Control";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            buttons.Controls.AddRange(new Control[] { _installBtn, _startBtn, _stopBtn });

            var stack = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
            stack.Controls.Add(buttons);
            stack.Controls.Add(_status);
            stack.Controls.Add(_log);

            Controls.Add(stack);

            _installBtn.Click += async (_, __) => await InstallDriversAsync();
            _startBtn.Click   += (_, __) => StartBroker();
            _stopBtn.Click    += (_, __) => StopBroker();
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(AppendLog), text); return; }
            _log.AppendText(text);
            if (!text.EndsWith(Environment.NewLine)) _log.AppendText(Environment.NewLine);
        }

        private static IEnumerable<string> FindDriverInfs()
        {
            // 1) Prefer a "drivers" directory next to the EXE
            string baseDir = AppContext.BaseDirectory;
            string driversDir = Path.Combine(baseDir, "drivers");
            if (Directory.Exists(driversDir))
            {
                foreach (var inf in Directory.EnumerateFiles(driversDir, "*.inf", SearchOption.AllDirectories))
                    yield return inf;
            }

            // 2) If running from source tree, scan for known INF names
            string? probe = baseDir;
            for (int i = 0; i < 6 && probe is not null; i++)
            {
                var parent = Directory.GetParent(probe);
                if (parent is null) break;
                probe = parent.FullName;
                string srcDrivers = Path.Combine(probe, "src", "drivers");
                if (Directory.Exists(srcDrivers))
                {
                    foreach (var inf in Directory.EnumerateFiles(srcDrivers, "*.inf", SearchOption.AllDirectories))
                        yield return inf;
                    yield break;
                }
            }
        }

        private async Task InstallDriversAsync()
        {
            try
            {
                _status.Text = "Installing drivers...";
                _installBtn.Enabled = false;

                var infs = FindDriverInfs().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (infs.Length == 0)
                {
                    _status.Text = "No .inf files found";
                    MessageBox.Show(this, "No driver .inf files were found. Ensure the MSI deployed a 'drivers' folder next to VPadWin.exe or run from source with /src/drivers present.", "Missing drivers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AppendLog($"Found {infs.Length} INF(s):");
                foreach (var i in infs) AppendLog($"  {i}");

                int failures = 0;
                foreach (var inf in infs)
                {
                    var (exitCode, stdout, stderr) = await RunProcessAsync("pnputil.exe", $"/add-driver \"{inf}\" /install");
                    AppendLog($"pnputil exit={exitCode} for {Path.GetFileName(inf)}");
                    if (!string.IsNullOrWhiteSpace(stdout)) AppendLog(stdout.Trim());
                    if (!string.IsNullOrWhiteSpace(stderr)) AppendLog(stderr.Trim());
                    if (exitCode != 0) failures++;
                }

                if (failures == 0)
                {
                    _status.Text = "Drivers installed";
                    AppendLog("All drivers installed successfully.");
                }
                else
                {
                    _status.Text = $"Install finished with {failures} failure(s)";
                    MessageBox.Show(this, $"Driver installation finished with {failures} failure(s). See log for details.", "Install issues", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Win32Exception wex) when (wex.NativeErrorCode == 740 /*ERROR_ELEVATION_REQUIRED*/)
            {
                _status.Text = "Elevation required";
                MessageBox.Show(this, "Please run VPad Control as Administrator to install drivers.", "Admin required", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                _status.Text = "Install failed (exception)";
                MessageBox.Show(this, ex.ToString(), "Install failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _installBtn.Enabled = true;
            }
        }

        private static async Task<(int exitCode, string stdout, string stderr)> RunProcessAsync(string fileName, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            var tcs = new TaskCompletionSource<object?>();

            p.OutputDataReceived += (_, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
            p.ErrorDataReceived  += (_, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };
            p.Exited += (_, __) => tcs.TrySetResult(null);

            if (!p.Start())
                throw new InvalidOperationException($"Failed to start {fileName}");

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            await tcs.Task.ConfigureAwait(false);
            int code = p.ExitCode;
            p.Dispose();
            return (code, sbOut.ToString(), sbErr.ToString());
        }

        private void StartBroker()
        {
            try
            {
                _status.Text = "Broker starting...";
                // The broker is started in Program.cs on a background Task; this button just toggles UI state.
                _startBtn.Enabled = false;
                _stopBtn.Enabled = true;
                _status.Text = "Broker running";
            }
            catch (Exception ex)
            {
                _status.Text = "Broker start failed";
                MessageBox.Show(this, ex.ToString(), "Start failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopBroker()
        {
            try
            {
                _status.Text = "Broker stopping...";
                _stopBtn.Enabled = false;
                _startBtn.Enabled = true;
                _status.Text = "Broker stopped";
            }
            catch (Exception ex)
            {
                _status.Text = "Broker stop failed";
                MessageBox.Show(this, ex.ToString(), "Stop failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
