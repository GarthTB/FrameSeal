using ImageMagick;
using ImageMagick.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace FrameSeal.Models;

/// <summary>
/// Utilities for MagickImage operations
/// </summary>
internal static class ImageUtils
{
    /// <summary>
    /// Rounds the corners of an image
    /// </summary>
    internal static MagickImage RoundCorner(
        this MagickImage image, double cornerRatio)
    {
        if (cornerRatio <= 0)
            return image;

        var r = cornerRatio * Math.Min(image.Width, image.Height);
        using MagickImage mask = new(MagickColors.None, image.Width, image.Height);
        mask.Settings.FillColor = MagickColors.White;
        mask.Draw(new DrawableRoundRectangle(0, 0, image.Width, image.Height, r, r));
        mask.Blur(0, 1.0 / 3); // antialiasing
        image.Alpha(AlphaOption.Set);
        image.Composite(mask, CompositeOperator.CopyAlpha);

        return image;
    }

    /// <summary>
    /// Mounts an image with a border
    /// </summary>
    internal static MagickImage Mount(
        this MagickImage image, SettingsModel settings)
    {
        MagickImage mounted = new(
            settings.BorderColor,
            (uint)(image.Width * (1 + settings.BorderLeft + settings.BorderRight)),
            (uint)(image.Height * (1 + settings.BorderTop + settings.BorderBottom)));
        mounted.Composite(
            image,
            (int)(settings.BorderLeft * image.Width),
            (int)(settings.BorderTop * image.Height),
            CompositeOperator.Over);
        return mounted;
    }

    /// <summary>
    /// Writes the Exif information to the mounted image
    /// </summary>
    internal static MagickImage WriteInfo(
        this MagickImage mounted, SettingsModel settings, MagickImage image)
    {
        if (settings.BorderBottom == 0)
            return mounted;

        string info = image.GetExifProfile().ParseExifOrDefault(settings);
        mounted.Settings.FontFamily = settings.FontName;
        var (textWidth, textHeight, textAscent, fontPoint) =
            mounted.MeasureText(info,
                settings.TextRatio * settings.BorderBottom * image.Height);

        var spacing = settings.BorderBottom * image.Height - textHeight;
        var textOffsetX = (mounted.Width - textWidth) / 2;
        var textOffsetY = (1 + settings.BorderTop) * image.Height + spacing / 2;

        if (settings.Icon is not null)
            mounted.MountIcon(
                (MagickImage)settings.Icon.Clone(),
                settings.IconMargin,
                textHeight,
                ref textOffsetX,
                textOffsetY);

        _ = new Drawables().FillColor(settings.TextColor)
            .Font(settings.FontName)
            .FontPointSize(fontPoint)
            .StrokeColor(settings.TextColor)
            .StrokeOpacity(new Percentage(50))
            .StrokeWidth(fontPoint * 0.01)
            .Text(textOffsetX, textOffsetY + textAscent, info)
            .Draw(mounted);

        return mounted;
    }

    /// <summary>
    /// Measures the size of a text string in a given target height
    /// </summary>
    private static (double, double, double, double) MeasureText(
        this MagickImage bg, string info, double targetHeight)
    {
        bg.Settings.FontPointsize = 24;
        var metrics0 = bg.FontTypeMetrics(info)
            ?? throw new ArgumentException("Cannot measure text");
        var scale = targetHeight / metrics0.TextHeight;

        bg.Settings.FontPointsize = 24 * scale;
        var metrics1 = bg.FontTypeMetrics(info)
            ?? throw new ArgumentException("Cannot measure text");

        return (metrics1.TextWidth,
            metrics1.TextHeight,
            metrics1.Ascent,
            24 * scale);
    }

    /// <summary>
    /// Mounts an icon to the mounted image
    /// </summary>
    private static void MountIcon(
        this MagickImage image,
        MagickImage icon,
        double iconMargin,
        double targetHeight,
        ref double textOffsetX,
        double iconOffsetY)
    {
        var scale = targetHeight / icon.Height;
        icon.Resize(new Percentage(100 * scale));

        textOffsetX += (icon.Width + targetHeight * iconMargin) / 2.0;
        var iconOffsetX = textOffsetX - (icon.Width + targetHeight * iconMargin);
        image.Composite(
            icon, (int)iconOffsetX, (int)iconOffsetY, CompositeOperator.Over);

        icon.Dispose();
    }

    /// <summary>
    /// Converts a MagickImage to a BitmapImage
    /// </summary>
    internal static BitmapImage ToBitmap(this MagickImage image)
    {
        BitmapImage bitmap = new();
        using (MemoryStream ms = new())
        {
            image.Format = MagickFormat.Bmp3;
            image.Write(ms);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
        }
        bitmap.Freeze();
        return bitmap;
    }
}
