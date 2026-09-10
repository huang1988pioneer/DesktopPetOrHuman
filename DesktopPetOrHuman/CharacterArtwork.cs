using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Svg;

namespace DesktopPetOrHuman;

internal static class CharacterArtwork
{
    // Keep the image as vector drawing commands so every pet size and display
    // scale renders from the original paths, without an intermediate bitmap.
    public static IImage Load(string key, string mood)
    {
        var uri = new Uri($"avares://DesktopPetOrHuman/Assets/Characters/{key}_{mood}.svg");
        using var stream = AssetLoader.Open(uri);
        var source = SvgSource.Load(stream);
        if (source.Picture is null)
        {
            throw new InvalidDataException($"Could not load character artwork: {uri}");
        }

        return new SvgImage { Source = source };
    }
}
