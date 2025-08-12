using System;
using System.Collections.Generic;
using System.Linq;
using GaymController.Shared.Mapping;
using HidSharp;

namespace GaymController.Wooting {
    // Raw HID provider using HidSharp for device enumeration.
    public sealed class RawHidProvider : IWootingProvider {
        public event EventHandler<InputEvent>? OnKeyAnalog;

        public const int VendorId = 0x31E3;
        static readonly HashSet<int> SupportedProductIds = new() { 0x1100, 0x1200, 0x1210, 0x1220 };

        HidStream? _stream;

        /// <summary>Checks whether the VID/PID pair represents a supported Wooting keyboard.</summary>
        public static bool IsSupportedDevice(int vendorId, int productId) =>
            vendorId == VendorId && SupportedProductIds.Contains(productId);

        /// <summary>Enumerate raw HID device paths for supported Wooting keyboards.</summary>
        public static IEnumerable<string> EnumerateDevicePaths() {
            foreach (var device in DeviceList.Local.GetHidDevices()) {
                if (IsSupportedDevice(device.VendorID, device.ProductID)) {
                    yield return device.DevicePath;
                }
            }
        }

        public void Start() {
            if (_stream != null) { return; }
            var device = DeviceList.Local.GetHidDevices()
                .FirstOrDefault(d => IsSupportedDevice(d.VendorID, d.ProductID));
            if (device == null) { return; }
            _stream = device.Open();
        }

        public void Stop() {
            _stream?.Dispose();
            _stream = null;
        }

        public void Dispose() { Stop(); }
    }
}
