using System;

namespace WootMouseRemap
{
    public sealed class InputRouter : IDisposable
    {
        private readonly RawInput _raw;
        private readonly bool _useHooksMouseFallback;

        public event Action<int, bool>? OnKey;
        public event Action<MouseInput, bool>? OnMouseButton;
        public event Action<int, int>? OnMouseMove;
        public event Action<int>? OnWheel;

        public InputRouter(RawInput raw, bool useHooksMouseFallback = true)
        {
            _raw = raw;
            _useHooksMouseFallback = useHooksMouseFallback;

            _raw.KeyboardEvent += (vk, down) => OnKey?.Invoke(vk, down);
            _raw.MouseButton += (b, d) => OnMouseButton?.Invoke(b, d);
            _raw.MouseMove += (dx, dy) => OnMouseMove?.Invoke(dx, dy);
            _raw.MouseWheel += delta => OnWheel?.Invoke(delta);

            if (_useHooksMouseFallback)
            {
                LowLevelHooks.KeyEvent += (vk, down) => OnKey?.Invoke(vk, down);
                LowLevelHooks.MouseButton += (b, d) => OnMouseButton?.Invoke(b, d);
                LowLevelHooks.MouseMove += (dx, dy) => OnMouseMove?.Invoke(dx, dy);
            }
        }

        public void Dispose() { /* static hooks remain installed globally; form will uninstall at exit */ }
    }
}
