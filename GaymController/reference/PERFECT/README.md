# WootMouseRemap

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

An advanced mouse-to-controller input remapping tool for Windows that provides precise control over mouse input conversion to Xbox 360 controller output.

## 🚀 Features

### Core Functionality
- **Real-time Input Conversion**: Convert mouse movement to right analog stick input with customizable sensitivity
- **Multiple Input Modes**: Switch between mouse/keyboard passthrough and controller output modes
- **Advanced Curve Processing**: Sophisticated input curves with smoothing and dead zone handling
- **Profile Management**: Save and load different configuration profiles for various games/applications

### Advanced Features
- **Diagonal Smoothing**: Eliminates XY stutter at diagonal movements using radial processing
- **No-Motion Watchdog**: Automatically zeros right stick when mouse stops moving to prevent drift
- **Auto-Tuning**: Automatic calibration system with figure-8 pattern recognition
- **OS Input Suppression**: Prevents original mouse input from interfering with games
- **Hotkey Support**: Comprehensive hotkey system for mode switching and control

### User Interface
- **Modern UI**: Clean, responsive interface with dark/light theme support
- **Real-time Visualization**: Live input/output monitoring and curve preview
- **Compact Mode**: Minimized interface for unobtrusive operation
- **System Tray Integration**: Run minimized with tray notifications
- **Edge Snapping**: Automatic window positioning at screen edges

## 📋 Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 8.0 Runtime (or self-contained build)
- **Dependencies**: ViGEm Bus Driver (for virtual controller support)

## 🛠️ Installation

### Option 1: Pre-built Release
1. Download the latest release from the [Releases](../../releases) page
2. Extract the archive to your desired location
3. Install ViGEm Bus Driver if not already installed
4. Run `WootMouseRemap.exe`

### Option 2: Build from Source
```powershell
# Clone the repository
git clone https://github.com/your-repo/WootMouseRemap.git
cd WootMouseRemap

# Build and run
.\build_publish.ps1 -Publish
```

## 🎮 Usage

### Quick Start
1. Launch WootMouseRemap
2. The application starts in **Output Mode** (mouse → controller)
3. Use **F8** or **Middle Mouse** to toggle between modes
4. Adjust sensitivity and curves in the UI as needed

### Hotkeys
| Key Combination | Action |
|----------------|--------|
| `F8` | Toggle input mode |
| `Middle Mouse` | Toggle input mode |
| `Ctrl+Alt+H` | Toggle overlay visibility |
| `Ctrl+Alt+L` | Lock/unlock overlay position |
| `Ctrl+Alt+C` | Toggle compact mode |
| `Ctrl+Alt+Pause` | Emergency suppression disable (PANIC) |

### Modes
- **Output Mode**: Mouse input → Controller output (for gaming)
- **Passthrough Mode**: Normal mouse/keyboard operation

## ⚙️ Configuration

### Profiles
- Profiles are stored in the `Profiles/` directory as JSON files
- The `default.json` profile is loaded automatically on startup
- Create custom profiles for different games or use cases
- Profile history is maintained in `Profiles/_history/`

### Settings
- Application settings are stored in `Config/app.json`
- Logs are written to the `Logs/` directory
- Automatic log rotation prevents disk space issues

### Advanced Configuration
```json
{
  "logging": {
    "minLevel": "Info",
    "enableDebugLogging": false,
    "maxLogFileSizeMB": 5
  },
  "ui": {
    "alwaysOnTop": true,
    "theme": "Dark",
    "edgeSnapping": true
  },
  "input": {
    "mouseSensitivity": 100,
    "enableSmoothing": true,
    "noMotionTimeoutMs": 18
  }
}
```

## 🔧 Development

### Building
```powershell
# Standard build
.\build_publish.ps1

# Clean build with tests
.\build_publish.ps1 -Clean -Test

# Publish single-file executable
.\build_publish.ps1 -Publish -SelfContained

# Verbose output
.\build_publish.ps1 -Verbose
```

### Project Structure
```
WootMouseRemap/
├── Configuration/          # Configuration management
├── Controllers/           # Controller input handling
├── Diagnostics/          # Logging and performance monitoring
├── Gamepad/              # Virtual gamepad management
├── Input/                # Raw input processing
├── Mapping/              # Input routing and mapping
├── Processing/           # Curve processing and smoothing
├── Profiles/             # Profile management
├── UI/                   # User interface components
├── Utilities/            # Helper utilities
└── VirtualPad/           # ViGEm integration
```

### Key Components
- **Logger**: Enhanced logging with context and rotation
- **PerformanceMonitor**: Real-time performance metrics
- **AppConfig**: Centralized configuration management
- **ExceptionHandler**: Robust error handling utilities
- **ValidationHelper**: Input validation and sanitization

## 🐛 Troubleshooting

### Common Issues

**Application won't start**
- Ensure .NET 8.0 Runtime is installed
- Check Windows Event Log for startup errors
- Verify ViGEm Bus Driver is installed

**Controller not detected in games**
- Install/reinstall ViGEm Bus Driver
- Check if other controller software is conflicting
- Verify the virtual controller is connected (check Device Manager)

**Input lag or stuttering**
- Adjust performance settings in configuration
- Disable unnecessary background applications
- Check system resources (CPU/Memory usage)

**Hotkeys not working**
- Run as Administrator if needed
- Check for conflicts with other applications
- Verify hotkey registration in logs

### Logs and Diagnostics
- Main log: `Logs/woot.log`
- Fatal errors: `Logs/fatal.txt`
- Performance data: Enable in configuration
- Debug logging: Set `enableDebugLogging: true` in config

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow existing code style and patterns
- Add appropriate logging and error handling
- Include unit tests for new functionality
- Update documentation as needed

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [ViGEm](https://github.com/ViGEm/ViGEmBus) - Virtual Gamepad Emulation Framework
- [Nefarius.ViGEm.Client](https://github.com/ViGEm/Nefarius.ViGEm.Client) - .NET Client Library

## 📞 Support

- **Issues**: [GitHub Issues](../../issues)
- **Discussions**: [GitHub Discussions](../../discussions)
- **Documentation**: [Wiki](../../wiki)

---

**Note**: This application requires administrative privileges for low-level input hooking and virtual device creation. Always download from trusted sources and verify file integrity.