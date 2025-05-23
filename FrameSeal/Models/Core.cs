using ImageMagick;
using System.Windows.Media.Imaging;

namespace FrameSeal.Models;

/// <summary>
/// Core processing functions
/// </summary>
internal static class Core
{
    /// <summary>
    /// Process preview image
    /// </summary>
    internal static BitmapImage ProcessPreview(SettingsModel settings)
    {
        using MagickImage image = new(
            settings.InputOutputPaths.First().Item1);

        return image.RoundCorner(settings.CornerRatio)
            .Mount(settings)
            .WriteInfo(settings, image)
            .ToBitmap();
    }

    /// <summary>
    /// Process images and return the count of successfully processed images
    /// </summary>
    internal static int ProcessImages(SettingsModel settings)
    {
        var count = 0;

        settings.InputOutputPaths.AsParallel().ForAll(paths =>
        {
            try
            {
                using MagickImage image = new(paths.Item1);
                var result = image.RoundCorner(settings.CornerRatio)
                    .Mount(settings)
                    .WriteInfo(settings, image);
                result.Quality = 98;
                result.Write(paths.Item2);
                _ = Interlocked.Increment(ref count);
            }
            catch { } // Ignore invalid images
        });

        return count;
    }
}
