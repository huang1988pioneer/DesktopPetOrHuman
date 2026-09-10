using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DesktopPetOrHuman;
using SkiaSharp;

// Exercise the same embedded assets, loader, Image control, and renderer as the
// desktop app. No windows are shown and no saved user preferences are written.
var output = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/character-review");
Directory.CreateDirectory(output);
AppBuilder.Configure<App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .WithInterFont()
    .SetupWithoutStarting();

var pet = new MainWindow();
const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
object Read(string field) => typeof(MainWindow).GetField(field, Private)!.GetValue(pet)!;
void Call(string method, params object[] values) => typeof(MainWindow).GetMethod(method, Private)!.Invoke(pet, values);
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var characters = (Array)Read("_characters");
var assets = new List<(string Key, string Name, string Mood, IImage Image)>();
string[] moodKeys = ["idle", "happy", "sleep"];
var allHashes = new HashSet<string>();
foreach (var character in characters)
{
    var type = character!.GetType();
    var key = (string)type.GetProperty("Key")!.GetValue(character)!;
    var name = (string)type.GetProperty("DisplayName")!.GetValue(character)!;
    Call("LoadCharacter", character);
    var moods = (IImage[])Read("_moods");
    Call("LoadCharacter", character);
    Check(ReferenceEquals(moods, Read("_moods")), $"{key}: cached images were replaced");

    for (var i = 0; i < moodKeys.Length; i++)
    {
        var mood = moodKeys[i];
        var uri = new Uri($"avares://DesktopPetOrHuman/Assets/Characters/{key}_{mood}.svg");
        using var stream = AssetLoader.Open(uri);
        var svg = XDocument.Load(stream);
        Check(svg.Root?.Attribute("viewBox")?.Value == "0 0 320 320", $"{key}/{mood}: unexpected viewBox");
        Check(!svg.Descendants().Any(node => node.Name.LocalName is "image" or "script" or "foreignObject"),
            $"{key}/{mood}: artwork must be pure, self-contained vectors");
        Check(moods[i].Size == new Size(320, 320), $"{key}/{mood}: empty or incorrectly sized SVG");
        assets.Add((key, name, mood, moods[i]));
        foreach (var size in new[] { 115, 157.5, 200, 242.5, 285 })
        {
            foreach (var scale in new[] { 1, 2 })
            {
                using var rendered = Render(new Image { Source = moods[i], Stretch = Stretch.Uniform }, size, size, scale);
                using var png = new MemoryStream();
                rendered.Save(png, PngBitmapEncoderOptions.Default);
                var bytes = png.ToArray();
                using var pixels = SKBitmap.Decode(bytes);
                Check(pixels is not null, $"{key}/{mood}: could not decode rendered image");
                var opaque = 0;
                var bounds = new SKRectI(pixels!.Width, pixels.Height, 0, 0);
                for (var y = 0; y < pixels.Height; y++)
                for (var x = 0; x < pixels.Width; x++)
                {
                    if (pixels.GetPixel(x, y).Alpha < 16) continue;
                    opaque++;
                    bounds.Left = Math.Min(bounds.Left, x);
                    bounds.Top = Math.Min(bounds.Top, y);
                    bounds.Right = Math.Max(bounds.Right, x);
                    bounds.Bottom = Math.Max(bounds.Bottom, y);
                }
                Check(opaque > pixels.Width * pixels.Height * .12, $"{key}/{mood}: blank or mostly missing artwork");
                Check(opaque < pixels.Width * pixels.Height * .80, $"{key}/{mood}: opaque background");
                Check(bounds.Left > 0 && bounds.Top > 0 && bounds.Right < pixels.Width - 1 && bounds.Bottom < pixels.Height - 1,
                    $"{key}/{mood} at {size}/{scale}x: artwork clipped at image boundary");
                if (size == 285 && scale == 1)
                {
                    Check(allHashes.Add(Convert.ToHexString(SHA256.HashData(bytes))), $"{key}/{mood}: duplicate artwork");
                }
            }
        }
    }
}
Check(assets.Count == 36, $"Expected 36 character states; got {assets.Count}");

// Verify mood assignment and the existing five resize steps still use vectors.
var image = (Image)Read("_petImage");
for (var mood = 0; mood < 3; mood++)
{
    Call("SetMood", mood, "preview");
    Check(ReferenceEquals(image.Source, ((IImage[])Read("_moods"))[mood]), "Mood assignment failed");
}
for (var step = 0; step < 5; step++)
{
    typeof(MainWindow).GetField("_sizeStep", Private)!.SetValue(pet, step);
    Call("ApplySizeStep");
    Check(image.Width == 115 + step * 42.5 && image.Height == image.Width, "Resize step changed");
}
Call("AnimateIdle");
Check(image.RenderTransform is TransformGroup transform && transform.Children.Count == 2, "Idle transform lost");
((DispatcherTimer)Read("_idleTimer")).Stop();
((DispatcherTimer)Read("_bubbleTimer")).Stop();

// Two readable contact sheets show all states at the shipped size extremes.
SaveSheet("characters-large", 285, 2, false);
SaveSheet("characters-small-dark", 115, 3, true);
SaveLineup();
Console.WriteLine($"PASS: {characters.Length} characters, {assets.Count} unique states, 360 vector renders (five sizes, 1x/2x DPI), transparent margins, mood/size/cache checks.");
Console.WriteLine($"Preview images: {output}");

RenderTargetBitmap Render(Control control, double width, double height, int scale = 1)
{
    control.Width = width;
    control.Height = height;
    control.Measure(new Size(width, height));
    control.Arrange(new Rect(0, 0, width, height));
    var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(width * scale), (int)Math.Ceiling(height * scale)), new Vector(96 * scale, 96 * scale));
    bitmap.Render(control);
    return bitmap;
}

void SaveSheet(string file, int size, int columns, bool dark)
{
    var rows = characters.Length / columns;
    var groupWidth = size * 3 + 28;
    var groupHeight = size + 68;
    var background = dark ? "#252830" : "#f5f0e6";
    var foreground = dark ? "#f5f0e6" : "#493e46";
    var panel = new Canvas { Background = SolidColorBrush.Parse(background) };
    for (var i = 0; i < characters.Length; i++)
    {
        var x = 20 + i % columns * groupWidth;
        var y = 20 + i / columns * groupHeight;
        AddLabel(panel, assets[i * 3].Name, x + 8, y, 17, foreground);
        for (var mood = 0; mood < 3; mood++)
        {
            var asset = assets[i * 3 + mood];
            var art = new Image { Source = asset.Image, Width = size, Height = size };
            Canvas.SetLeft(art, x + mood * size);
            Canvas.SetTop(art, y + 24);
            panel.Children.Add(art);
            AddLabel(panel, moodKeys[mood], x + mood * size + size / 2 - 16, y + 28 + size, 12, foreground);
        }
    }
    using var result = Render(panel, columns * groupWidth + 40, rows * groupHeight + 40);
    result.Save(Path.Combine(output, file + ".png"), PngBitmapEncoderOptions.Default);
}

void SaveLineup()
{
    var panel = new Canvas { Background = SolidColorBrush.Parse("#f5f0e6") };
    for (var i = 0; i < characters.Length; i++)
    {
        var asset = assets[i * 3];
        var x = 16 + i % 6 * 190;
        var y = 12 + i / 6 * 218;
        var art = new Image { Source = asset.Image, Width = 190, Height = 190 };
        Canvas.SetLeft(art, x);
        Canvas.SetTop(art, y);
        panel.Children.Add(art);
        var label = new TextBlock { Text = asset.Name, Width = 190, FontSize = 15, TextAlignment = TextAlignment.Center, Foreground = SolidColorBrush.Parse("#493e46") };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y + 189);
        panel.Children.Add(label);
    }
    using var result = Render(panel, 1172, 448);
    result.Save(Path.Combine(output, "characters-lineup.png"), PngBitmapEncoderOptions.Default);
}

void AddLabel(Canvas canvas, string label, double x, double y, double size, string color)
{
    var text = new TextBlock { Text = label, FontSize = size, Foreground = SolidColorBrush.Parse(color) };
    Canvas.SetLeft(text, x);
    Canvas.SetTop(text, y);
    canvas.Children.Add(text);
}
