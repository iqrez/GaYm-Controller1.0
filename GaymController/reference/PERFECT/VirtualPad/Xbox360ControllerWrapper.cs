using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using WootMouseRemap.Controllers; // added

namespace WootMouseRemap
{
    public sealed class Xbox360ControllerWrapper : IDisposable
    {
        // Removed nested PadSnapshot class; use Controllers.PadSnapshot struct instead
        private PadSnapshot _snap; // default struct

        private ViGEmClient? _client;
        private IXbox360Controller? _target;
        private global::System.Threading.Timer? _watchdog;

        public bool IsConnected => _target is not null;
        public event Action<bool>? StatusChanged;

        public void Connect()
        {
            try
            {
                _client = new ViGEmClient();
                _target = _client.CreateXbox360Controller();
                _target.Connect();
                StatusChanged?.Invoke(true);
                _watchdog = new global::System.Threading.Timer(_ => Submit(), null, 0, 5);
            }
            catch (Exception ex)
            {
                Logger.Error("ViGEm connect failed", ex);
                StatusChanged?.Invoke(false);
            }
        }

        private int _retries;
        private void Reconnect()
        {
            if (_retries > 6) return;
            _retries++;
            Logger.Warn($"ViGEm reconnect attempt #{_retries}");
            DisposePadOnly();
            try
            {
                System.Threading.Thread.Sleep(200 * _retries);
                Connect();
            }
            catch { /* ignore */ }
            if (IsConnected) _retries = 0;
        }

        public void SetButton(Xbox360Button button, bool pressed)
        {
            try
            {
                _target?.SetButtonState(button, pressed);
                if (button == Xbox360Button.A) _snap.A = pressed;
                else if (button == Xbox360Button.B) _snap.B = pressed;
                else if (button == Xbox360Button.X) _snap.X = pressed;
                else if (button == Xbox360Button.Y) _snap.Y = pressed;
                else if (button == Xbox360Button.LeftShoulder) _snap.LB = pressed;
                else if (button == Xbox360Button.RightShoulder) _snap.RB = pressed;
                else if (button == Xbox360Button.Back) _snap.Back = pressed;
                else if (button == Xbox360Button.Start) _snap.Start = pressed;
                else if (button == Xbox360Button.LeftThumb) _snap.L3 = pressed;
                else if (button == Xbox360Button.RightThumb) _snap.R3 = pressed;
                else if (button == Xbox360Button.Up) _snap.DUp = pressed;
                else if (button == Xbox360Button.Down) _snap.DDown = pressed;
                else if (button == Xbox360Button.Left) _snap.DLeft = pressed;
                else if (button == Xbox360Button.Right) _snap.DRight = pressed;
            }
            catch
            {
                Reconnect();
            }
        }

        public void SetTrigger(bool right, byte value)
        {
            try
            {
                if (_target == null) return;
                var slider = right ? Xbox360Slider.RightTrigger : Xbox360Slider.LeftTrigger;
                _target.SetSliderValue(slider, value);
                if (slider == Xbox360Slider.LeftTrigger) _snap.LT = value; else _snap.RT = value;
            }
            catch
            {
                Reconnect();
            }
        }

        public void SetRightStick(short x, short y)
        {
            try
            {
                _target?.SetAxisValue(Xbox360Axis.RightThumbX, x);
                _target?.SetAxisValue(Xbox360Axis.RightThumbY, y);
                _snap.RX = x; _snap.RY = y;
            }
            catch
            {
                Reconnect();
            }
        }

        public void SetLeftStick(short x, short y)
        {
            try
            {
                _target?.SetAxisValue(Xbox360Axis.LeftThumbX, x);
                _target?.SetAxisValue(Xbox360Axis.LeftThumbY, y);
                _snap.LX = x; _snap.LY = y;
            }
            catch
            {
                Reconnect();
            }
        }

        public void SetDpad(bool up, bool down, bool left, bool right)
        {
            try
            {
                if (_target == null) return;
                _target.SetButtonState(Xbox360Button.Up, up); _snap.DUp = up;
                _target.SetButtonState(Xbox360Button.Down, down); _snap.DDown = down;
                _target.SetButtonState(Xbox360Button.Left, left); _snap.DLeft = left;
                _target.SetButtonState(Xbox360Button.Right, right); _snap.DRight = right;
            }
            catch
            {
                Reconnect();
            }
        }

        public void ResetAll()
        {
            try
            {
                _target?.ResetReport();
                _target?.SubmitReport();
                _snap.Clear();
            }
            catch
            {
                Reconnect();
            }
        }

        public PadSnapshot GetSnapshot() => _snap;

        public void Submit()
        {
            try
            {
                _target?.SubmitReport();
            }
            catch
            {
                Reconnect();
            }
        }

        private void DisposePadOnly()
        {
            try { _target?.Disconnect(); } catch { }
            try { _client?.Dispose(); } catch { }
            _target = null; _client = null;
            StatusChanged?.Invoke(false);
        }

        public void Dispose()
        {
            try { _watchdog?.Dispose(); } catch { }
            DisposePadOnly();
        }
    }
}
