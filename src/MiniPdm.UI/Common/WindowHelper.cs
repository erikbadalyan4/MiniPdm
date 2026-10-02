using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MiniPdm.UI.Common;

public static class WindowHelper
{
    public static readonly DependencyProperty RemoveIconProperty =
        DependencyProperty.RegisterAttached(
            "RemoveIcon",
            typeof(bool),
            typeof(WindowHelper),
            new PropertyMetadata(false, OnRemoveIconChanged));

    public static bool GetRemoveIcon(DependencyObject obj) => (bool)obj.GetValue(RemoveIconProperty);
    public static void SetRemoveIcon(DependencyObject obj, bool value) => obj.SetValue(RemoveIconProperty, value);

    private static void OnRemoveIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window && (bool)e.NewValue)
        {
            if (window.IsLoaded)
            {
                ApplyRemoveIcon(window);
            }
            else
            {
                window.SourceInitialized += (s, _) => ApplyRemoveIcon(window);
            }
        }
    }

    public static void ApplyRemoveIcon(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);

        SendMessage(hwnd, WM_SETICON, new IntPtr(1), IntPtr.Zero);
        SendMessage(hwnd, WM_SETICON, new IntPtr(0), IntPtr.Zero);

        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_DLGMODALFRAME = 0x0001;
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_FRAMECHANGED = 0x0020;
    private const uint WM_SETICON = 0x0080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
}
