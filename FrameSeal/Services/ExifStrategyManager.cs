using ExifStrategy = System.Func<ImageMagick.IExifProfile?, string?>;

namespace FrameSeal.Services;

using ImageMagick;

/// <summary> 用于提取EXIF元数据的策略管理器 </summary>
internal static class ExifStrategyManager
{
    /// <summary> 用于提取各项EXIF元数据的方法 </summary>
    private static readonly Dictionary<string, ExifStrategy?> Strategies = new() {
        ["手动输入"] = null,
        ["相机型号"] = static exif => exif?.GetValue(ExifTag.Model)?.Value,
        ["相机厂商"] = static exif => exif?.GetValue(ExifTag.Make)?.Value,
        ["镜头型号"] = static exif => exif?.GetValue(ExifTag.LensModel)?.Value,
        ["镜头厂商"] = static exif => exif?.GetValue(ExifTag.LensMake)?.Value,
        ["实际焦距"] = GetFocalLength,
        ["35mm焦距"] = GetFocalLengthIn35MmFilm,
        ["曝光时间"] = GetExposureTime,
        ["光圈F值"] = GetFNumber,
        ["ISO感光度"] = GetIsoSpeedRating,
        ["拍摄时间"] = GetDateTimeOriginal,
        ["作者"] = static exif => exif?.GetValue(ExifTag.Artist)?.Value,
        ["版权"] = static exif => exif?.GetValue(ExifTag.Copyright)?.Value,
        ["软件"] = static exif => exif?.GetValue(ExifTag.Software)?.Value
    };

    /// <returns> 所有可用的EXIF元数据项 </returns>
    public static string[] ExifKeys => [.. Strategies.Keys];

    /// <returns> 用于提取某项EXIF元数据的方法 </returns>
    public static ExifStrategy GetExifStrategy(string key, string manualText) =>
        Strategies.TryGetValue(key, out var func)
            ? func ?? (_ => manualText)
            : throw new KeyNotFoundException($"EXIF 中不存在 {key} 项");

    /// <returns> 焦距元数据 </returns>
    private static string? GetFocalLength(IExifProfile? exif) {
        var f = exif?.GetValue(ExifTag.FocalLength)?.Value.ToDouble();
        return f switch {
            >= 100 => $"{f:0} mm", >= 10 => $"{f:0.#} mm", > 0 => $"{f:0.##} mm", _ => null
        };
    }

    /// <returns> 35mm焦距元数据 </returns>
    private static string? GetFocalLengthIn35MmFilm(IExifProfile? exif) {
        var f = exif?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value;
        return f > 0
            ? $"{f} mm"
            : null;
    }

    /// <returns> 曝光时间元数据 </returns>
    private static string? GetExposureTime(IExifProfile? exif) {
        var s = exif?.GetValue(ExifTag.ExposureTime)?.Value.ToDouble();
        return s switch { >= 0.37 => $"{s:0.#} s", > 0 => $"1/{1 / s:0} s", _ => null };
    }

    /// <returns> 光圈F值元数据 </returns>
    private static string? GetFNumber(IExifProfile? exif) {
        var a = exif?.GetValue(ExifTag.FNumber)?.Value.ToDouble();
        return a switch { >= 8 => $"f/{a:0.#}", > 0 => $"f/{a:0.##}", _ => null };
    }

    /// <returns> ISO感光度元数据 </returns>
    private static string? GetIsoSpeedRating(IExifProfile? exif) {
        var iso = exif?.GetValue(ExifTag.ISOSpeedRatings)?.Value.ElementAtOrDefault(0);
        return iso > 0
            ? $"ISO {iso}"
            : null;
    }

    /// <returns> 拍摄时间元数据 </returns>
    private static string? GetDateTimeOriginal(IExifProfile? exif) {
        var t = exif?.GetValue(ExifTag.DateTimeOriginal)?.Value;
        return string.IsNullOrWhiteSpace(t)
            ? null
            : t;
    }
}
