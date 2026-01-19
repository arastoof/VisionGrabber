using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisionGrabber.Services
{
    /// <summary>
    /// Manages the system tray icon and its associated context menu.
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private NotifyIcon _notifyIcon;

        /// <summary>
        /// Initializes the tray icon with the specified actions for menu items.
        /// </summary>
        public void Initialize(Action showWindow, Action captureArea, Action openSettings, Action exitApp)
        {
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            Icon appIcon = System.IO.File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Visible = true,
                Text = "VisionGrabber"
            };

            _notifyIcon.DoubleClick += (s, args) => showWindow?.Invoke();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Capture Area", null, (s, e) => captureArea?.Invoke());
            contextMenu.Items.Add("Show Window", null, (s, e) => showWindow?.Invoke());
            contextMenu.Items.Add("Settings", null, (s, e) => openSettings?.Invoke());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => exitApp?.Invoke());

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// Removes the tray icon and disposes of resources.
        /// </summary>
        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
