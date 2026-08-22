using System.Diagnostics;
using Avalonia.Threading;

namespace DesktopPetOrHuman;

internal sealed class AppLaunchWatcher : IDisposable
{
    private static readonly HashSet<string> IgnoredNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DesktopPetOrHuman",
        "dotnet",
        "Idle",
        "System",
        "csrss",
        "smss",
        "wininit",
        "services",
        "lsass",
        "svchost",
        "dwm",
        "fontdrvhost",
        "conhost",
        "dllhost",
        "taskhostw",
        "RuntimeBroker",
        "sihost",
        "ctfmon",
        "SearchHost",
        "StartMenuExperienceHost",
        "ShellExperienceHost",
        "ApplicationFrameHost",
        "TextInputHost",
        "SecurityHealthSystray",
        "SecurityHealthService",
        "Widgets",
        "WidgetService",
        "PhoneExperienceHost",
        "LockApp",
        "LogonUI",
        "explorer",
        "msedgewebview2",
        "crashpad_handler",
        "GoogleCrashHandler",
        "GoogleCrashHandler64",
        "OfficeClickToRun",
        "SearchIndexer",
        "smartscreen",
        "backgroundTaskHost",
        "DataExchangeHost",
        "FileCoAuth",
        "UserOOBEBroker",
        "CrossDeviceResume",
        "GameBar",
        "WidgetBoard",
        "cmd",
        "powershell",
        "pwsh",
        "OpenConsole",
        "wsl",
        "wslhost",
        "wslservice",
        "loginwindow",
        "Dock",
        "SystemUIServer",
        "WindowServer",
        "Window Server",
        "ControlCenter",
        "NotificationCenter",
        "Spotlight",
        "Siri",
        "Wallpaper",
        "WallpaperAgent",
        "Finder"
    };

    private static readonly Dictionary<string, AppLaunch> Catalog = CreateCatalog();

    private readonly DispatcherTimer _timer;
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AppLaunch> _tracked = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AppLaunch> _visibleApps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<AppWatchEvent> _pending = new();
    private readonly Dictionary<string, DateTime> _lastSpoken = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _sessionId = Process.GetCurrentProcess().SessionId;
    private readonly string _selfName = Process.GetCurrentProcess().ProcessName;
    private DateTime _nextSpeakUtc = DateTime.UtcNow.AddSeconds(4.5);
    private bool _polling;

    public event Action<AppWatchEvent>? AppChanged;

    public AppLaunchWatcher()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        _timer.Tick += (_, _) => _ = PollAsync();
    }

    public void Start()
    {
        CaptureSeen();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        AppChanged = null;
    }

    private async Task PollAsync()
    {
        if (_polling)
        {
            return;
        }

        _polling = true;
        try
        {
            var (opened, closed) = await Task.Run(DetectChanges);
            foreach (var launch in opened)
            {
                Enqueue(launch, AppWatchAction.Opened);
            }

            foreach (var launch in closed)
            {
                Enqueue(launch, AppWatchAction.Closed);
            }

            TrySpeak();
        }
        finally
        {
            _polling = false;
        }
    }

    private void Enqueue(AppLaunch launch, AppWatchAction action)
    {
        if (_pending.Any(pending =>
                pending.Action == action
                && string.Equals(pending.App.DisplayName, launch.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var speakKey = SpeakKey(action, launch.DisplayName);
        if (_lastSpoken.TryGetValue(speakKey, out var spokenAt)
            && DateTime.UtcNow - spokenAt < TimeSpan.FromSeconds(75))
        {
            return;
        }

        if (_pending.Count >= 4)
        {
            _pending.Dequeue();
        }

        _pending.Enqueue(new AppWatchEvent(launch, action));
    }

    private void TrySpeak()
    {
        if (_pending.Count == 0 || DateTime.UtcNow < _nextSpeakUtc)
        {
            return;
        }

        var next = _pending.Dequeue();
        _lastSpoken[SpeakKey(next.Action, next.App.DisplayName)] = DateTime.UtcNow;
        _nextSpeakUtc = DateTime.UtcNow.AddSeconds(8);
        AppChanged?.Invoke(next);
    }

    private static string SpeakKey(AppWatchAction action, string displayName) =>
        action == AppWatchAction.Closed ? $"close:{displayName}" : $"open:{displayName}";

    private (AppLaunch[] Opened, AppLaunch[] Closed) DetectChanges()
    {
        var opened = new List<AppLaunch>();
        var closed = new List<AppLaunch>();
        CollectProcessChanges(opened, closed);
        CollectVisibleChanges(opened, closed);
        return (DistinctApps(opened), DistinctApps(closed));
    }

    private void CollectProcessChanges(List<AppLaunch> opened, List<AppLaunch> closed)
    {
        if (OperatingSystem.IsMacOS())
        {
            CollectMacAppChanges(opened, closed);
            return;
        }

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch
        {
            return;
        }

        var namesNow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in processes)
        {
            try
            {
                if (OperatingSystem.IsWindows() && process.SessionId != _sessionId)
                {
                    continue;
                }

                var name = process.ProcessName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                namesNow.Add(name);
                if (_seen.Contains(name) || IsIgnored(name))
                {
                    continue;
                }

                if (TryResolve(process, name, out var launch))
                {
                    opened.Add(launch);
                    _tracked[name] = launch;
                }
            }
            catch
            {
                // Access-denied or exited processes are skipped.
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (var (name, launch) in _tracked.ToArray())
        {
            if (namesNow.Contains(name))
            {
                continue;
            }

            closed.Add(launch);
            _tracked.Remove(name);
        }

        closed.RemoveAll(launch =>
            _tracked.Values.Any(tracked =>
                string.Equals(tracked.DisplayName, launch.DisplayName, StringComparison.OrdinalIgnoreCase)));

        _seen.RemoveWhere(name => !namesNow.Contains(name));
        foreach (var name in namesNow)
        {
            _seen.Add(name);
        }
    }

    private void CollectMacAppChanges(List<AppLaunch> opened, List<AppLaunch> closed)
    {
        var namesNow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var borrowed = new List<Process>();
        try
        {
            foreach (var app in VisibleAppProbe.ListMacRegularApps())
            {
                Process? process = null;
                try
                {
                    process = Process.GetProcessById(app.ProcessId);
                    borrowed.Add(process);
                }
                catch
                {
                    process = null;
                }

                var processName = process?.ProcessName ?? app.Name;
                if (string.IsNullOrWhiteSpace(processName) || IsIgnored(processName) || IsIgnored(app.Name))
                {
                    continue;
                }

                namesNow.Add(processName);
                if (_seen.Contains(processName))
                {
                    continue;
                }

                AppLaunch launch;
                if (process is not null)
                {
                    TryResolveVisible(process, processName, app.Name, out launch);
                }
                else
                {
                    launch = new AppLaunch(TrimDisplay(app.Name), AppKind.Generic, processName, app.ProcessId);
                }

                opened.Add(launch);
                _tracked[processName] = launch;
            }
        }
        finally
        {
            foreach (var process in borrowed)
            {
                process.Dispose();
            }
        }

        foreach (var (name, launch) in _tracked.ToArray())
        {
            if (namesNow.Contains(name))
            {
                continue;
            }

            closed.Add(launch);
            _tracked.Remove(name);
        }

        closed.RemoveAll(launch =>
            _tracked.Values.Any(tracked =>
                string.Equals(tracked.DisplayName, launch.DisplayName, StringComparison.OrdinalIgnoreCase)));

        _seen.RemoveWhere(name => !namesNow.Contains(name));
        foreach (var name in namesNow)
        {
            _seen.Add(name);
        }
    }

    private void CollectVisibleChanges(List<AppLaunch> opened, List<AppLaunch> closed)
    {
        var visibleNow = ScanVisibleApps();
        if (visibleNow.Count == 0 && _visibleApps.Count > 1)
        {
            return;
        }

        foreach (var (displayName, launch) in visibleNow)
        {
            if (!_visibleApps.ContainsKey(displayName))
            {
                opened.Add(launch);
            }
        }

        HashSet<int>? hiddenPids = null;
        if (OperatingSystem.IsMacOS())
        {
            hiddenPids = [];
            foreach (var app in VisibleAppProbe.ListMacRegularApps())
            {
                if (app.IsHidden)
                {
                    hiddenPids.Add(app.ProcessId);
                }
            }
        }

        foreach (var (displayName, launch) in _visibleApps.ToArray())
        {
            if (visibleNow.ContainsKey(displayName))
            {
                continue;
            }

            if (hiddenPids is not null
                && launch.ProcessId > 0
                && hiddenPids.Contains(launch.ProcessId))
            {
                // Cmd+H: keep tracking so unhide is not treated as a new open.
                continue;
            }

            closed.Add(launch);
            _visibleApps.Remove(displayName);
        }

        foreach (var (displayName, launch) in visibleNow)
        {
            _visibleApps[displayName] = launch;
        }
    }

    private Dictionary<string, AppLaunch> ScanVisibleApps()
    {
        var found = new Dictionary<string, AppLaunch>(StringComparer.OrdinalIgnoreCase);
        var byPid = new Dictionary<int, Process>();

        try
        {
            foreach (var window in VisibleAppProbe.List())
            {
                if (!byPid.TryGetValue(window.ProcessId, out var process))
                {
                    try
                    {
                        process = Process.GetProcessById(window.ProcessId);
                    }
                    catch
                    {
                        if (!string.IsNullOrWhiteSpace(window.Title) && !IsIgnored(window.Title))
                        {
                            found.TryAdd(
                                window.Title,
                                new AppLaunch(TrimDisplay(window.Title), AppKind.Generic, window.Title, window.ProcessId));
                        }

                        continue;
                    }

                    byPid[window.ProcessId] = process;
                }

                try
                {
                    if (OperatingSystem.IsWindows() && process.SessionId != _sessionId)
                    {
                        continue;
                    }

                    var name = process.ProcessName;
                    if (string.IsNullOrWhiteSpace(name) || IsIgnored(name))
                    {
                        continue;
                    }

                    if (TryResolveVisible(process, name, window.Title, out var launch))
                    {
                        found.TryAdd(launch.DisplayName, launch);
                    }
                }
                catch
                {
                    // Skip windows we cannot map to a process.
                }
            }
        }
        finally
        {
            foreach (var process in byPid.Values)
            {
                process.Dispose();
            }
        }

        return found;
    }

    private static bool TryResolveVisible(Process process, string processName, string windowTitle, out AppLaunch launch)
    {
        if (Catalog.TryGetValue(processName, out launch))
        {
            launch = launch with { ProcessId = process.Id };
            return true;
        }

        var display = !string.IsNullOrWhiteSpace(windowTitle)
            ? TrimDisplay(windowTitle)
            : ReadDisplayName(process, processName);
        launch = new AppLaunch(display, AppKind.Generic, processName, process.Id);
        return true;
    }

    private static string TrimDisplay(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 16 ? trimmed : trimmed[..14] + "…";
    }

    private static AppLaunch[] DistinctApps(List<AppLaunch> launches) =>
        launches
            .GroupBy(launch => launch.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

    private void CaptureSeen()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                CaptureMacSeen();
            }
            else
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.SessionId != _sessionId || string.IsNullOrWhiteSpace(process.ProcessName))
                        {
                            continue;
                        }

                        var name = process.ProcessName;
                        _seen.Add(name);
                        if (!IsIgnored(name) && TryResolve(process, name, out var launch))
                        {
                            _tracked[name] = launch;
                        }
                    }
                    catch
                    {
                        // Ignore processes we cannot inspect.
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
        }
        catch
        {
            // First snapshot is best-effort.
        }

        foreach (var (displayName, launch) in ScanVisibleApps())
        {
            _visibleApps[displayName] = launch;
        }
    }

    private void CaptureMacSeen()
    {
        foreach (var app in VisibleAppProbe.ListMacRegularApps())
        {
            Process? process = null;
            try
            {
                process = Process.GetProcessById(app.ProcessId);
                var name = process.ProcessName;
                if (string.IsNullOrWhiteSpace(name) || IsIgnored(name) || IsIgnored(app.Name))
                {
                    continue;
                }

                _seen.Add(name);
                if (TryResolveVisible(process, name, app.Name, out var launch))
                {
                    _tracked[name] = launch;
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(app.Name) && !IsIgnored(app.Name))
                {
                    _seen.Add(app.Name);
                    _tracked[app.Name] = new AppLaunch(TrimDisplay(app.Name), AppKind.Generic, app.Name, app.ProcessId);
                }
            }
            finally
            {
                process?.Dispose();
            }
        }
    }

    private bool IsIgnored(string processName)
    {
        if (string.Equals(processName, _selfName, StringComparison.OrdinalIgnoreCase)
            || IgnoredNames.Contains(processName))
        {
            return true;
        }

        return processName.Contains("helper", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("crash", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("update", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("webview", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("setup", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("install", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolve(Process process, string processName, out AppLaunch launch)
    {
        if (Catalog.TryGetValue(processName, out launch))
        {
            launch = launch with { ProcessId = process.Id };
            return true;
        }

        nint handle;
        string title;
        try
        {
            handle = process.MainWindowHandle;
            title = process.MainWindowTitle;
        }
        catch
        {
            launch = default;
            return false;
        }

        if (handle == 0 || string.IsNullOrWhiteSpace(title))
        {
            launch = default;
            return false;
        }

        var display = ReadDisplayName(process, processName);
        launch = new AppLaunch(display, AppKind.Generic, processName, process.Id);
        return true;
    }

    private static string ReadDisplayName(Process process, string processName)
    {
        try
        {
            var description = process.MainModule?.FileVersionInfo.FileDescription;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var trimmed = description.Trim();
                return trimmed.Length <= 16 ? trimmed : trimmed[..14] + "…";
            }
        }
        catch
        {
            // MainModule is often blocked for foreign processes.
        }

        return processName;
    }

    private static Dictionary<string, AppLaunch> CreateCatalog()
    {
        AppLaunch App(string display, AppKind kind, string processName) =>
            new(display, kind, processName);

        var entries = new (string Process, string Display, AppKind Kind)[]
        {
            ("chrome", "Chrome", AppKind.Browser),
            ("msedge", "Edge", AppKind.Browser),
            ("firefox", "Firefox", AppKind.Browser),
            ("brave", "Brave", AppKind.Browser),
            ("opera", "Opera", AppKind.Browser),
            ("Code", "VS Code", AppKind.Code),
            ("Cursor", "Cursor", AppKind.Code),
            ("devenv", "Visual Studio", AppKind.Code),
            ("idea64", "IntelliJ", AppKind.Code),
            ("pycharm64", "PyCharm", AppKind.Code),
            ("webstorm64", "WebStorm", AppKind.Code),
            ("rider64", "Rider", AppKind.Code),
            ("notepad++", "Notepad++", AppKind.Code),
            ("sublime_text", "Sublime", AppKind.Code),
            ("WindowsTerminal", "終端機", AppKind.Terminal),
            ("WindowsTerminalPreview", "終端機", AppKind.Terminal),
            ("WINWORD", "Word", AppKind.Office),
            ("EXCEL", "Excel", AppKind.Office),
            ("POWERPNT", "PowerPoint", AppKind.Office),
            ("ONENOTE", "OneNote", AppKind.Office),
            ("OUTLOOK", "Outlook", AppKind.Office),
            ("wps", "WPS", AppKind.Office),
            ("Acrobat", "Acrobat", AppKind.Office),
            ("AcroRd32", "Acrobat", AppKind.Office),
            ("notepad", "記事本", AppKind.Note),
            ("Notepad", "記事本", AppKind.Note),
            ("obsidian", "Obsidian", AppKind.Note),
            ("Notion", "Notion", AppKind.Note),
            ("Discord", "Discord", AppKind.Chat),
            ("Slack", "Slack", AppKind.Chat),
            ("Teams", "Teams", AppKind.Chat),
            ("ms-teams", "Teams", AppKind.Chat),
            ("Telegram", "Telegram", AppKind.Chat),
            ("LINE", "LINE", AppKind.Chat),
            ("Line", "LINE", AppKind.Chat),
            ("WeChat", "微信", AppKind.Chat),
            ("Weixin", "微信", AppKind.Chat),
            ("WeChatAppEx", "微信", AppKind.Chat),
            ("Skype", "Skype", AppKind.Chat),
            ("Zoom", "Zoom", AppKind.Chat),
            ("Spotify", "Spotify", AppKind.Music),
            ("Music.UI", "媒體播放器", AppKind.Music),
            ("vlc", "VLC", AppKind.Video),
            ("PotPlayerMini64", "PotPlayer", AppKind.Video),
            ("mpc-hc64", "MPC-HC", AppKind.Video),
            ("obs64", "OBS", AppKind.Video),
            ("steam", "Steam", AppKind.Game),
            ("EpicGamesLauncher", "Epic Games", AppKind.Game),
            ("LeagueClient", "英雄聯盟", AppKind.Game),
            ("League of Legends", "英雄聯盟", AppKind.Game),
            ("GenshinImpact", "原神", AppKind.Game),
            ("Minecraft", "Minecraft", AppKind.Game),
            ("RobloxPlayerBeta", "Roblox", AppKind.Game),
            ("Taskmgr", "工作管理員", AppKind.Utility),
            ("CalculatorApp", "小算盤", AppKind.Utility),
            ("mspaint", "小畫家", AppKind.Utility),
            ("PaintApp", "小畫家", AppKind.Utility),
            ("SnippingTool", "剪取工具", AppKind.Utility),
            ("SystemSettings", "設定", AppKind.Utility),
            ("Photos", "相片", AppKind.Utility),
            ("WinStore.App", "Microsoft Store", AppKind.Utility),
            ("ChatGPT", "ChatGPT", AppKind.Code),
            ("Claude", "Claude", AppKind.Code),
            ("Postman", "Postman", AppKind.Code),
            ("Figma", "Figma", AppKind.Code),
            ("Photoshop", "Photoshop", AppKind.Office),
            ("Illustrator", "Illustrator", AppKind.Office),
            ("blender", "Blender", AppKind.Office),
            ("Safari", "Safari", AppKind.Browser),
            ("Google Chrome", "Chrome", AppKind.Browser),
            ("Microsoft Edge", "Edge", AppKind.Browser),
            ("Brave Browser", "Brave", AppKind.Browser),
            ("Arc", "Arc", AppKind.Browser),
            ("Terminal", "終端機", AppKind.Terminal),
            ("iTerm2", "iTerm", AppKind.Terminal),
            ("iTerm", "iTerm", AppKind.Terminal),
            ("Warp", "Warp", AppKind.Terminal),
            ("Alacritty", "Alacritty", AppKind.Terminal),
            ("kitty", "kitty", AppKind.Terminal),
            ("Ghostty", "Ghostty", AppKind.Terminal),
            ("TextEdit", "文字編輯", AppKind.Note),
            ("Notes", "備忘錄", AppKind.Note),
            ("Preview", "預覽", AppKind.Office),
            ("Finder", "Finder", AppKind.Utility),
            ("Calculator", "計算機", AppKind.Utility),
            ("Calendar", "行事曆", AppKind.Utility),
            ("Reminders", "提醒事項", AppKind.Utility),
            ("Mail", "郵件", AppKind.Chat),
            ("Messages", "訊息", AppKind.Chat),
            ("FaceTime", "FaceTime", AppKind.Chat),
            ("Music", "音樂", AppKind.Music),
            ("TV", "TV", AppKind.Video),
            ("QuickTime Player", "QuickTime", AppKind.Video),
            ("IINA", "IINA", AppKind.Video),
            ("Movist Pro", "Movist Pro", AppKind.Video),
            ("Xcode", "Xcode", AppKind.Code),
            ("Simulator", "Simulator", AppKind.Code),
            ("Zed", "Zed", AppKind.Code),
            ("Windsurf", "Windsurf", AppKind.Code),
            ("Grok", "Grok", AppKind.Code),
            ("System Settings", "系統設定", AppKind.Utility),
            ("System Preferences", "系統偏好設定", AppKind.Utility),
            ("Activity Monitor", "活動監視器", AppKind.Utility),
            ("App Store", "App Store", AppKind.Utility),
            ("Freeform", "無邊記", AppKind.Note),
            ("Pages", "Pages", AppKind.Office),
            ("Numbers", "Numbers", AppKind.Office),
            ("Keynote", "Keynote", AppKind.Office),
            ("Microsoft Word", "Word", AppKind.Office),
            ("Microsoft Excel", "Excel", AppKind.Office),
            ("Microsoft PowerPoint", "PowerPoint", AppKind.Office),
            ("Microsoft Outlook", "Outlook", AppKind.Office),
            ("Microsoft Teams", "Teams", AppKind.Chat)
        };

        var catalog = new Dictionary<string, AppLaunch>(StringComparer.OrdinalIgnoreCase);
        foreach (var (process, display, kind) in entries)
        {
            catalog[process] = App(display, kind, process);
        }

        return catalog;
    }
}

internal readonly record struct AppLaunch(string DisplayName, AppKind Kind, string ProcessName, int ProcessId = 0);

internal readonly record struct AppWatchEvent(AppLaunch App, AppWatchAction Action);

internal enum AppWatchAction
{
    Opened,
    Closed
}

internal enum AppKind
{
    Browser,
    Code,
    Office,
    Chat,
    Music,
    Video,
    Game,
    Note,
    Terminal,
    Utility,
    Generic
}
