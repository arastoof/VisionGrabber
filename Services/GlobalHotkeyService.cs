using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using VisionGrabber.Utilities;
using VisionGrabber.Models;

namespace VisionGrabber.Services
{
    /// <summary>
    /// Provides services for registering and handling global system hotkeys.
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource _source;
        private Action _onHotkeyTriggered;
        private bool _isDisposed;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Initializes the hotkey service for a specific window.
        /// </summary>
        /// <param name="window">The window that will receive the hotkey messages.</param>
        /// <param name="onHotkeyTriggered">The action to perform when the hotkey is pressed.</param>
        public void Initialize(Window window, Action onHotkeyTriggered)
        {
            _windowHandle = new WindowInteropHelper(window).EnsureHandle();
            _onHotkeyTriggered = onHotkeyTriggered;

            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            Register();
        }

        /// <summary>
        /// Registers or re-registers the global hotkey based on current settings.
        /// </summary>
        /// <returns>True if registration was successful; otherwise, false.</returns>
        public bool Register()
        {
            if (_windowHandle == IntPtr.Zero) return false;

            // Unregister first to allow for dynamic setting updates
            UnregisterHotKey(_windowHandle, HOTKEY_ID);

            var s = SettingsManager.Current;
            uint modifiers = 0;
            if (s.ShortcutAlt) modifiers |= MOD_ALT;
            if (s.ShortcutCtrl) modifiers |= MOD_CONTROL;
            if (s.ShortcutShift) modifiers |= MOD_SHIFT;
            if (s.ShortcutWin) modifiers |= MOD_WIN;

            uint vk = 0x54; // Default to 'T' (0x54)
            if (!string.IsNullOrEmpty(s.ShortcutKey))
            {
                char c = s.ShortcutKey.ToUpper()[0];
                vk = (uint)c;
            }

            return RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, vk);
        }

        /// <summary>
        /// Message hook for handling window messages, specifically for detecting global hotkeys.
        /// </summary>
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                _onHotkeyTriggered?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Partially unregisters the hotkey and disposes of COM resources.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_windowHandle != IntPtr.Zero)
                {
                    UnregisterHotKey(_windowHandle, HOTKEY_ID);
                }
                if (_source != null)
                {
                    _source.RemoveHook(HwndHook);
                }
                _isDisposed = true;
            }
        }
    }
}
