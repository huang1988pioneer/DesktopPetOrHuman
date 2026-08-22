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
        if (OperatingSystem.IsWindows())
        {
            return ListWindows();
        }

        if (OperatingSystem.IsMacOS())
        {
            return ListMacApps();
        }

        return [];
    }

    private static List<VisibleWindow> ListWindows()
    {
        var windows = new List<VisibleWindow>();

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

    private static List<VisibleWindow> ListMacApps()
    {
        // Match Windows: last visible window counts as closed, even if the
        // app stays in the Dock. NSWorkspace runningApplications would hide
        // that because most Mac apps do not quit on the red close button.
        return ListMacWindows(onScreenOnly: true);
    }

    internal static List<MacRegularApp> ListMacRegularApps()
    {
        return ListMacRunningRegularApps();
    }

    private static readonly HashSet<string> MacIgnoredOwners = new(StringComparer.OrdinalIgnoreCase)
    {
        "Window Server",
        "WindowServer",
        "Dock",
        "Control Center",
        "Control Centre",
        "控制中心",
        "Notification Center",
        "Notification Centre",
        "通知中心",
        "Wallpaper",
        "WallpaperAgent",
        "loginwindow",
        "Spotlight",
        "聚焦",
        "Siri",
        "SystemUIServer",
        "DesktopPetOrHuman",
        "Finder"
    };

    private const uint kCGWindowListOptionOnScreenOnly = 1 << 0;
    private const uint kCGWindowListExcludeDesktopElements = 1 << 4;
    private const uint kCFStringEncodingUTF8 = 0x08000100;
    private const int kCFNumberSInt32Type = 3;

    private static nint _keyLayer;
    private static nint _keyPid;
    private static nint _keyOwner;

    private static List<VisibleWindow> ListMacWindows(bool onScreenOnly)
    {
        var windows = new List<VisibleWindow>();
        nint array = 0;
        try
        {
            EnsureCfKeys();
            var options = kCGWindowListExcludeDesktopElements;
            if (onScreenOnly)
            {
                options |= kCGWindowListOptionOnScreenOnly;
            }

            array = CGWindowListCopyWindowInfo(options, 0);
            if (array == 0)
            {
                return windows;
            }

            var count = CFArrayGetCount(array);
            for (long i = 0; i < count; i++)
            {
                var dict = CFArrayGetValueAtIndex(array, i);
                if (dict == 0 || CfInt(dict, _keyLayer) != 0)
                {
                    continue;
                }

                var processId = CfInt(dict, _keyPid);
                var owner = CfString(dict, _keyOwner);
                if (processId <= 0 || string.IsNullOrWhiteSpace(owner) || MacIgnoredOwners.Contains(owner))
                {
                    continue;
                }

                windows.Add(new VisibleWindow(processId, owner));
            }
        }
        catch
        {
            // Window listing is best-effort on macOS.
        }
        finally
        {
            if (array != 0)
            {
                CFRelease(array);
            }
        }

        return windows;
    }

    private static List<MacRegularApp> ListMacRunningRegularApps()
    {
        var appsFound = new List<MacRegularApp>();
        nint pool = 0;
        try
        {
            dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 1);
            pool = objc_msgSend(objc_msgSend(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc")), sel_registerName("init"));
            var workspace = objc_msgSend(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
            var apps = objc_msgSend(workspace, sel_registerName("runningApplications"));
            var count = objc_msgSend_nuint(apps, sel_registerName("count"));
            var objectAtIndex = sel_registerName("objectAtIndex:");
            var activationPolicy = sel_registerName("activationPolicy");
            var processIdentifier = sel_registerName("processIdentifier");
            var localizedName = sel_registerName("localizedName");
            var utf8String = sel_registerName("UTF8String");
            var isHiddenSel = sel_registerName("isHidden");

            for (nuint i = 0; i < count; i++)
            {
                var app = objc_msgSend_idx(apps, objectAtIndex, i);
                if (app == 0 || objc_msgSend(app, activationPolicy) != 0)
                {
                    continue;
                }

                var processId = objc_msgSend_int(app, processIdentifier);
                var nameObj = objc_msgSend(app, localizedName);
                var utf8 = nameObj == 0 ? 0 : objc_msgSend(nameObj, utf8String);
                var name = utf8 == 0 ? "" : Marshal.PtrToStringUTF8(utf8);
                if (processId <= 0 || string.IsNullOrWhiteSpace(name) || MacIgnoredOwners.Contains(name))
                {
                    continue;
                }

                var hidden = objc_msgSend_int(app, isHiddenSel) != 0;
                appsFound.Add(new MacRegularApp(processId, name, hidden));
            }
        }
        catch
        {
            // Running-app listing is best-effort on macOS.
        }
        finally
        {
            if (pool != 0)
            {
                objc_msgSend(pool, sel_registerName("drain"));
            }
        }

        return appsFound;
    }

    private static void EnsureCfKeys()
    {
        if (_keyLayer != 0)
        {
            return;
        }

        _keyLayer = CFStringCreateWithCString(0, "kCGWindowLayer", kCFStringEncodingUTF8);
        _keyPid = CFStringCreateWithCString(0, "kCGWindowOwnerPID", kCFStringEncodingUTF8);
        _keyOwner = CFStringCreateWithCString(0, "kCGWindowOwnerName", kCFStringEncodingUTF8);
    }

    private static int CfInt(nint dict, nint key)
    {
        var value = CFDictionaryGetValue(dict, key);
        if (value == 0 || !CFNumberGetValue(value, kCFNumberSInt32Type, out var number))
        {
            return 0;
        }

        return number;
    }

    private static string CfString(nint dict, nint key)
    {
        var value = CFDictionaryGetValue(dict, key);
        if (value == 0)
        {
            return "";
        }

        var buffer = new byte[512];
        if (!CFStringGetCString(value, buffer, buffer.Length, kCFStringEncodingUTF8))
        {
            return "";
        }

        var length = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, length < 0 ? buffer.Length : length);
    }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern long CFArrayGetCount(nint theArray);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFArrayGetValueAtIndex(nint theArray, long idx);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDictionaryGetValue(nint theDict, nint key);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFStringCreateWithCString(nint alloc, string cStr, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFStringGetCString(nint s, byte[] buffer, long bufferSize, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFNumberGetValue(nint number, int theType, out int valuePtr);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint cf);

    [DllImport("/usr/lib/libSystem.B.dylib")]
    private static extern nint dlopen(string path, int mode);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern nint objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern nint sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend(nint receiver, nint selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern nuint objc_msgSend_nuint(nint receiver, nint selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend_idx(nint receiver, nint selector, nuint index);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern int objc_msgSend_int(nint receiver, nint selector);

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

internal readonly record struct MacRegularApp(int ProcessId, string Name, bool IsHidden);
