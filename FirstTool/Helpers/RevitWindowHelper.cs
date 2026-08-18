using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FirstTool.Helpers
{
    public static class RevitWindowHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void BringToFront(Window window)
        {
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Activate();

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
                SetForegroundWindow(hwnd);
        }
    }
}