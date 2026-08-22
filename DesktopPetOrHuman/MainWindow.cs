using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace DesktopPetOrHuman;

public sealed class MainWindow : Window
{
    private readonly Image _petImage;
    private readonly Border _bubble;
    private readonly TextBlock _bubbleText;
    private readonly DispatcherTimer _idleTimer;
    private readonly DispatcherTimer _bubbleTimer;
    private readonly AppLaunchWatcher _appWatcher = new();
    private readonly Random _random = new();
    private readonly CharacterDefinition[] _characters =
    [
        new(
            "喵白白",
            "miaobaibai",
            "喵～白白來陪你了",
            "喵嗚！好開心",
            "白白想睡了…",
            "白白先去睡覺了喵",
            ["白白在這裡喔", "可以摸摸我的帽子", "今天也要乖乖的"]),
        new(
            "喵布布",
            "miaobubu",
            "布布衝出來囉！",
            "嘿嘿，再摸一下！",
            "布布先趴一下…",
            "布布明天再來玩",
            ["有零食嗎", "布布最可愛", "陪我玩嘛"]),
        new(
            "小塗",
            "xiaotu",
            "嘿，小塗報到！",
            "耶！今天也超讚",
            "先喝口茶再睡…",
            "小塗先閃了，掰掰",
            ["作業寫完了嗎", "右鍵可以換朋友", "我會待在桌角"]),
        new(
            "Old Wang Cat",
            "laowangmao",
            "招財進寶！老王貓報到",
            "金元寶來了！",
            "賺飽了，先睡一波",
            "財神先收工，明天見",
            ["財源滾滾來", "摸摸我招好運", "今天也要發大財"]),
        new(
            "鋒兄",
            "fengxiong",
            "鋒兄來了，今天一起慢慢來",
            "哈哈，被你逗樂了",
            "先瞇一下，別吵",
            "鋒兄先走了，別太晚睡",
            ["記得喝水", "工作別太拼", "我在旁邊看著"]),
        new(
            "鋒哥",
            "fengge",
            "鋒哥上線，開始幹活",
            "做得好！",
            "午休十分鐘，別吵",
            "鋒哥下班了",
            ["進度如何？", "會議先放著", "需要幫忙再叫我"]),
        new(
            "小英",
            "xiaoying",
            "小英打卡了",
            "有被鼓勵到…",
            "先閉一下眼…",
            "小英終於可以打卡下班…",
            ["郵件回完了嗎", "再撐一下就下班", "咖啡續一下"]),
        new(
            "喵娘",
            "miaoniang",
            "喵娘來啦～一起玩嘛",
            "喵喵！最喜歡你了",
            "喵娘要捲成一團了",
            "喵娘去睡美容覺囉",
            ["摸摸頭可以嗎", "今天也要被疼愛", "右鍵換我的朋友"]),
        new(
            "塗哥",
            "tuge",
            "塗哥登場！準備開幹",
            "耶嘿！超有精神",
            "才沒有累…只是躺一下",
            "塗哥先撤！明天再衝",
            ["今天也要第一", "別發呆啦", "跟塗哥一起加油"]),
        new(
            "牙妹",
            "yamei",
            "牙妹來陪你了",
            "呵呵，好開心",
            "蓋好被子，晚安",
            "牙妹先回家了，晚安",
            ["功課寫了嗎", "要記得休息", "我會安靜陪著你"]),
        new(
            "魚妹",
            "yumei",
            "撲通～魚妹游過來了",
            "尾巴都在搖了！",
            "縮回貝殼裡睡一下",
            "魚妹游回海里囉",
            ["這裡也有海的味道", "想聽海浪嗎", "別把我弄乾了"]),
        new(
            "ググガガ",
            "gugugaga",
            "ググ！ガガ！企鵝報到",
            "ガガガ！好開心",
            "企鵝要站著睡了…Zzz",
            "ググ…ペンギン寝る",
            ["ググガガ", "今天也要滑行", "肚子想吃魚"])
    ];
    private Bitmap[] _moods = [];
    private CharacterDefinition _currentCharacter;
    private int _idleTick;
    private bool _isDragging;
    private bool _farewellStarted;
    private bool _farewellDone;

    public MainWindow()
    {
        _currentCharacter = _characters[0];

        Width = 210;
        Height = 250;
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        ExtendClientAreaToDecorationsHint = true;
        WindowDecorations = WindowDecorations.None;
        Title = "Desktop Pet";

        LoadCharacter(_currentCharacter);

        _petImage = new Image
        {
            Source = _moods[0],
            Width = 170,
            Height = 170,
            Stretch = Stretch.Uniform,
            RenderTransformOrigin = RelativePoint.Center,
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(1, 1),
                    new RotateTransform(0)
                }
            }
        };

        _bubbleText = new TextBlock
        {
            Text = _currentCharacter.Greeting,
            FontSize = 14,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        _bubble = new Border
        {
            IsVisible = false,
            Background = new SolidColorBrush(Color.FromArgb(220, 20, 26, 38)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6),
            MaxWidth = 180,
            Child = _bubbleText
        };

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        root.Children.Add(_bubble);
        Grid.SetRow(_petImage, 1);
        root.Children.Add(_petImage);

        Content = root;
        ContextMenu = BuildMenu();

        PointerPressed += OnPointerPressed;
        PointerReleased += (_, _) => _isDragging = false;
        DoubleTapped += (_, _) => Cheer();
        Opened += (_, _) =>
        {
            MoveToLowerRight();
            ShowBubble(_currentCharacter.Greeting, 3.2);
            _appWatcher.Start();
        };
        Closed += (_, _) => _appWatcher.Dispose();
        Closing += OnWindowClosing;

        _appWatcher.AppChanged += OnAppChanged;

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _idleTimer.Tick += (_, _) => AnimateIdle();
        _idleTimer.Start();

        _bubbleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.4) };
        _bubbleTimer.Tick += (_, _) => HideBubble();
    }

    private static Bitmap LoadAsset(string fileName)
    {
        var uri = new Uri($"avares://DesktopPetOrHuman/Assets/{fileName}");
        return new Bitmap(AssetLoader.Open(uri));
    }

    private static Bitmap LoadCharacterAsset(string key, string mood)
    {
        return LoadAsset($"Characters/{key}_{mood}.png");
    }

    private ContextMenu BuildMenu()
    {
        var stayOnTop = new MenuItem { Header = "取消置頂" };
        stayOnTop.Click += (_, _) =>
        {
            Topmost = !Topmost;
            stayOnTop.Header = Topmost ? "取消置頂" : "保持置頂";
        };

        var bigger = new MenuItem { Header = "變大" };
        bigger.Click += (_, _) => ResizePet(1.12);

        var smaller = new MenuItem { Header = "變小" };
        smaller.Click += (_, _) => ResizePet(0.9);

        var sleep = new MenuItem { Header = "睡一下" };
        sleep.Click += (_, _) => SetMood(2, _currentCharacter.Sleep);

        var characters = new MenuItem { Header = "切換角色" };
        characters.ItemsSource = _characters.Select(character =>
        {
            var item = new MenuItem { Header = character.DisplayName };
            item.Click += (_, _) => SwitchCharacter(character);
            return item;
        }).ToArray();

        var reset = new MenuItem { Header = "回到右下角" };
        reset.Click += (_, _) => MoveToLowerRight();

        var quit = new MenuItem { Header = "離開" };
        quit.Click += (_, _) => Close();

        return new ContextMenu
        {
            ItemsSource = new object[]
            {
                stayOnTop,
                bigger,
                smaller,
                sleep,
                characters,
                reset,
                new Separator(),
                quit
            }
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            BeginMoveDrag(e);
        }
    }

    private void AnimateIdle()
    {
        _idleTick++;
        var scale = 1 + Math.Sin(_idleTick / 9.0) * 0.018;
        var tilt = Math.Sin(_idleTick / 15.0) * 2.3;

        if (_petImage.RenderTransform is TransformGroup group)
        {
            if (group.Children[0] is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = 1 / scale;
            }

            if (group.Children[1] is RotateTransform rotateTransform)
            {
                rotateTransform.Angle = _isDragging ? tilt * 2.0 : tilt;
            }
        }

        if (_idleTick % 95 == 0 && !_bubble.IsVisible)
        {
            var messages = _currentCharacter.Idle;
            ShowBubble(messages[_random.Next(messages.Length)]);
        }
    }

    private void Cheer()
    {
        SetMood(1, _currentCharacter.Cheer);
    }

    private void OnAppChanged(AppWatchEvent watchEvent)
    {
        ShowBubble(AppDialogue.For(_currentCharacter.Key, watchEvent), 3.4);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_farewellDone)
        {
            return;
        }

        e.Cancel = true;
        if (_farewellStarted)
        {
            return;
        }

        _farewellStarted = true;
        _appWatcher.Dispose();
        ShowBubble(_currentCharacter.Farewell, 2.4);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _farewellDone = true;
            Close();
        };
        timer.Start();
    }

    private void SetMood(int moodIndex, string message)
    {
        _petImage.Source = _moods[moodIndex];
        ShowBubble(message);

        var restoreTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        restoreTimer.Tick += (_, _) =>
        {
            restoreTimer.Stop();
            _petImage.Source = _moods[0];
        };
        restoreTimer.Start();
    }

    private void ShowBubble(string message, double seconds = 2.4)
    {
        _bubbleText.Text = message;
        _bubble.IsVisible = true;
        _bubbleTimer.Stop();
        _bubbleTimer.Interval = TimeSpan.FromSeconds(seconds);
        _bubbleTimer.Start();
    }

    private void HideBubble()
    {
        _bubbleTimer.Stop();
        _bubble.IsVisible = false;
    }

    private void ResizePet(double factor)
    {
        Width = Math.Clamp(Width * factor, 150, 360);
        Height = Math.Clamp(Height * factor, 180, 420);
        _petImage.Width = Math.Clamp(_petImage.Width * factor, 115, 285);
        _petImage.Height = Math.Clamp(_petImage.Height * factor, 115, 285);
        KeepInsideScreen();
    }

    private void MoveToLowerRight()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        Position = new PixelPoint(
            workArea.Right - WindowPixelWidth - 24,
            workArea.Bottom - WindowPixelHeight - 24);
    }

    private void KeepInsideScreen()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        Position = new PixelPoint(
            Math.Clamp(Position.X, workArea.X, workArea.Right - WindowPixelWidth),
            Math.Clamp(Position.Y, workArea.Y, workArea.Bottom - WindowPixelHeight));
    }

    private int WindowPixelWidth => Math.Max(1, (int)Math.Ceiling(Bounds.Width * RenderScaling));

    private int WindowPixelHeight => Math.Max(1, (int)Math.Ceiling(Bounds.Height * RenderScaling));

    private void SwitchCharacter(CharacterDefinition character)
    {
        _currentCharacter = character;
        LoadCharacter(character);
        _petImage.Source = _moods[0];
        ShowBubble(character.Greeting, 3.2);
    }

    private void LoadCharacter(CharacterDefinition character)
    {
        _moods =
        [
            LoadCharacterAsset(character.Key, "idle"),
            LoadCharacterAsset(character.Key, "happy"),
            LoadCharacterAsset(character.Key, "sleep")
        ];
    }

    private readonly record struct CharacterDefinition(
        string DisplayName,
        string Key,
        string Greeting,
        string Cheer,
        string Sleep,
        string Farewell,
        string[] Idle);
}
