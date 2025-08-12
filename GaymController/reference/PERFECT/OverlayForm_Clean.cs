#pragma warning disable CS0169, CS0414, CS0649
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WootMouseRemap.Controllers;

namespace WootMouseRemap
{
    public sealed partial class OverlayForm : Form
    {
        private readonly Xbox360ControllerWrapper _pad;
        private readonly RawInput _raw;
        private readonly RawInputMsgWindow _msgWin;

        private readonly ProfileManager _profiles = new();
        private readonly StickMapper _mapper = new();
        private readonly InputRouter _router;
        private readonly MacroEngine _macros;
        private readonly XInputPassthrough _xpass;

        private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 50 };
        private readonly System.Windows.Forms.Timer _savePosDebounce = new() { Interval = 800 };
        private readonly System.Windows.Forms.Timer _snapDebounce = new() { Interval = 180 }; // new: edge snap debounce

        // Dynamic controller detection
        private readonly System.Windows.Forms.Timer _deviceRefreshDebounce = new() { Interval = 400 };
        private readonly System.Windows.Forms.Timer _deviceWatchTimer = new() { Interval = 5000 };
        private HashSet<string> _lastControllers = new(StringComparer.OrdinalIgnoreCase);
        private IntPtr _devNotifyHid = IntPtr.Zero;

        // Cancellation tokens for background ops
        private CancellationTokenSource? _figCts;
        private CancellationTokenSource? _tuneCts;
        
        // Local input snapshot for status
        private int _rawDx, _rawDy;
        private short _stickRX, _stickRY;

        private readonly SynchronizationContext _uiCtx;
        // Removed legacy InputMode _mode field now that AppMode/ModeManager is primary
        private readonly ModeManager _modeManager;        private AppMode AppMode => _modeManager.Current;

        // === Advanced Layout Fields ===
        private Panel _headerPanel = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _slMode = null!, _slInput = null!, _slOutput = null!, _slFps = null!;
        private SplitContainer _mainSplit = null!, _contentSplit = null!;
        private Panel _sidebarPanel = null!;
private Button _sidebarToggle = null!;
// removed: _curveShowChk toggle (curve preview is always visible)
// removed: opacity label/trackbar
private TabControl _mainTabs = null!;
private TabPage _tabCalib = null!, _tabMacros = null!, _tabNotifications = null!, _tabHotkeys = null!;
// Added: new tabs keeping current style
private TabPage _tabDashboard = null!, _tabProfiles = null!;
        // Added feature toggle controls
        private CheckBox _chkAlwaysOnTop = null!, _chkLockPos = null!, _chkCompact = null!, _chkEdgeSnap = null!;
        // removed: dashboard suppression checkbox (suppression is hotkey/mode only)
        private Button _btnOpenLogs = null!;
        // Added: profiles controls
        private ListBox _lstProfiles = null!;
        private Button _btnProfileLoad = null!, _btnProfileNew = null!, _btnProfileClone = null!, _btnProfileDelete = null!, _btnProfileSave = null!;
        // Added: additional sidebar controls
        private NumericUpDown _numEdgeThreshold = null!;
        private ComboBox _cmbTheme = null!;
        private Button _btnAccent = null!;
        // Simple FPS tracker for StatusStrip
        private readonly System.Diagnostics.Stopwatch _fpsSw = new();
        private int _fpsFrames = 0;
        private int _fpsValue = 0;

        // Theme management
        private ThemeManager _themeManager = null!;

        // Legacy UI (kept for compatibility during transition)
        private readonly InputVisualizerControl _viz = new() { Dock = DockStyle.Fill };
        private readonly CurvePreviewControl _curvePrev = new() { Dock = DockStyle.Fill };
        // removed: passthrough checkbox per user request
        private readonly NumericUpDown _xinputIndex = new() { Minimum = 1, Maximum = 4, Value = 1, Width = 60 };
        private readonly Label _xinputStatus = new() { AutoSize = true }; // new: reflect physical controller & passthrough
        private readonly Button _toggleMode = new() { Text = "Toggle Mode (F8/Middle)" };
        private readonly NotifyIcon _tray = new() { Visible = true, Text = "WootMouseRemap", Icon = SystemIcons.Application };        
        
        // Hotkeys centralized
        private HotkeyManager _hotkeys = null!; // new manager instance

        // Overlay state
        private bool _lockPosition = false;
        private bool _compactMode = false;
        private bool _alwaysOnTop = true;
        private bool _edgeSnapping = true;
        private int _edgeSnapThreshold = 12; // new threshold storage
        private bool _darkTheme = true;
        private Color _accentColor = Color.FromArgb(138, 43, 226);
        private List<Color> _recentAccentColors = new();
        // removed: click-through state and style cache
        private DateTime _lastTrayTipUtc = DateTime.MinValue; // throttle tray tips
        
        // New: curve controls on Calibration tab
        private NumericUpDown _numSensitivity = null!, _numExpo = null!, _numAntiDead = null!, _numEma = null!, _numVelGain = null!, _numJitter = null!, _numScaleX = null!, _numScaleY = null!;
        
        // New: controllers status UI
        private Label _controllersLbl = null!;
        private ListBox _lstControllers = null!;
        
        // Win32 constants
        // removed: WS_EX_* and related constants used for click-through
        
        // Removed legacy hotkey P/Invokes (handled by HotkeyManager)
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

        // Minimal XInput connectivity probe (used for status when passthrough disabled)
        [StructLayout(LayoutKind.Sequential)] private struct XINPUT_GAMEPAD { public ushort wButtons; public byte bLeftTrigger; public byte bRightTrigger; public short sThumbLX, sThumbLY, sThumbRX, sThumbRY; }
        [StructLayout(LayoutKind.Sequential)] private struct XINPUT_STATE { public uint dwPacketNumber; public XINPUT_GAMEPAD Gamepad; }
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] private static extern int XInputGetState14(uint dwUserIndex, out XINPUT_STATE pState);
        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")] private static extern int XInputGetState13(uint dwUserIndex, out XINPUT_STATE pState);
        private static bool ProbeXInputConnected(int index)
        {
            XINPUT_STATE s;
            try { return XInputGetState14((uint)index, out s) == 0; }
            catch { try { return XInputGetState13((uint)index, out s) == 0; } catch { return false; } }
        }

        public OverlayForm(Xbox360ControllerWrapper pad, RawInput raw, RawInputMsgWindow msgWin)
        {
            _pad = pad ?? throw new ArgumentNullException(nameof(pad));
            _raw = raw ?? throw new ArgumentNullException(nameof(raw));
            _msgWin = msgWin ?? throw new ArgumentNullException(nameof(msgWin));

            _router = new InputRouter(_raw);
            _macros = new MacroEngine(_pad);
            _xpass = new XInputPassthrough(_pad); // pass pad
            _modeManager = new ModeManager(_xpass); // inject passthrough so manager controls lifecycle
            _hotkeys = new HotkeyManager(() => Handle); // init hotkey manager

            _uiCtx = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

            InitializeComponent();
            RebuildLayoutAdvanced();
            InitializeThemeManager();
            ConfigureWindow();
            SetupEventHandlers();

            // Subscribe to mode manager changes
            _modeManager.Changed += OnAppModeChanged;
            // Subscribe to hotkey manager events
            _hotkeys.OverlayToggle += ToggleOverlayVisibility;
            _hotkeys.ModeNext += () => _modeManager.Next();
            // removed: _hotkeys.SuppressToggle
            _hotkeys.LockToggle += () => { _lockPosition = !_lockPosition; SaveOverlayPrefsToProfile(); TrayTip("Overlay", _lockPosition ? "Position locked" : "Position unlocked", ToolTipIcon.Info); };
            _hotkeys.CompactToggle += () => { _compactMode = !_compactMode; SaveOverlayPrefsToProfile(); TrayTip("Overlay", _compactMode ? "Compact mode" : "Standard mode", ToolTipIcon.Info); };

            // Initialize mode from profile after load later
            // Wire input router for local visualization
            _router.OnMouseMove += (dx, dy) =>
            {
                _rawDx = dx; _rawDy = dy;
                var (sx, sy) = _mapper.MouseToRightStick(dx, dy);
                _stickRX = sx; _stickRY = sy;
            };
            // Allow middle mouse to toggle modes (legacy behavior)
            _router.OnMouseButton += (btn, down) =>
            {
                if (down && btn == MouseInput.Middle)
                    ToggleMode();
            };

            // Respond to PANIC (Ctrl+Alt+Pause) to disable suppression
            LowLevelHooks.PanicTriggered += () =>
            {
                _uiCtx.Post(_ =>
                {
                    UpdateSuppressUI(false);
                    TrayTip("Panic", "Suppression disabled", ToolTipIcon.Warning);
                }, null);
            };

            LoadProfileAndApplyToUI();
            StartBackgroundProcessing();
            // Moved: RegisterDeviceNotifications(); RegisterHotkeys(); to OnHandleCreated
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
            // Improve paint quality / reduce flicker
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            DoubleBuffered = true;

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

            // Make header draggable
            _headerPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && !_lockPosition)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            };

            _headerPanel.Controls.AddRange(new Control[] { titleLbl, _sidebarToggle });

            // Status strip at bottom
            _statusStrip = new StatusStrip
            {
                BackColor = _darkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(246, 246, 246),
                SizingGrip = false,
                Dock = DockStyle.Bottom
            };

            _slMode = new ToolStripStatusLabel("Mode: Output") { Spring = false };
            _slInput = new ToolStripStatusLabel("Input: 0,0") { Spring = false };
            _slOutput = new ToolStripStatusLabel("Output: 0,0") { Spring = false };
            _slFps = new ToolStripStatusLabel("FPS: -") { Spring = true, TextAlign = ContentAlignment.MiddleRight };

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
            // After adding the button, set its initial caption
            UpdateModeToggleButtonText();
            
            // XInput index (passthrough checkbox removed)
            _xinputIndex.Location = new Point(8, y);
            _xinputIndex.Size = new Size(60, 25);
            _xinputIndex.ValueChanged += (_, __) => { _xpass.SetPlayerIndex((int)_xinputIndex.Value - 1); SaveOverlayPrefsToProfile(); };
            _sidebarPanel.Controls.Add(_xinputIndex);
            var xIdxLbl = new Label { Text = "Player Id", Location = new Point(72, y + 4), Size = new Size(80, 18) };
            _sidebarPanel.Controls.Add(xIdxLbl);
            y += 30;

            // XInput status label
            _xinputStatus.Location = new Point(8, y);
            _xinputStatus.Text = "Pad: n/a";
            _sidebarPanel.Controls.Add(_xinputStatus);
            y += 22;

            // Controllers list label
            _controllersLbl = new Label { Text = "Controllers", Location = new Point(8, y), Size = new Size(160, 18) };
            _sidebarPanel.Controls.Add(_controllersLbl);
            y += 18;
            // Controllers list box
            _lstControllers = new ListBox
            {
                Location = new Point(8, y),
                Size = new Size(180, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                IntegralHeight = false
            };
            _sidebarPanel.Controls.Add(_lstControllers);
            y += 130;

            // Feature toggles
            _chkAlwaysOnTop = new CheckBox { Text = "Always On Top", Location = new Point(8, y), Size = new Size(160, 22), Checked = _alwaysOnTop };
            _chkAlwaysOnTop.CheckedChanged += (s, e2) => { _alwaysOnTop = _chkAlwaysOnTop.Checked; TopMost = _alwaysOnTop; SaveOverlayPrefsToProfile(); };
            _sidebarPanel.Controls.Add(_chkAlwaysOnTop); y += 22;

            _chkLockPos = new CheckBox { Text = "Lock Position", Location = new Point(8, y), Size = new Size(160, 22), Checked = _lockPosition };
            _chkLockPos.CheckedChanged += (s, e2) => { _lockPosition = _chkLockPos.Checked; SaveOverlayPrefsToProfile(); };
            _sidebarPanel.Controls.Add(_chkLockPos); y += 22;

            _chkCompact = new CheckBox { Text = "Compact Mode", Location = new Point(8, y), Size = new Size(160, 22), Checked = _compactMode };
            _chkCompact.CheckedChanged += (s, e2) => { _compactMode = _chkCompact.Checked; SaveOverlayPrefsToProfile(); };
            _sidebarPanel.Controls.Add(_chkCompact); y += 22;

            _chkEdgeSnap = new CheckBox { Text = "Edge Snap", Location = new Point(8, y), Size = new Size(160, 22), Checked = _edgeSnapping };
            _chkEdgeSnap.CheckedChanged += (s, e2) => { _edgeSnapping = _chkEdgeSnap.Checked; SaveOverlayPrefsToProfile(); };
            _sidebarPanel.Controls.Add(_chkEdgeSnap); y += 22;

            _numEdgeThreshold = new NumericUpDown { Location = new Point(8, y), Size = new Size(60, 22), Minimum = 2, Maximum = 64, Value = _edgeSnapThreshold };
            var edgeLbl = new Label { Text = "Snap px", Location = new Point(72, y+3), Size = new Size(70, 18) };
            _numEdgeThreshold.ValueChanged += (s, e2) => { _edgeSnapThreshold = (int)_numEdgeThreshold.Value; SaveOverlayPrefsToProfile(); };
            _sidebarPanel.Controls.Add(_numEdgeThreshold); _sidebarPanel.Controls.Add(edgeLbl); y += 28;

            _cmbTheme = new ComboBox { Location = new Point(8, y), Size = new Size(110, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTheme.Items.AddRange(new object[] { "Dark", "Light", "Purple" });
            _cmbTheme.SelectedIndex = _darkTheme ? 0 : 1; // crude mapping; Purple treated as Dark for now
            _cmbTheme.SelectedIndexChanged += (s, e2) => { var sel = _cmbTheme.SelectedItem?.ToString() ?? "Dark"; _darkTheme = sel != "Light"; _profiles.Current.OverlayTheme = sel; _themeManager?.ApplyTheme(_darkTheme); SaveOverlayPrefsToProfile(); };
            var themeLbl = new Label { Text = "Theme", Location = new Point(122, y+4), Size = new Size(50, 18) };
            _sidebarPanel.Controls.Add(_cmbTheme); _sidebarPanel.Controls.Add(themeLbl); y += 30;

            _btnAccent = new Button { Text = "Accent", Location = new Point(8, y), Size = new Size(80, 25), BackColor = _accentColor, ForeColor = Color.White };
            _btnAccent.Click += (s, e2) => { using var cd = new ColorDialog { Color = _accentColor, FullOpen = true }; if (cd.ShowDialog() == DialogResult.OK) { _accentColor = cd.Color; _btnAccent.BackColor = _accentColor; SaveOverlayPrefsToProfile(); } };
            _sidebarPanel.Controls.Add(_btnAccent); y += 35;

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

            // Dashboard tab (keep minimalist, consistent styling)
            _tabDashboard = new TabPage("Dashboard");
            var dashPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            _btnOpenLogs = new Button { Text = "Open Logs", Size = new Size(110, 28), Location = new Point(8, 8) };
            _btnOpenLogs.Click += (s, e) => { try { System.Diagnostics.Process.Start("explorer.exe", "Logs"); } catch { } };
            var dashInfo = new Label { Text = "Mode toggle: F8 or Middle Mouse", AutoSize = true, Location = new Point(8, 46) };
            dashPanel.Controls.AddRange(new Control[] { _btnOpenLogs, dashInfo });
            _tabDashboard.Controls.Add(dashPanel);

            // Calibration tab
            _tabCalib = new TabPage("Calibration");
            var calibPanel = new Panel { Dock = DockStyle.Fill };
            
            // Add visualizer and curve controls
            var vizPanel = new Panel { Dock = DockStyle.Fill };
            vizPanel.Controls.Add(_viz);

            var curvePanel = new Panel { Dock = DockStyle.Right, Width = 320, Padding = new Padding(8) };
            int cy = 8;
            Func<string, Control> mkLabel = (text) => new Label { Text = text, Location = new Point(8, cy + 3), Size = new Size(140, 20) };
            Func<decimal, decimal, decimal, decimal, NumericUpDown> mkNum = (min, max, val, inc) =>
            {
                var n = new NumericUpDown { Location = new Point(160, cy), Size = new Size(120, 22), Minimum = min, Maximum = max, DecimalPlaces = 3, Increment = inc, Value = val, ThousandsSeparator = false }; cy += 28; return n;
            };

            // Sensitivity
            curvePanel.Controls.Add(mkLabel("Sensitivity"));
            _numSensitivity = mkNum(0, 5, (decimal)_mapper.Curve.Sensitivity, 0.01m);
            _numSensitivity.DecimalPlaces = 2; _numSensitivity.Maximum = 5; _numSensitivity.Minimum = 0; _numSensitivity.Increment = 0.01m;
            _numSensitivity.ValueChanged += (s, e) => { _mapper.Curve.Sensitivity = (float)_numSensitivity.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numSensitivity);

            // Expo
            curvePanel.Controls.Add(mkLabel("Expo"));
            _numExpo = mkNum(0, 1, (decimal)_mapper.Curve.Expo, 0.01m);
            _numExpo.DecimalPlaces = 2; _numExpo.ValueChanged += (s, e) => { _mapper.Curve.Expo = (float)_numExpo.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numExpo);

            // AntiDeadzone
            curvePanel.Controls.Add(mkLabel("Anti-Deadzone"));
            _numAntiDead = mkNum(0, 0.5m, (decimal)_mapper.Curve.AntiDeadzone, 0.01m);
            _numAntiDead.DecimalPlaces = 3; _numAntiDead.ValueChanged += (s, e) => { _mapper.Curve.AntiDeadzone = (float)_numAntiDead.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numAntiDead);

            // EmaAlpha
            curvePanel.Controls.Add(mkLabel("Smoothing (EMA)"));
            _numEma = mkNum(0, 1, (decimal)_mapper.Curve.EmaAlpha, 0.01m);
            _numEma.DecimalPlaces = 2; _numEma.ValueChanged += (s, e) => { _mapper.Curve.EmaAlpha = (float)_numEma.Value; _mapper.Curve.ResetSmoothing(); SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numEma);

            // VelocityGain
            curvePanel.Controls.Add(mkLabel("Velocity Gain"));
            _numVelGain = mkNum(0, 3, (decimal)_mapper.Curve.VelocityGain, 0.01m);
            _numVelGain.DecimalPlaces = 2; _numVelGain.ValueChanged += (s, e) => { _mapper.Curve.VelocityGain = (float)_numVelGain.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numVelGain);

            // JitterFloor
            curvePanel.Controls.Add(mkLabel("Jitter Floor"));
            _numJitter = mkNum(0, 0.2m, (decimal)_mapper.Curve.JitterFloor, 0.001m);
            _numJitter.DecimalPlaces = 3; _numJitter.ValueChanged += (s, e) => { _mapper.Curve.JitterFloor = (float)_numJitter.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numJitter);

            // ScaleX
            curvePanel.Controls.Add(mkLabel("Scale X"));
            _numScaleX = mkNum(0.1m, 3m, (decimal)_mapper.Curve.ScaleX, 0.01m);
            _numScaleX.DecimalPlaces = 2; _numScaleX.ValueChanged += (s, e) => { _mapper.Curve.ScaleX = (float)_numScaleX.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numScaleX);

            // ScaleY
            curvePanel.Controls.Add(mkLabel("Scale Y"));
            _numScaleY = mkNum(0.1m, 3m, (decimal)_mapper.Curve.ScaleY, 0.01m);
            _numScaleY.DecimalPlaces = 2; _numScaleY.ValueChanged += (s, e) => { _mapper.Curve.ScaleY = (float)_numScaleY.Value; SaveOverlayPrefsToProfile(); };
            curvePanel.Controls.Add(_numScaleY);

            calibPanel.Controls.AddRange(new Control[] { vizPanel, curvePanel });
            _tabCalib.Controls.Add(calibPanel);

            // Macros tab
            _tabMacros = new TabPage("Macros");
            _tabMacros.Controls.Add(new Label { Text = "Macro editor coming soon", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

            // Notifications tab
            _tabNotifications = new TabPage("Notifications");
            _tabNotifications.Controls.Add(new Label { Text = "Notification center coming soon", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

            // Hotkeys tab
            _tabHotkeys = new TabPage("Hotkeys");
            _tabHotkeys.Controls.Add(new Label { Text = "Hotkey editor coming soon", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

            // Profiles tab (simple manager)
            _tabProfiles = new TabPage("Profiles");
            var profPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            _lstProfiles = new ListBox { Dock = DockStyle.Left, Width = 240 };
            _btnProfileLoad = new Button { Text = "Load", Size = new Size(110, 28), Location = new Point(260, 12) };
            _btnProfileNew = new Button { Text = "New", Size = new Size(110, 28), Location = new Point(260, 52) };
            _btnProfileClone = new Button { Text = "Clone", Size = new Size(110, 28), Location = new Point(260, 92) };
            _btnProfileDelete = new Button { Text = "Delete", Size = new Size(110, 28), Location = new Point(260, 132) };
            _btnProfileSave = new Button { Text = "Save", Size = new Size(110, 28), Location = new Point(260, 172) };
            _btnProfileLoad.Click += (s, e) => { if (_lstProfiles.SelectedItem is string p) { _profiles.Load(p); ApplyProfileToUI_Safe(_profiles.Current); RefreshProfilesList_Safe(p); } };
            _btnProfileNew.Click += (s, e) => { var path = _profiles.Create("profile"); RefreshProfilesList_Safe(path); };
            _btnProfileClone.Click += (s, e) => { var path = _profiles.CloneCurrent("clone"); RefreshProfilesList_Safe(path); };
            _btnProfileDelete.Click += (s, e) => { if (_lstProfiles.SelectedItem is string p) { _profiles.Delete(p); RefreshProfilesList_Safe(); } };
            _btnProfileSave.Click += (s, e) => { SaveOverlayPrefsToProfile(); _profiles.Save(); TrayTip("Profiles", "Saved", ToolTipIcon.Info); };
            profPanel.Controls.AddRange(new Control[] { _lstProfiles, _btnProfileLoad, _btnProfileNew, _btnProfileClone, _btnProfileDelete, _btnProfileSave });
            _tabProfiles.Controls.Add(profPanel);

            _mainTabs.TabPages.AddRange(new TabPage[] { _tabDashboard, _tabCalib, _tabProfiles, _tabMacros, _tabNotifications, _tabHotkeys });

            // Curve preview (bottom of content) - always visible
            _contentSplit.Panel1.Controls.Add(_mainTabs);
            _contentSplit.Panel2Collapsed = false;
            _contentSplit.Panel2.Controls.Add(_curvePrev);

            // Assemble
            Controls.AddRange(new Control[] { _contentSplit, _mainSplit, _statusStrip, _headerPanel });

            // place content split on right side of main split
            _mainSplit.Panel2.Controls.Add(_contentSplit);

            // Populate dynamic lists
            RefreshProfilesList_Safe(_profiles.CurrentPath);
            RefreshControllersListUI(true);

            ResumeLayout();
        }

        private void InitializeThemeManager()
        {
            _themeManager = new ThemeManager();
            
            // Register themable controls
            _themeManager.RegisterControl(this); // include form itself for background/foreground
            _themeManager.RegisterControl(_headerPanel);
            _themeManager.RegisterControl(_statusStrip);
            _themeManager.RegisterControl(_sidebarPanel);
            _themeManager.RegisterControl(_mainTabs);
            _themeManager.RegisterControl(_viz);
            _themeManager.RegisterControl(_curvePrev);

            // Apply current theme now that controls are registered
            _themeManager.ApplyTheme(_darkTheme);
        }

        private void ConfigureWindow()
        {
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            ShowInTaskbar = false;
            TopMost = _alwaysOnTop;
            // removed: click-through configuration
        }

        private void SetupEventHandlers()
        {
            Load += OverlayForm_Load;
            Shown += OverlayForm_Shown; // ensure visibility on first show
            FormClosing += OverlayForm_FormClosing;
            MouseDown += OverlayForm_MouseDown;
            Resize += OverlayForm_Resize;
            LocationChanged += OverlayForm_LocationChanged;

            _uiTimer.Tick += UiTimer_Tick;
            _savePosDebounce.Tick += SavePosDebounce_Tick;
            _deviceRefreshDebounce.Tick += DeviceRefreshDebounce_Tick;
            _deviceWatchTimer.Tick += DeviceWatchTimer_Tick;
            _snapDebounce.Tick += SnapDebounce_Tick;

            // Tray menu
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { Show(); BringToFront(); });
            trayMenu.Items.Add("Hide", null, (s, e) => Hide());
            // removed: click-through tray items
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Open Logs", null, (s, e) => { try { System.Diagnostics.Process.Start("explorer.exe", "Logs"); } catch { } });
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            _tray.ContextMenuStrip = trayMenu;
            _tray.DoubleClick += (s, e) => { Show(); BringToFront(); };
        }

        private void LoadProfileAndApplyToUI()
        {
            try
            {
                // ProfileManager constructor loads default; just apply
                ApplyProfileToUI_Safe(_profiles.Current);
                AutoSelectXInputIndexIfAvailable();
                if (_profiles.Current.StartHidden)
                    BeginInvoke(new Action(() => Hide()));
                // Ensure profiles list is populated at startup
                RefreshProfilesList_Safe(_profiles.CurrentPath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading profile", ex);
            }
        }

        private void StartBackgroundProcessing()
        {
            _uiTimer.Start();
            _deviceWatchTimer.Start();
            try { if (!_fpsSw.IsRunning) _fpsSw.Restart(); } catch { }
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
                Logger.Error($"Failed to register device notifications", ex);
            }
        }

        private void RegisterHotkeys()
        {
            // replaced by HotkeyManager.RegisterAll
            _hotkeys.RegisterAll(_profiles.Current);
        }

        private void UpdateStatus()
        {
            var modeText = AppMode switch
            {
                AppMode.KbmToPad => "MOUSE2JOY",
                AppMode.Passthrough => "KBM+Pass",
                _ => AppMode.ToString()
            };
            var suppressed = LowLevelHooks.Suppress;
            var modeWithFlags = suppressed ? $"Mode: {modeText} | Suppressed" : $"Mode: {modeText}";

            // Pad connection/passthrough status text
            string padInfo;
            var idxUI = (int)_xinputIndex.Value; // 1..4 for display
            var idx0 = idxUI - 1; // 0..3 internal
            bool phys = ProbeXInputConnected(idx0);
            if (_xpass.Enabled)
                padInfo = _xpass.IsConnected ? $"Pad P{idxUI}: Connected ({_xpass.PacketRateHz} Hz)" : $"Pad P{idxUI}: Not Connected";
            else
                padInfo = phys ? $"Pad P{idxUI}: Present" : $"Pad P{idxUI}: None";
            _xinputStatus.Text = padInfo;

            var inputText = $"Input: {_rawDx},{_rawDy}";
            var outputText = $"Output: {_stickRX},{_stickRY}";
            var fpsText = _fpsValue > 0 ? $"FPS: {_fpsValue}" : "FPS: -";

            if (_slMode != null) _slMode.Text = modeWithFlags;
            if (_slInput != null) _slInput.Text = inputText;
            if (_slOutput != null) _slOutput.Text = outputText;
            if (_slFps != null) _slFps.Text = fpsText;
        }

        private void UpdateInputVisualization()
        {
            // Update visualizer with the latest snapshot
            _viz.RX = _stickRX; _viz.RY = _stickRY;
            _viz.Invalidate();

            // Update curve preview
            _curvePrev.SetCurve(_mapper.Curve);
            _curvePrev.UpdateLive(_rawDx, _rawDy, _stickRX, _stickRY);
        }

        private void ToggleSidebar()
        {
            _mainSplit.Panel1Collapsed = !_mainSplit.Panel1Collapsed;
            _sidebarToggle.Text = _mainSplit.Panel1Collapsed ? "▶" : "◀";
        }

        // removed: ToggleCurveVisibility (curve preview always visible)

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // FPS tick
                _fpsFrames++;
                if (_fpsSw.IsRunning && _fpsSw.ElapsedMilliseconds >= 1000)
                {
                    _fpsValue = _fpsFrames;
                    _fpsFrames = 0;
                    _fpsSw.Restart();
                }

                switch (AppMode)
                {
                    case AppMode.KbmToPad:
                        // Send processed mouse movement to virtual gamepad
                        _pad.SetRightStick(_stickRX, _stickRY);
                        break;
                    case AppMode.Passthrough:
                        break;
                }
                UpdateStatus();
                UpdateInputVisualization();
            }
            catch (Exception ex)
            {
                Logger.Error("UI timer error", ex);
            }
        }

        private void SavePosDebounce_Tick(object? sender, EventArgs e)
        {
            try
            {
                _savePosDebounce.Stop();
                SaveWindowPositionToProfile();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save position", ex);
            }
        }

        private void DeviceRefreshDebounce_Tick(object? sender, EventArgs e)
        {
            _deviceRefreshDebounce.Stop();
            EnumerateControllers();
            AutoSelectXInputIndexIfAvailable();
        }

        private void DeviceWatchTimer_Tick(object? sender, EventArgs e)
        {
            EnumerateControllers();
            AutoSelectXInputIndexIfAvailable();
        }

        private void SnapDebounce_Tick(object? sender, EventArgs e)
        {
            _snapDebounce.Stop();
            if (_edgeSnapping && !_lockPosition)
            {
                SnapToEdges();
            }
        }

        private void EnumerateControllers()
        {
            try
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (Index, Name) in ControllerManager.EnumerateXInput()) set.Add(Name);
                foreach (var (Instance, Name) in ControllerManager.EnumerateDirectInput()) set.Add(Name);

                if (!_lastControllers.SetEquals(set))
                {
                    _lastControllers = set;
                    Logger.Info($"Controllers changed: {string.Join(", ", _lastControllers)}");
                    RefreshControllersListUI();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Controller enumeration failed", ex);
            }
        }

        private void AutoSelectXInputIndexIfAvailable()
        {
            try
            {
                // If current index isn't connected, try to pick the first connected one.
                int curUI = (int)(_xinputIndex?.Value ?? 1);
                int cur0 = curUI - 1;
                if (!ProbeXInputConnected(cur0))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (ProbeXInputConnected(i))
                        {
                            if (_xinputIndex != null)
                            {
                                // Map internal 0..3 -> UI 1..4
                                var uiVal = i + 1;
                                _xinputIndex.Value = Math.Max(_xinputIndex.Minimum, Math.Min(_xinputIndex.Maximum, uiVal));
                            }
                            _xpass.SetPlayerIndex(i);
                            Logger.Info($"Auto-selected XInput index P{i}");
                            break;
                        }
                    }
                }
                // Re-evaluate passthrough if mode/toggle allow, as index may have changed.
                // ReevaluatePassthrough removed; passthrough handled by UpdateStatus() and user toggle
            }
            catch (Exception ex)
            {
                Logger.Warn($"AutoSelectXInputIndexIfAvailable failed: {ex.Message}");
            }
        }

        private void UpdateSuppressUI(bool on)
        {
            try
            {
                // Centralize to UpdateStatus so the periodic UI timer doesn't overwrite it
                UpdateStatus();
            }
            catch { }
        }

        private void SaveWindowPositionToProfile()
        {
            try
            {
                if (_profiles.Current != null && WindowState == FormWindowState.Normal)
                {
                    _profiles.Current.OverlayLeft = Left;
                    _profiles.Current.OverlayTop = Top;
                    _profiles.Save();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to persist window position", ex);
            }
        }

        private void SaveOverlayPrefsToProfile()
        {
            try
            {
                var p = _profiles.Current;
                if (p == null) return;
                p.CurrentAppMode = AppMode;
                // removed: OverlayClickThrough persistence
                p.OverlayAlwaysOnTop = _alwaysOnTop;
                p.OverlayLockPosition = _lockPosition;
                p.OverlaySnapEdges = _edgeSnapping;
                p.OverlaySnapThreshold = _edgeSnapThreshold;
                p.OverlayCompact = _compactMode;
                p.OverlayTheme = _cmbTheme?.SelectedItem?.ToString() ?? (_darkTheme ? "Dark" : "Light");
                p.OverlayAccentArgb = _accentColor.ToArgb();
                p.OverlayRecentAccents = _recentAccentColors.Select(c => c.ToArgb()).ToList();
                // removed: CurveShowPanel persistence (always visible)
                if (WindowState == FormWindowState.Normal) { p.OverlayLeft = Left; p.OverlayTop = Top; }
                // Persist internal 0..3 index
                p.PreferredXInputIndex = (int)_xinputIndex.Value - 1;
                // removed: p.XInputPassthrough (no checkbox/UI)
                if (p.Curves != null)
                {
                    var c = p.Curves; var cp = _mapper.Curve;
                    c.Sensitivity = cp.Sensitivity; c.Expo = cp.Expo; c.AntiDeadzone = cp.AntiDeadzone; c.EmaAlpha = cp.EmaAlpha; c.VelocityGain = cp.VelocityGain; c.JitterFloor = cp.JitterFloor; c.ScaleX = cp.ScaleX; c.ScaleY = cp.ScaleY;
                }
                _profiles.Save();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save overlay prefs", ex);
            }
        }

        // === Window proc: hotkeys and device change ===
        private const int WM_HOTKEY = 0x0312;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    _deviceRefreshDebounce.Stop();
                    _deviceRefreshDebounce.Start();
                    break;
                case WM_HOTKEY:
                    _hotkeys.ProcessHotkey(m.WParam.ToInt32());
                    break;
            }
            base.WndProc(ref m);
        }

        private void OverlayForm_Load(object? sender, EventArgs e)
        {
            Logger.Info("OverlayForm loaded");
            try { _raw.Register(); } catch (Exception ex) { Logger.Error("RawInput register failed", ex); }
            UpdateSuppressUI(LowLevelHooks.Suppress);
            // Register device notifications and hotkeys now that handle exists
            RegisterDeviceNotifications();
            RegisterHotkeys();
            // Initial controllers list
            EnumerateControllers();
        }

        private void OverlayForm_Shown(object? sender, EventArgs e)
        {
            // Final guard once the form is visible
            // EnsureOnScreen removed; rely on Windows to position the form appropriately.
        }

        private void OverlayForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // Save current state
                SaveOverlayPrefsToProfile();
                _hotkeys?.Dispose();

                // Cleanup
                _macros?.Dispose();
                _uiTimer.Stop();
                _deviceWatchTimer.Stop();
                _savePosDebounce.Stop();
                _deviceRefreshDebounce.Stop();

                // Unregister notifications
                if (_devNotifyHid != IntPtr.Zero)
                    UnregisterDeviceNotification(_devNotifyHid);

                _tray.Visible = false;
                _tray.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during form closing", ex);
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
                if (_edgeSnapping && !_lockPosition)
                {
                    _snapDebounce.Stop();
                    _snapDebounce.Start();
                }
            }
        }

        private void SnapToEdges()
        {
            try
            {
                var screen = Screen.FromControl(this).WorkingArea;
                int threshold = _edgeSnapThreshold;
                int newX = Left;
                int newY = Top;
                bool changed = false;
                if (Math.Abs(Left - screen.Left) <= threshold) { newX = screen.Left; changed = true; }
                if (Math.Abs((Left + Width) - (screen.Right)) <= threshold) { newX = screen.Right - Width; changed = true; }
                if (Math.Abs(Top - screen.Top) <= threshold) { newY = screen.Top; changed = true; }
                if (Math.Abs((Top + Height) - (screen.Bottom)) <= threshold) { newY = screen.Bottom - Height; changed = true; }
                if (changed)
                {
                    Location = new Point(newX, newY);
                    SaveWindowPositionToProfile();
                }
            }
            catch { }
        }

        private void ToggleMode_Click(object? sender, EventArgs e)
        {
            _modeManager.Next(); // use new manager
        }

        private void ToggleMode()
        {
            _modeManager.Next(); // redirect legacy callers
        }

        private void ToggleOverlayVisibility()
        {
            if (Visible)
            {
                Hide();
                TrayTip("Overlay", "Overlay hidden", ToolTipIcon.Info);
            }
            else
            {
                Show();
                BringToFront();
                TrayTip("Overlay", "Overlay shown", ToolTipIcon.Info);
            }
        }

        private void TrayTip(string title, string text, ToolTipIcon icon)
        {
            // Throttle tray tips to avoid spam
            var now = DateTime.UtcNow;
            if ((now - _lastTrayTipUtc).TotalMilliseconds < 500) return;
            _lastTrayTipUtc = now;

            try
            {
                _tray.ShowBalloonTip(2000, title, text, icon);
            }
            catch (Exception ex)
            {
                // Silently ignore tray tip errors
                System.Diagnostics.Debug.WriteLine($"TrayTip error: {ex.Message}");
            }
        }

        private void OnAppModeChanged(AppMode newMode)
        {
            // Persist current mode to profile
            var p = _profiles.Current; if (p != null) { p.CurrentAppMode = newMode; _profiles.Save(); }

            // Manage suppression by mode only (no UI toggle)
            bool shouldSuppress = newMode == AppMode.KbmToPad; // suppress OS input only when mapping KBM -> pad
            LowLevelHooks.Suppress = shouldSuppress;
            UpdateSuppressUI(shouldSuppress);

            // If entering Passthrough, switch to the next connected XInput pad
            if (newMode == AppMode.Passthrough)
            {
                try
                {
                    int curUI = (int)(_xinputIndex?.Value ?? 1);
                    int cur0 = Math.Clamp(curUI - 1, 0, 3);
                    int next0 = GetNextConnectedXInputIndex(cur0);
                    if (next0 != cur0)
                    {
                        int nextUI = next0 + 1;
                        if (_xinputIndex != null)
                        {
                            _xinputIndex.Value = Math.Max((int)_xinputIndex.Minimum, Math.Min((int)_xinputIndex.Maximum, nextUI));
                        }
                        _xpass.SetPlayerIndex(next0);
                        SaveOverlayPrefsToProfile();
                        Logger.Info($"Switched passthrough to next active pad: P{nextUI}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to switch to next active pad: {ex.Message}");
                }
            }

            UpdateStatus();
            UpdateModeToggleButtonText();
        }

        // Returns the next connected XInput index after current (wrap-around), or current if none other is connected.
        private static int GetNextConnectedXInputIndex(int current)
        {
            int cur = Math.Clamp(current, 0, 3);
            for (int step = 1; step <= 4; step++)
            {
                int i = (cur + step) & 3; // wrap 0..3
                if (i == cur) break; // completed full loop
                if (ProbeXInputConnected(i)) return i;
            }
            return cur;
        }

        private void UpdateModeToggleButtonText()
        {
            string current = AppMode switch
            {
                AppMode.KbmToPad => "MOUSE2JOY",
                AppMode.Passthrough => "KBM+Pass",
                _ => AppMode.ToString()
            };
            string next = (_modeManager.Current == AppMode.KbmToPad ? AppMode.Passthrough : AppMode.KbmToPad) switch
            {
                AppMode.KbmToPad => "MOUSE2JOY",
                AppMode.Passthrough => "KBM+Pass",
                _ => "?"
            };
            if (_toggleMode != null)
            {
                _toggleMode.Text = $"Mode: {current} → {next}";
            }
        }

        // Safely refresh the profiles list box; falls back to showing current/selected path if enumeration isn't available.
        private void RefreshProfilesList_Safe(string? selectPath = null)
        {
            try
            {
                if (InvokeRequired) { BeginInvoke(new Action(() => RefreshProfilesList_Safe(selectPath))); return; }
                if (_lstProfiles == null) return;

                _lstProfiles.BeginUpdate();
                try
                {
                    _lstProfiles.Items.Clear();

                    IEnumerable<string> paths = Enumerable.Empty<string>();

                    // Try to use a common enumeration method on ProfileManager via reflection (keeps compile-time decoupling).
                    try
                    {
                        var mi = _profiles.GetType().GetMethod("EnumeratePaths", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (mi != null)
                        {
                            var result = mi.Invoke(_profiles, null) as IEnumerable<string>;
                            if (result != null) paths = result;
                        }
                    }
                    catch { }

                    if (!paths.Any())
                    {
                        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrWhiteSpace(_profiles.CurrentPath)) set.Add(_profiles.CurrentPath);
                        if (!string.IsNullOrWhiteSpace(selectPath)) set.Add(selectPath);
                        paths = set;
                    }

                    foreach (var p in paths)
                        _lstProfiles.Items.Add(p);

                    var toSelect = !string.IsNullOrWhiteSpace(selectPath) ? selectPath : _profiles.CurrentPath;
                    if (!string.IsNullOrWhiteSpace(toSelect))
                    {
                        int idx = _lstProfiles.Items.IndexOf(toSelect);
                        if (idx >= 0) _lstProfiles.SelectedIndex = idx;
                    }
                }
                finally
                {
                    _lstProfiles.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"RefreshProfilesList_Safe failed: {ex.Message}");
            }
        }

        // Update the controllers status list in the sidebar
        private void RefreshControllersListUI(bool force = false)
        {
            try
            {
                if (_lstControllers == null) return;

                // Optionally re-enumerate when forced
                if (force)
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (_, Name) in ControllerManager.EnumerateXInput()) set.Add(Name);
                    foreach (var (_, Name) in ControllerManager.EnumerateDirectInput()) set.Add(Name);
                    _lastControllers = set;
                }

                var items = _lastControllers.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();

                // Only update UI if changed to avoid flicker
                bool same = _lstControllers.Items.Count == items.Length;
                if (same)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (!string.Equals(_lstControllers.Items[i]?.ToString(), items[i], StringComparison.Ordinal))
                        {
                            same = false; break;
                        }
                    }
                }
                if (same)
                {
                    _controllersLbl.Text = $"Controllers ({items.Length})";
                    return;
                }

                _lstControllers.BeginUpdate();
                try
                {
                    _lstControllers.Items.Clear();
                    _lstControllers.Items.AddRange(items);
                }
                finally
                {
                    _lstControllers.EndUpdate();
                }
                _controllersLbl.Text = $"Controllers ({items.Length})";
            }
            catch (Exception ex)
            {
                Logger.Warn($"RefreshControllersListUI failed: {ex.Message}");
            }
        }

        // Safely apply current profile settings to the UI; ignores the parameter and uses _profiles.Current.
        private void ApplyProfileToUI_Safe(object? _)
        {
            try
            {
                if (InvokeRequired) { BeginInvoke(new Action(() => ApplyProfileToUI_Safe(_))); return; }

                var p = _profiles.Current;
                if (p == null) return;

                _alwaysOnTop = p.OverlayAlwaysOnTop;
                _lockPosition = p.OverlayLockPosition;
                _edgeSnapping = p.OverlaySnapEdges;
                _edgeSnapThreshold = p.OverlaySnapThreshold;
                _compactMode = p.OverlayCompact;

                var themeName = string.IsNullOrWhiteSpace(p.OverlayTheme) ? "Dark" : p.OverlayTheme;
                _darkTheme = !string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase);

                try { _accentColor = Color.FromArgb(p.OverlayAccentArgb); } catch { }
                try { _recentAccentColors = (p.OverlayRecentAccents ?? new List<int>()).Select(Color.FromArgb).ToList(); } catch { }

                TopMost = _alwaysOnTop;
                if (_chkAlwaysOnTop != null) _chkAlwaysOnTop.Checked = _alwaysOnTop;
                if (_chkLockPos != null) _chkLockPos.Checked = _lockPosition;
                if (_chkEdgeSnap != null) _chkEdgeSnap.Checked = _edgeSnapping;
                if (_numEdgeThreshold != null)
                {
                    var val = Math.Max((int)_numEdgeThreshold.Minimum, Math.Min((int)_numEdgeThreshold.Maximum, _edgeSnapThreshold));
                    _numEdgeThreshold.Value = val;
                }
                if (_chkCompact != null) _chkCompact.Checked = _compactMode;

                if (_cmbTheme != null)
                {
                    int idx = -1;
                    for (int i = 0; i < _cmbTheme.Items.Count; i++)
                        if (string.Equals(_cmbTheme.Items[i]?.ToString(), themeName, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
                    _cmbTheme.SelectedIndex = idx >= 0 ? idx : (_darkTheme ? 0 : 1);
                }
                if (_btnAccent != null) _btnAccent.BackColor = _accentColor;

                // Apply XInput preferences
                if (_xinputIndex != null)
                {
                    // Clamp internal 0..3
                    var idx0 = Math.Max(0, Math.Min(3, p.PreferredXInputIndex));
                    // Map to UI 1..4 and clamp to control bounds just in case
                    var idxUI = Math.Max((int)_xinputIndex.Minimum, Math.Min((int)_xinputIndex.Maximum, idx0 + 1));
                    _xinputIndex.Value = idxUI;
                    _xpass.SetPlayerIndex(idx0);
                }
                // removed: passthrough checkbox usage

                // Load curve settings into mapper and controls
                var cs = p.Curves ?? new ProfileSchemaV1.CurveSettings();
                _mapper.Curve.Sensitivity = cs.Sensitivity;
                _mapper.Curve.Expo = cs.Expo;
                _mapper.Curve.AntiDeadzone = cs.AntiDeadzone;
                _mapper.Curve.EmaAlpha = cs.EmaAlpha;
                _mapper.Curve.VelocityGain = cs.VelocityGain;
                _mapper.Curve.JitterFloor = cs.JitterFloor;
                _mapper.Curve.ScaleX = cs.ScaleX;
                _mapper.Curve.ScaleY = cs.ScaleY;
                try
                {
                    if (_numSensitivity != null) _numSensitivity.Value = (decimal)cs.Sensitivity;
                    if (_numExpo != null) _numExpo.Value = (decimal)cs.Expo;
                    if (_numAntiDead != null) _numAntiDead.Value = (decimal)cs.AntiDeadzone;
                    if (_numEma != null) _numEma.Value = (decimal)cs.EmaAlpha;
                    if (_numVelGain != null) _numVelGain.Value = (decimal)cs.VelocityGain;
                    if (_numJitter != null) _numJitter.Value = (decimal)cs.JitterFloor;
                    if (_numScaleX != null) _numScaleX.Value = (decimal)cs.ScaleX;
                    if (_numScaleY != null) _numScaleY.Value = (decimal)cs.ScaleY;
                }
                catch { }

                // Optionally restore last window position if reasonable
                if (WindowState == FormWindowState.Normal)
                {
                    try
                    {
                        var screen = Screen.FromControl(this).WorkingArea;
                        var x = p.OverlayLeft;
                        var y = p.OverlayTop;
                        if (x >= screen.Left - 100 && x <= screen.Right && y >= screen.Top - 100 && y <= screen.Bottom)
                            Location = new Point(x, y);
                    }
                    catch { }
                }

                // Apply app mode from profile and ensure suppression/passthrough alignment
                var mode = p.CurrentAppMode ?? AppMode.KbmToPad;
                if (_modeManager.Current != mode)
                {
                    _modeManager.Set(mode);
                }
                else
                {
                    // Even if unchanged, make sure suppression reflects the mode on load
                    bool shouldSuppress = mode == AppMode.KbmToPad;
                    LowLevelHooks.Suppress = shouldSuppress;
                    UpdateSuppressUI(shouldSuppress);
                }

                _themeManager?.ApplyTheme(_darkTheme);
                UpdateModeToggleButtonText();
                UpdateStatus();
                RefreshControllersListUI(true);
            }
            catch (Exception ex)
            {
                Logger.Warn($"ApplyProfileToUI_Safe failed: {ex.Message}");
            }
        }
    }
}