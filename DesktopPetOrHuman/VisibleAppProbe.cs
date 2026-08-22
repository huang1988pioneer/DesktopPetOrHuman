using System.Runtime.InteropServices;
using System.Text;

namespace DesktopPetOrHuman;

internal static class VisibleAppProbe
{
    private const uint GwOwner = 4;
    private const int GwlExStyle = -20;
    private const int WsExToolwindow = 0x00000080;
    private const int DwmwaCloaked = 14;

    public static List<VisibleWindow> List()
    {
        var windows = new List<VisibleWindow>();
        if (!OperatingSystem.IsWindows())
        {
            return windows;
        }

        EnumWindows((hwnd, lParam) =>
        {
            if (!IsWindowVisible(hwnd) || GetWindow(hwnd, GwOwner) != 0)
            {
                return true;
            }

            if ((GetWindowLong(hwnd, GwlExStyle) & WsExToolwindow) != 0 || IsCloaked(hwnd))
            {
                return true;
            }

            var title = ReadTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId != 0)
            {
                windows.Add(new VisibleWindow(processId, title));
            }

            return true;
        }, 0);

        return windows;
    }

    private static bool IsCloaked(nint hwnd)
    {
        return DwmGetWindowAttribute(hwnd, DwmwaCloaked, out var cloaked, sizeof(int)) == 0
            && cloaked != 0;
    }

    private static string ReadTitle(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return "";
        }

        var buffer = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
}

internal readonly record struct VisibleWindow(int ProcessId, string Title);
