using System;

namespace WootMouseRemap
{
    public sealed class ModeManager
    {
        public AppMode Current { get; private set; } = AppMode.KbmToPad;
        public event Action<AppMode>? Changed;
        private readonly XInputPassthrough? _xpass; // reserved for future use
        public ModeManager() { }
        public ModeManager(XInputPassthrough xpass) => _xpass = xpass;
        public void Set(AppMode mode)
        {
            if (mode == Current) return;
            Current = mode;
            // Manage passthrough lifecycle based on mode
            try
            {
                if (_xpass != null)
                {
                    if (mode == AppMode.Passthrough) _xpass.Start();
                    else _xpass.Stop();
                }
            }
            catch { }
            Changed?.Invoke(mode);
        }
        public void Next()
        {
            var next = Current == AppMode.KbmToPad ? AppMode.Passthrough : AppMode.KbmToPad;
            Set(next);
        }
    }
}
