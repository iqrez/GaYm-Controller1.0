#pragma warning disable CS0169, CS0414
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using WootMouseRemap.Controllers;

namespace WootMouseRemap
{
    // Renamed legacy form to avoid conflicts with the new OverlayForm in OverlayForm_Clean.cs
    internal sealed class OverlayFormLegacy : Form
    {
        private readonly Xbox360ControllerWrapper _pad;
        private readonly RawInput _raw;
        private readonly RawInputMsgWindow _msgWin;

        private readonly Telemetry _tele = new();
        private readonly ProfileManager _profiles = new();
        private readonly StickMapper _mapper = new();
        private readonly InputRouter _router;
        private readonly MacroEngine _macros;
        private readonly XInputPassthrough _xpass;

        private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 50 };
        private readonly System.Windows.Forms.Timer _savePosDebounce = new() { Interval = 800 };
        private System.Threading.Timer? _submitBg;

        // Dynamic controller detection
        private readonly System.Windows.Forms.Timer _deviceRefreshDebounce = new() { Interval = 400 };
        private readonly System.Windows.Forms.Timer _deviceWatchTimer = new() { Interval = 5000 };
        private HashSet<string> _lastControllers = new(StringComparer.OrdinalIgnoreCase);
        private IntPtr _devNotifyHid = IntPtr.Zero;

        // Cancellation tokens for background ops
        private CancellationTokenSource? _figCts;
        private CancellationTokenSource? _tuneCts;
        
        private DateTime _lastMouseMoveUtc = DateTime.UtcNow;

        // Local input snapshot for status
        private int _rawDx, _rawDy;
        private short _stickRX, _stickRY;

        private readonly SynchronizationContext _uiCtx;
        private InputMode _mode = InputMode.ControllerOutput;

        // === Advanced Layout Fields ===
        private Panel _headerPanel;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _slMode, _slInput, _slOutput, _slFps;
        private SplitContainer _mainSplit, _contentSplit;
        private Panel _sidebarPanel;
        private Button _sidebarToggle;
        private CheckBox _curveShowChk;
        private Label _opacityOutputLbl, _opacityPassLbl;
        private TrackBar _opacityOutputTrack, _opacityPassTrack;
        private TabControl _mainTabs;
        private TabPage _tabCalib, _tabMacros, _tabNotifications, _tabHotkeys;

        // Theme management
        private ThemeManager _themeManager;

        // Legacy UI (kept for compatibility during transition)
        private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
        private readonly InputVisualizerControl _viz = new() { Dock = DockStyle.Fill };
        private readonly CurvePreviewControl _curvePrev = new() { Dock = DockStyle.Fill };
        private readonly CheckBox _xinputEnable = new() { Text = "Controller Passthrough (XInput)" };
        private readonly NumericUpDown _xinputIndex = new() { Minimum = 0, Maximum = 3, Value = 0, Width = 60 };
        private readonly Label _status = new() { AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold) };
        private readonly Button _toggleMode = new() { Text = "Toggle Mode (F8/Middle)" };
        private readonly CheckBox _suppressChk = new() { Text = "Suppress OS Input (no dual input)" };
        private readonly Button _figStart = new() { Text = "Figure-8 Start" };
        private readonly Button _figStop = new() { Text = "Figure-8 Stop" };
        private readonly Button _autoTune = new() { Text = "Auto-Tune (5s)" };
        private readonly NotifyIcon _tray = new() { Visible = true, Text = "WootMouseRemap", Icon = SystemIcons.Application };
        private readonly Button _xinputToggleBtn = new() { Text = "Passthrough: Off" };

        // Overlay state
        private bool _lockPosition = false;
        private bool _compactMode = false;
        private Rectangle _normalBounds;
        private bool _alwaysOnTop = true;
        private bool _edgeSnapping = true;
        private double _outputOpacity = 0.9;
        private double _passthroughOpacity = 0.7;
        private bool _darkTheme = true;
        private Color _accentColor = Color.FromArgb(138, 43, 226);
        private List<Color> _recentAccentColors = new();

        // Per-monitor bounds persistence
        private Dictionary<string, Rectangle> _perMonitorBounds = new();

        // Win32 constants
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, int Flags);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterDeviceNotification(IntPtr Handle);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 2;

        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public char dbcc_name;
        }

        public OverlayFormLegacy(Xbox360ControllerWrapper pad, RawInput raw, RawInputMsgWindow msgWin)
        {
            _pad = pad ?? throw new ArgumentNullException(nameof(pad));
            _raw = raw ?? throw new ArgumentNullException(nameof(raw));
            _msgWin = msgWin ?? throw new ArgumentNullException(nameof(msgWin));

            _router = new InputRouter(_raw);
            _macros = new MacroEngine(_pad);
            _xpass = new XInputPassthrough(_pad);

            _uiCtx = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

            InitializeComponent();
            RebuildLayoutAdvanced();
            InitializeThemeManager();
            ConfigureWindow();
            SetupEventHandlers();
            LoadProfileAndApplyToUI();
            StartBackgroundProcessing();
            RegisterDeviceNotifications();
            RegisterHotkeys();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            
            Text = "WootMouseRemap Overlay";
            Size = new Size(800, 600);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            
            ResumeLayout(false);
        }

        private void RebuildLayoutAdvanced()
        {
            SuspendLayout();
            Controls.Clear();

            // Initialize theme manager first
            _themeManager = new ThemeManager();

            // Header panel with title and controls
            _headerPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = _darkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(246, 246, 246)
            };

            var titleLbl = new Label
            {
                Text = "WootMouseRemap",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = _darkTheme ? Color.White : Color.Black,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _sidebarToggle = new Button
            {
                Text = "◀",
                Size = new Size(30, 25),
                Location = new Point(_headerPanel.Width - 40, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat
            };
            _sidebarToggle.FlatAppearance.BorderSize = 0;
            _sidebarToggle.Click += (s, e) => ToggleSidebar();

            _headerPanel.Controls.AddRange(new Control[] { titleLbl, _sidebarToggle });

            // Status strip at bottom
            _statusStrip = new StatusStrip
            {
                BackColor = _darkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(246, 246, 246)
            };

            _slMode = new ToolStripStatusLabel("Mode: Output") { Spring = false };
            _slInput = new ToolStripStatusLabel("Input: 0,0") { Spring = false };
            _slOutput = new ToolStripStatusLabel("Output: 0,0") { Spring = false };
            _slFps = new ToolStripStatusLabel("FPS: 0") { Spring = true, TextAlign = ContentAlignment.MiddleRight };

            _statusStrip.Items.AddRange(new ToolStripItem[] { _slMode, _slInput, _slOutput, _slFps });

            // Main split container
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 200,
                FixedPanel = FixedPanel.Panel1
            };

            // Sidebar panel (left side)
            _sidebarPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _darkTheme ? Color.FromArgb(37, 37, 38) : Color.FromArgb(240, 240, 240),
                Padding = new Padding(8)
            };

            // Sidebar controls
            var y = 10;
            
            // Mode toggle
            _toggleMode.Location = new Point(8, y);
            _toggleMode.Size = new Size(180, 30);
            _toggleMode.Click += ToggleMode_Click;
            _sidebarPanel.Controls.Add(_toggleMode);
            y += 35;

            // Curve preview toggle
            _curveShowChk = new CheckBox
            {
                Text = "Show Curve Preview",
                Location = new Point(8, y),
                Size = new Size(180, 25),
                Checked = true
            };
            _curveShowChk.CheckedChanged += (s, e) => ToggleCurveVisibility();
            _sidebarPanel.Controls.Add(_curveShowChk);
            y += 30;

            // Opacity controls
            _opacityOutputLbl = new Label
            {
                Text = "Output Opacity:",
                Location = new Point(8, y),
                Size = new Size(180, 20)
            };
            _sidebarPanel.Controls.Add(_opacityOutputLbl);
            y += 25;

            _opacityOutputTrack = new TrackBar
            {
                Location = new Point(8, y),
                Size = new Size(180, 45),
                Minimum = 10,
                Maximum = 100,
                Value = (int)(_outputOpacity * 100),
                TickFrequency = 10
            };
            _opacityOutputTrack.ValueChanged += (s, e) => 
            {
                _outputOpacity = _opacityOutputTrack.Value / 100.0;
                if (_mode == InputMode.ControllerOutput) UpdateFormOpacity();
            };
            _sidebarPanel.Controls.Add(_opacityOutputTrack);
            y += 50;

            _opacityPassLbl = new Label
            {
                Text = "Passthrough Opacity:",
                Location = new Point(8, y),
                Size = new Size(180, 20)
            };
            _sidebarPanel.Controls.Add(_opacityPassLbl);
            y += 25;

            _opacityPassTrack = new TrackBar
            {
                Location = new Point(8, y),
                Size = new Size(180, 45),
                Minimum = 10,
                Maximum = 100,
                Value = (int)(_passthroughOpacity * 100),
                TickFrequency = 10
            };
            _opacityPassTrack.ValueChanged += (s, e) => 
            {
                _passthroughOpacity = _opacityPassTrack.Value / 100.0;
                if (_mode == InputMode.ControllerPassthrough) UpdateFormOpacity();
            };
            _sidebarPanel.Controls.Add(_opacityPassTrack);
            y += 50;

            // XInput controls
            _xinputEnable.Location = new Point(8, y);
            _xinputEnable.Size = new Size(180, 25);
            _sidebarPanel.Controls.Add(_xinputEnable);
            y += 30;

            var xinputLbl = new Label
            {
                Text = "XInput Index:",
                Location = new Point(8, y),
                Size = new Size(100, 20)
            };
            _sidebarPanel.Controls.Add(xinputLbl);

            _xinputIndex.Location = new Point(110, y);
            _xinputIndex.Size = new Size(60, 25);
            _sidebarPanel.Controls.Add(_xinputIndex);
            y += 35;

            // Always On Top toggle
            var aotChk = new CheckBox
            {
                Text = "Always On Top",
                Location = new Point(8, y),
                Size = new Size(180, 25),
                Checked = _alwaysOnTop
            };
            aotChk.CheckedChanged += (s, e) => 
            {
                _alwaysOnTop = aotChk.Checked;
                TopMost = _alwaysOnTop;
                SaveOverlayPrefsToProfile();
            };
            _sidebarPanel.Controls.Add(aotChk);
            y += 30;

            // Lock Position toggle
            var lockChk = new CheckBox
            {
                Text = "Lock Position",
                Location = new Point(8, y),
                Size = new Size(180, 25),
                Checked = _lockPosition
            };
            lockChk.CheckedChanged += (s, e) => 
            {
                _lockPosition = lockChk.Checked;
                SaveOverlayPrefsToProfile();
            };
            _sidebarPanel.Controls.Add(lockChk);

            _mainSplit.Panel1.Controls.Add(_sidebarPanel);

            // Content split container (right side)
            _contentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            // Main tabs (top of content)
            _mainTabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Calibration tab
            _tabCalib = new TabPage("Calibration");
            var calibPanel = new Panel { Dock = DockStyle.Fill };
            
            // Add visualizer and calibration controls
            var vizPanel = new Panel { Dock = DockStyle.Fill };
            vizPanel.Controls.Add(_viz);
            
            var calibButtonPanel = new Panel { Height = 50, Dock = DockStyle.Bottom };
            _figStart.Location = new Point(10, 10);
            _figStart.Size = new Size(100, 30);
            _figStart.Click += FigStart_Click;
            
            _figStop.Location = new Point(120, 10);
            _figStop.Size = new Size(100, 30);
            _figStop.Click += FigStop_Click;
            
            _autoTune.Location = new Point(230, 10);
            _autoTune.Size = new Size(100, 30);
            _autoTune.Click += AutoTune_Click;
            
            calibButtonPanel.Controls.AddRange(new Control[] { _figStart, _figStop, _autoTune });
            
            calibPanel.Controls.AddRange(new Control[] { vizPanel, calibButtonPanel });
            _tabCalib.Controls.Add(calibPanel);

            // Macros tab
            _tabMacros = new TabPage("Macros");
            var macroLbl = new Label 
            { 
                Text = "Macro Engine UI - Coming Soon", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            _tabMacros.Controls.Add(macroLbl);

            // Notifications tab
            _tabNotifications = new TabPage("Notifications");
            var notifyLbl = new Label 
            { 
                Text = "Notification System - Coming Soon", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            _tabNotifications.Controls.Add(notifyLbl);

            // Hotkeys tab
            _tabHotkeys = new TabPage("Hotkeys");
            var hotkeyLbl = new Label 
            { 
                Text = "Hotkey Manager - Coming Soon", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            _tabHotkeys.Controls.Add(hotkeyLbl);

            _mainTabs.TabPages.AddRange(new TabPage[] { _tabCalib, _tabMacros, _tabNotifications, _tabHotkeys });
            _contentSplit.Panel1.Controls.Add(_mainTabs);

            // Curve preview panel (bottom of content)
            var curvePanel = new Panel { Dock = DockStyle.Fill };
            curvePanel.Controls.Add(_curvePrev);
            _contentSplit.Panel2.Controls.Add(curvePanel);

            _mainSplit.Panel2.Controls.Add(_contentSplit);

            // Add main containers to form
            Controls.AddRange(new Control[] { _mainSplit, _headerPanel, _statusStrip });

            // Apply theme to new controls
            _themeManager?.ApplyTheme(_darkTheme);

            ResumeLayout(false);
        }

        private void InitializeThemeManager()
        {
            _themeManager = new ThemeManager();
            
            // Register themable controls
            _themeManager.RegisterControl(_headerPanel);
            _themeManager.RegisterControl(_statusStrip);
            _themeManager.RegisterControl(_sidebarPanel);
            _themeManager.RegisterControl(_mainTabs);
            _themeManager.RegisterControl(_viz);
            _themeManager.RegisterControl(_curvePrev);
        }

        private void ConfigureWindow()
        {
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            ShowInTaskbar = false;
            TopMost = _alwaysOnTop;
            Opacity = _mode == InputMode.ControllerOutput ? _outputOpacity : _passthroughOpacity;
        }

        private void SetupEventHandlers()
        {
            Load += OverlayForm_Load;
            FormClosing += OverlayForm_FormClosing;
            MouseDown += OverlayForm_MouseDown;
            Resize += OverlayForm_Resize;
            LocationChanged += OverlayForm_LocationChanged;

            _uiTimer.Tick += UiTimer_Tick;
            _savePosDebounce.Tick += SavePosDebounce_Tick;
            _deviceRefreshDebounce.Tick += DeviceRefreshDebounce_Tick;
            _deviceWatchTimer.Tick += DeviceWatchTimer_Tick;

            // Tray menu
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("Hide", null, (s, e) => Hide());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            _tray.ContextMenuStrip = trayMenu;
            _tray.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
        }

        private void LoadProfileAndApplyToUI()
        {
            try
            {
                // Load profile logic placeholder
                // _profiles.LoadDefaultProfile();
                // ApplyProfileToUI(_profiles.CurrentProfile);
            }
            catch (Exception ex)
            {
                Logger.Info($"Error loading profile: {ex.Message}");
            }
        }

        private void StartBackgroundProcessing()
        {
            _uiTimer.Start();
            _deviceWatchTimer.Start();

            // Start background processing timer
            _submitBg = new System.Threading.Timer(BackgroundProcessing, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(5));
        }

        private void RegisterDeviceNotifications()
        {
            try
            {
                var filter = new DEV_BROADCAST_DEVICEINTERFACE
                {
                    dbcc_size = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
                    dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                    dbcc_reserved = 0,
                    dbcc_classguid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030") // HID class
                };

                var filterPtr = Marshal.AllocHGlobal(filter.dbcc_size);
                Marshal.StructureToPtr(filter, filterPtr, false);

                _devNotifyHid = RegisterDeviceNotification(Handle, filterPtr, DEVICE_NOTIFY_WINDOW_HANDLE);

                Marshal.FreeHGlobal(filterPtr);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register device notifications: {ex.Message}");
            }
        }

        private void RegisterHotkeys()
        {
            try
            {
                RegisterHotKey(Handle, 1, 0, 0x77); // F8
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register hotkeys: {ex.Message}");
            }
        }

        private void BackgroundProcessing(object? state)
        {
            try
            {
                // Process input routing logic here
            }
            catch (Exception ex)
            {
                Logger.Error($"Background processing error: {ex.Message}");
            }
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                UpdateStatus();
                UpdateInputVisualization();
            }
            catch (Exception ex)
            {
                Logger.Error($"UI timer error: {ex.Message}");
            }
        }

        private void UpdateStatus()
        {
            var modeText = _mode == InputMode.ControllerOutput ? "Output" : "Passthrough";
            var inputText = $"Input: {_rawDx},{_rawDy}";
            var outputText = $"Output: {_stickRX},{_stickRY}";
            var fpsText = $"FPS: 60"; // Placeholder

            if (_slMode != null) _slMode.Text = $"Mode: {modeText}";
            if (_slInput != null) _slInput.Text = inputText;
            if (_slOutput != null) _slOutput.Text = outputText;
            if (_slFps != null) _slFps.Text = fpsText;

            // Legacy status label
            _status.Text = $"{modeText} | {inputText} | {outputText} | {fpsText}";
        }

        private void UpdateInputVisualization()
        {
            // Update visualization - placeholder
            // _viz.UpdateVisualization(_rawDx, _rawDy, _stickRX, _stickRY);
            // _curvePrev.UpdateCurve(_profiles.CurrentProfile);
        }

        private void ToggleSidebar()
        {
            _mainSplit.Panel1Collapsed = !_mainSplit.Panel1Collapsed;
            _sidebarToggle.Text = _mainSplit.Panel1Collapsed ? "▶" : "◀";
        }

        private void ToggleCurveVisibility()
        {
            _contentSplit.Panel2Collapsed = !_curveShowChk.Checked;
            
            // Save preference - placeholder
            // if (_profiles.CurrentProfile != null)
            // {
            //     _profiles.CurrentProfile.CurveShowPanel = _curveShowChk.Checked;
            //     SaveOverlayPrefsToProfile();
            // }
        }

        private void UpdateFormOpacity()
        {
            var targetOpacity = _mode == InputMode.ControllerOutput ? _outputOpacity : _passthroughOpacity;
            Opacity = targetOpacity;
        }

        private void ToggleMode_Click(object? sender, EventArgs e)
        {
            ToggleMode();
        }

        private void ToggleMode()
        {
            _mode = _mode == InputMode.ControllerOutput ? InputMode.ControllerPassthrough : InputMode.ControllerOutput;
            UpdateFormOpacity();
            UpdateStatus();
        }

        private void FigStart_Click(object? sender, EventArgs e)
        {
            // Figure-8 calibration logic here
            Logger.Info("Figure-8 calibration started");
        }

        private void FigStop_Click(object? sender, EventArgs e)
        {
            // Stop figure-8 calibration
            Logger.Info("Figure-8 calibration stopped");
        }

        private void AutoTune_Click(object? sender, EventArgs e)
        {
            // Auto-tune logic here
            Logger.Info("Auto-tune started");
        }

        private void ApplyProfileToUI(ProfileSchemaV1? profile)
        {
            if (profile == null) return;

            try
            {
                // Apply overlay preferences - placeholder for now
                // TODO: Add overlay preference fields to ProfileSchemaV1
                _outputOpacity = 0.9;
                _passthroughOpacity = 0.7;
                _darkTheme = true;
                _alwaysOnTop = true;
                _lockPosition = false;
                _edgeSnapping = true;
                _compactMode = false;

                _accentColor = Color.FromArgb(138, 43, 226);
                _recentAccentColors = new List<Color>();

                // Apply UI state
                if (_opacityOutputTrack != null)
                    _opacityOutputTrack.Value = (int)(_outputOpacity * 100);
                
                if (_opacityPassTrack != null)
                    _opacityPassTrack.Value = (int)(_passthroughOpacity * 100);

                if (_curveShowChk != null)
                    _curveShowChk.Checked = true; // profile.CurveShowPanel;

                // Apply window state
                TopMost = _alwaysOnTop;
                UpdateFormOpacity();

                // Restore per-monitor bounds if available
                // RestorePerMonitorBounds(profile);

                // Apply theme
                _themeManager?.ApplyTheme(_darkTheme);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying profile to UI: {ex.Message}");
            }
        }

        private void RestorePerMonitorBounds(ProfileSchemaV1 profile)
        {
            // Placeholder - per-monitor bounds restoration
            // TODO: Add OverlayPerMonitorBounds to ProfileSchemaV1
        }

        private string GetCurrentMonitorName()
        {
            var screen = Screen.FromControl(this);
            return screen.DeviceName;
        }

        private void SaveOverlayPrefsToProfile()
        {
            // Placeholder for profile saving
            // TODO: Implement when ProfileManager.CurrentProfile is available
            /*
            if (_profiles.CurrentProfile == null) return;

            try
            {
                var profile = _profiles.CurrentProfile;
                
                profile.OverlayOpacityOutput = _outputOpacity;
                profile.OverlayOpacityPassthrough = _passthroughOpacity;
                profile.OverlayDarkTheme = _darkTheme;
                profile.OverlayAlwaysOnTop = _alwaysOnTop;
                profile.OverlayLockPosition = _lockPosition;
                profile.OverlayEdgeSnapping = _edgeSnapping;
                profile.OverlayCompactMode = _compactMode;
                profile.OverlayAccentColor = _accentColor;
                profile.OverlayRecentAccentColors = _recentAccentColors;
                profile.CurveShowPanel = _curveShowChk?.Checked ?? true;

                // Save per-monitor bounds
                var currentMonitor = GetCurrentMonitorName();
                profile.OverlayPerMonitorBounds ??= new Dictionary<string, Rectangle>();
                profile.OverlayPerMonitorBounds[currentMonitor] = Bounds;

                _profiles.SaveCurrentProfile();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving overlay preferences: {ex.Message}");
            }
            */
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    HandleDeviceChange(m.WParam.ToInt32());
                    break;

                case 0x0312: // WM_HOTKEY
                    if (m.WParam.ToInt32() == 1) // F8
                        ToggleMode();
                    break;
            }

            base.WndProc(ref m);
        }

        private void HandleDeviceChange(int wParam)
        {
            if (wParam == DBT_DEVICEARRIVAL || wParam == DBT_DEVICEREMOVECOMPLETE || wParam == DBT_DEVNODES_CHANGED)
            {
                _deviceRefreshDebounce.Stop();
                _deviceRefreshDebounce.Start();
            }
        }

        private void DeviceRefreshDebounce_Tick(object? sender, EventArgs e)
        {
            _deviceRefreshDebounce.Stop();
            RefreshControllerList();
        }

        private void DeviceWatchTimer_Tick(object? sender, EventArgs e)
        {
            RefreshControllerList();
        }

        private void RefreshControllerList()
        {
            try
            {
                // Controller detection logic here
                var currentControllers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // ... populate currentControllers ...

                if (!currentControllers.SetEquals(_lastControllers))
                {
                    _lastControllers = currentControllers;
                    Logger.Info($"Controllers changed: {string.Join(", ", currentControllers)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error refreshing controller list: {ex.Message}");
            }
        }

        private void SavePosDebounce_Tick(object? sender, EventArgs e)
        {
            _savePosDebounce.Stop();
            SaveOverlayPrefsToProfile();
        }

        private void OverlayForm_Load(object? sender, EventArgs e)
        {
            Logger.Info("OverlayForm loaded");
        }

        private void OverlayForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // Save current state
                SaveOverlayPrefsToProfile();

                // Cleanup
                _submitBg?.Dispose();
                _macros?.Dispose();
                _uiTimer.Stop();
                _deviceWatchTimer.Stop();
                _savePosDebounce.Stop();
                _deviceRefreshDebounce.Stop();

                // Unregister notifications and hotkeys
                if (_devNotifyHid != IntPtr.Zero)
                    UnregisterDeviceNotification(_devNotifyHid);

                UnregisterHotKey(Handle, 1);

                _tray.Visible = false;
                _tray.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during form closing: {ex.Message}");
            }
        }

        private void OverlayForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !_lockPosition)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        private void OverlayForm_Resize(object? sender, EventArgs e)
        {
            _savePosDebounce.Stop();
            _savePosDebounce.Start();
        }

        private void OverlayForm_LocationChanged(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                _savePosDebounce.Stop();
                _savePosDebounce.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _submitBg?.Dispose();
                _macros?.Dispose();
                _uiTimer?.Dispose();
                _deviceWatchTimer?.Dispose();
                _savePosDebounce?.Dispose();
                _deviceRefreshDebounce?.Dispose();
                _tray?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Rename legacy ThemeManager to prevent duplicate type
    internal class ThemeManagerLegacy
    {
        private List<Control> _controls = new();

        public void RegisterControl(Control control)
        {
            _controls.Add(control);
        }

        public void ApplyTheme(bool darkTheme)
        {
            foreach (var control in _controls)
            {
                ApplyThemeToControl(control, darkTheme);
            }
        }

        private void ApplyThemeToControl(Control control, bool darkTheme)
        {
            if (darkTheme)
            {
                control.BackColor = Color.FromArgb(45, 45, 48);
                control.ForeColor = Color.White;
            }
            else
            {
                control.BackColor = Color.FromArgb(246, 246, 246);
                control.ForeColor = Color.Black;
            }

            // Apply to child controls recursively
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, darkTheme);
            }
        }
    }
}
