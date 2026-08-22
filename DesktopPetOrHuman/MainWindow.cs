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
    private readonly Random _random = new();
    private readonly CharacterDefinition[] _characters =
    [
        new("喵白白", "miaobaibai"),
        new("喵白白試產", "miaobaibai_trial"),
        new("喵布布", "miaobubu"),
        new("喵布布試產", "miaobubu_trial"),
        new("小塗", "xiaotu"),
        new("小塗試產", "xiaotu_trial"),
        new("Old Wang Cat", "laowangmao"),
        new("Old Wang Cat 試產", "laowangmao_trial"),
        new("鋒兄", "fengxiong"),
        new("鋒兄試產", "fengxiong_trial"),
        new("鋒哥", "fengge"),
        new("鋒哥試產", "fengge_trial"),
        new("小英", "xiaoying"),
        new("小英試產", "xiaoying_trial"),
        new("喵娘", "miaoniang"),
        new("喵娘試產", "miaoniang_trial"),
        new("塗哥", "tuge"),
        new("塗哥試產", "tuge_trial"),
        new("牙妹", "yamei"),
        new("牙妹試產", "yamei_trial"),
        new("魚妹", "yumei"),
        new("魚妹試產", "yumei_trial"),
        new("ググガガ", "gugugaga"),
        new("ググガガ試產", "gugugaga_trial")
    ];
    private Bitmap[] _moods = [];
    private CharacterDefinition _currentCharacter;
    private int _idleTick;
    private bool _isDragging;

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
            Text = "嗨！",
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
        Opened += (_, _) => MoveToLowerRight();

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
        sleep.Click += (_, _) => SetMood(2, "Zzz...");

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
            var messages = new[] { $"{_currentCharacter.DisplayName}在這裡！", "點兩下會開心", "右鍵可以換角色", "我會乖乖待在桌面上" };
            ShowBubble(messages[_random.Next(messages.Length)]);
        }
    }

    private void Cheer()
    {
        SetMood(1, "嘿嘿！");
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

    private void ShowBubble(string message)
    {
        _bubbleText.Text = message;
        _bubble.IsVisible = true;
        _bubbleTimer.Stop();
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
        ShowBubble($"我是{character.DisplayName}");
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

    private readonly record struct CharacterDefinition(string DisplayName, string Key);
}
