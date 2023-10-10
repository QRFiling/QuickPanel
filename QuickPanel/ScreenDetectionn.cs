using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace QuickPanel
{
    //http://holstcoding.blogspot.com/2011/02/wpf-c-how-to-detect-if-another.html
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public class ScreenDetectionn
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowRect(IntPtr hwnd, out RECT rc);

        static IntPtr desktopHandle = GetDesktopWindow();
        static IntPtr shellHandle = GetShellWindow();

        static Rect screenBounds = new Rect(0, 0, SystemParameters.PrimaryScreenWidth,
            SystemParameters.PrimaryScreenHeight);

        public static bool AreApplicationFullScreen()
        {
            RECT appBounds;
            IntPtr hWnd;
            hWnd = GetForegroundWindow();

            if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
            {
                if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
                {
                    GetWindowRect(hWnd, out appBounds);

                    return (appBounds.Bottom - appBounds.Top) == screenBounds.Height &&
                        (appBounds.Right - appBounds.Left) == screenBounds.Width;
                }
            }

            return false;
        }
    }
}
