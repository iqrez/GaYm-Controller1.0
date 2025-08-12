using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace WootMouseRemap
{
    public sealed class DeviceCloak
    {
        public sealed class DeviceInfo
        {
            public string InstanceId { get; init; } = string.Empty;
            public string FriendlyName { get; init; } = string.Empty;
            public string Class { get; init; } = string.Empty;
            public override string ToString() => $"{FriendlyName} [{InstanceId}]";
        }

        public sealed class ConfigModel
        {
            public bool HideWhenActive { get; set; } = false;
            public bool RestoreOnExit { get; set; } = true;
            public List<string> SelectedInstanceIds { get; set; } = new List<string>();
        }

        public ConfigModel Config { get; private set; } = new ConfigModel();

        public bool IsAdmin
        {
            get
            {
                try
                {
                    using var id = WindowsIdentity.GetCurrent();
                    var prin = new WindowsPrincipal(id);
                    return prin.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch { return false; }
            }
        }

        private readonly HashSet<string> _hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public string ConfigPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PERFECT");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "controller_cloak.json");
            }
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    Config = System.Text.Json.JsonSerializer.Deserialize<ConfigModel>(File.ReadAllText(ConfigPath)) ?? new ConfigModel();
                }
            }
            catch
            {
                Config = new ConfigModel();
            }
        }

        public void SaveConfig()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(Config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        // === Public API ===

        public IReadOnlyList<DeviceInfo> ListControllers()
        {
            var list = new List<DeviceInfo>();
            foreach (var dev in SetupApiEnumerateAll())
            {
                // Filter typical controller devices: HIDClass with "IG_" interface (gamepads) or "XINPUT" in hardware ID
                if (!dev.Class.Equals("HIDClass", StringComparison.OrdinalIgnoreCase))
                    continue;

                string iid = dev.InstanceId;
                string up = (dev.FriendlyName ?? "").ToUpperInvariant() + " " + dev.InstanceId.ToUpperInvariant();
                if (up.Contains("IG_") || up.Contains("XINPUT") || up.Contains("GAMEPAD") || up.Contains("CONTROLLER"))
                {
                    list.Add(dev);
                }
            }
            // Distinct by InstanceId
            return list
                .GroupBy(d => d.InstanceId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(d => d.FriendlyName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public void EnsureHidden()
        {
            foreach (var id in Config.SelectedInstanceIds.ToArray())
            {
                if (DisableDevice(id))
                    _hidden.Add(id);
            }
        }

        public void RestoreHidden()
        {
            foreach (var id in _hidden.ToArray())
            {
                EnableDevice(id);
                _hidden.Remove(id);
            }
        }

        public bool DisableDevice(string instanceId) => ChangeState(instanceId, DICS_DISABLE);
        public bool EnableDevice(string instanceId) => ChangeState(instanceId, DICS_ENABLE);

        // === SetupAPI implementation ===

        private const int DIGCF_PRESENT = 0x00000002;
        private const int DIGCF_ALLCLASSES = 0x00000004;

        private const int SPDRP_CLASS = 0x00000007;
        private const int SPDRP_DEVICEDESC = 0x00000000;
        private const int SPDRP_FRIENDLYNAME = 0x0000000C;
        private const int SPDRP_HARDWAREID = 0x00000001;

        private const int DICS_ENABLE = 0x00000001;
        private const int DICS_DISABLE = 0x00000002;
        private const int DICS_FLAG_GLOBAL = 0x00000001;
        private const int DIF_PROPERTYCHANGE = 0x00000012;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_PROPCHANGE_PARAMS
        {
            public uint ClassInstallHeader_cbSize;
            public uint ClassInstallHeader_InstallFunction;
            public uint StateChange;
            public uint Scope;
            public uint HwProfile;
        }

        private sealed class HDevInfoHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public HDevInfoHandle() : base(true) { }
            protected override bool ReleaseHandle() => SetupDiDestroyDeviceInfoList(handle);
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern HDevInfoHandle SetupDiGetClassDevsW(
            IntPtr ClassGuid,
            string? Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            HDevInfoHandle DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceInstanceIdW(
            HDevInfoHandle DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            System.Text.StringBuilder DeviceInstanceId,
            int DeviceInstanceIdSize,
            out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceRegistryPropertyW(
            HDevInfoHandle DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiSetClassInstallParamsW(
            HDevInfoHandle DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams,
            int ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiCallClassInstaller(
            uint InstallFunction,
            HDevInfoHandle DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        private IEnumerable<DeviceInfo> SetupApiEnumerateAll()
        {
            var devs = new List<DeviceInfo>();
            using var info = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (info == null || info.IsInvalid) yield break;

            uint index = 0;
            while (true)
            {
                var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                if (!SetupDiEnumDeviceInfo(info, index, ref data))
                    yield break;
                index++;

                string? cls = ReadRegStr(info, ref data, SPDRP_CLASS);
                string friendly = FirstNonEmpty(
                    ReadRegStr(info, ref data, SPDRP_FRIENDLYNAME),
                    ReadRegStr(info, ref data, SPDRP_DEVICEDESC));
                string? iid = ReadInstanceId(info, ref data);
                if (!string.IsNullOrWhiteSpace(iid))
                {
                    yield return new DeviceInfo
                    {
                        InstanceId = iid!,
                        FriendlyName = friendly ?? "(Unknown device)",
                        Class = cls ?? ""
                    };
                }
            }
        }

        private static string? ReadInstanceId(HDevInfoHandle set, ref SP_DEVINFO_DATA data)
        {
            int req;
            var sb = new System.Text.StringBuilder(1024);
            if (SetupDiGetDeviceInstanceIdW(set, ref data, sb, sb.Capacity, out req))
                return sb.ToString();
            return null;
        }

        private static string? ReadRegStr(HDevInfoHandle set, ref SP_DEVINFO_DATA data, uint prop)
        {
            uint type;
            uint needed;
            byte[] buf = new byte[1024];
            if (SetupDiGetDeviceRegistryPropertyW(set, ref data, prop, out type, buf, (uint)buf.Length, out needed))
            {
                int len = Array.IndexOf<byte>(buf, 0);
                if (len < 0) len = (int)needed;
                return System.Text.Encoding.Unicode.GetString(buf, 0, len).TrimEnd('\0');
            }
            return null;
        }

        private static string FirstNonEmpty(params string?[] arr)
        {
            foreach (var s in arr) if (!string.IsNullOrWhiteSpace(s)) return s!;
            return "(Unnamed device)";
        }

        private bool ChangeState(string instanceId, int state)
        {
            if (!IsAdmin) throw new InvalidOperationException("Hiding requires Administrator privileges.");
            using var info = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (info == null || info.IsInvalid) return false;

            uint index = 0;
            while (true)
            {
                var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                if (!SetupDiEnumDeviceInfo(info, index, ref data))
                    break;
                index++;

                string? iid = ReadInstanceId(info, ref data);
                if (!string.Equals(iid, instanceId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var p = new SP_PROPCHANGE_PARAMS
                {
                    ClassInstallHeader_cbSize = (uint)Marshal.SizeOf<SP_PROPCHANGE_PARAMS>(),
                    ClassInstallHeader_InstallFunction = DIF_PROPERTYCHANGE,
                    StateChange = (uint)state,
                    Scope = DICS_FLAG_GLOBAL,
                    HwProfile = 0
                };
                if (!SetupDiSetClassInstallParamsW(info, ref data, ref p, Marshal.SizeOf<SP_PROPCHANGE_PARAMS>()))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "SetupDiSetClassInstallParams failed.");
                if (!SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, info, ref data))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "SetupDiCallClassInstaller failed.");
                return true;
            }
            return false;
        }
    }
}
