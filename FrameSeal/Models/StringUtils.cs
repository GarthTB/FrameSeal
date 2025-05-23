using ImageMagick;

namespace FrameSeal.Models;

/// <summary>
/// This class is used to parse strings
/// </summary>
internal static class StringUtils
{
    /// <summary>
    /// Clamps a double value between a minimum and maximum value,
    /// or returns a default value if the value is not a valid double
    /// </summary>
    internal static double ClampOrDefault(
        this string value, double min, double max, double defaultValue)
        => double.TryParse(value, out var v)
            ? Math.Clamp(v, min, max)
            : defaultValue;

    /// <summary>
    /// Parses a color string,
    /// or returns red if the string is not a valid color
    /// </summary>
    internal static MagickColor ParseColorOrRed(this string color)
    {
        try { return new($"#{color}"); }
        catch { return MagickColors.Red; }
    }

    /// <summary>
    /// Parses a string value from an ExifProfile,
    /// or returns a default value if the value is not found
    /// </summary>
    internal static string ParseExifOrDefault(
        this IExifProfile? exif, SettingsModel settings)
    {
        var f = settings.DefaultFocalLength[0] == '*'
            ? settings.DefaultFocalLength.Split('*')[1]
            : exif.ParseFocalLength() ?? settings.DefaultFocalLength;
        var s = settings.DefaultExposureTime[0] == '*'
            ? settings.DefaultExposureTime.Split('*')[1]
            : exif.ParseExposureTime() ?? settings.DefaultExposureTime;
        var a = settings.DefaultAperture[0] == '*'
            ? settings.DefaultAperture.Split('*')[1]
            : exif.ParseAperture() ?? settings.DefaultAperture;
        var iso = settings.DefaultISO[0] == '*'
            ? settings.DefaultISO.Split('*')[1]
            : exif.ParseISO() ?? settings.DefaultISO;
        var t = settings.DefaultCreationTime[0] == '*'
            ? settings.DefaultCreationTime.Split('*')[1]
            : exif.ParseCreationTime() ?? settings.DefaultCreationTime;

        string?[] info = [f, s, a, iso, t];
        return string.Join("  ", info.Where(s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>
    /// Parses the focal length from the exif data
    /// </summary>
    private static string? ParseFocalLength(this IExifProfile? exif)
    {
        if (exif is null) return null;

        var actual = exif.GetValue(ExifTag.FocalLength)?.Value.ToDouble();
        var equiv = exif.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value;

        return actual is not null and > 0
            ? actual < 10
                ? $"{actual:#.#} mm"
                : $"{actual:#} mm"
            : equiv is not null and > 0
                ? $"{equiv} mm"
                : null;
    }

    /// <summary>
    /// Parses the exposure time from the exif data
    /// </summary>
    private static string? ParseExposureTime(this IExifProfile? exif)
    {
        if (exif is null) return null;

        var s = exif.GetValue(ExifTag.ExposureTime)?.Value.ToDouble();

        return s is not null and > 0
            ? s > 0.34
                ? $"{s:#.#} s"
                : $"1/{1 / s:#} s"
            : null;
    }

    /// <summary>
    /// Parses the aperture from the exif data
    /// </summary>
    private static string? ParseAperture(this IExifProfile? exif)
    {
        if (exif is null) return null;

        var a = exif.GetValue(ExifTag.FNumber)?.Value.ToDouble();

        return a is not null and > 0
            ? a < 2
                ? $"f/{a:#.##}"
                : $"f/{a:#.#}"
            : null;
    }

    /// <summary>
    /// Parses the ISO from the exif data
    /// </summary>
    private static string? ParseISO(this IExifProfile? exif)
    {
        if (exif is null) return null;

        var iso = exif.GetValue(ExifTag.ISOSpeedRatings)?.Value;

        return iso is not null && iso.Length > 0
            ? $"ISO {iso[0]}"
            : null;
    }

    /// <summary>
    /// Parses the creation time from the exif data
    /// </summary>
    private static string? ParseCreationTime(this IExifProfile? exif)
    {
        if (exif is null) return null;

        var t = exif.GetValue(ExifTag.DateTimeOriginal)?.Value;

        return string.IsNullOrWhiteSpace(t)
            ? null
            : $"{t.Split(' ')[0].Replace(':', '-')} {t.Split(' ')[1]}";
    }
}
