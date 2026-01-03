namespace FrameSeal.Core;

using ImageMagick;

/// <summary> 提供将图像信息转换为字符串的方法 </summary>
internal static class InfoFuncs
{
    /// <summary> 信息无效或为空时的占位字符串 </summary>
    private const string Tofu = "---";

    /// <summary> 将图像信息转换为字符串的方法 </summary>
    private static readonly Dictionary<string, Func<IExifProfile?, string>?> Funcs = new() {
        ["自定义"] = null,
        ["曝光时间"] = GetExposureTime,
        ["实际焦距"] = GetFocalLength,
        ["35mm焦距"] = GetFocalLengthIn35MmFilm,
        ["光圈F值"] = GetFNumber,
        ["ISO感光度"] = GetIsoSpeedRating,
        ["拍摄时间"] = GetDateTimeOriginal,
        ["相机"] = static exif => exif?.GetValue(ExifTag.Model)?.Value ?? Tofu,
        ["相机厂"] = static exif => exif?.GetValue(ExifTag.Make)?.Value ?? Tofu,
        ["镜头"] = static exif => exif?.GetValue(ExifTag.LensModel)?.Value ?? Tofu,
        ["镜头厂"] = static exif => exif?.GetValue(ExifTag.LensMake)?.Value ?? Tofu,
        ["作者"] = static exif => exif?.GetValue(ExifTag.Artist)?.Value ?? Tofu,
        ["版权"] = static exif => exif?.GetValue(ExifTag.Copyright)?.Value ?? Tofu,
        ["软件"] = static exif => exif?.GetValue(ExifTag.Software)?.Value ?? Tofu
    };

    /// <summary> 所有可用方法的键名 </summary>
    public static IReadOnlyCollection<string> Keys => Funcs.Keys;

    /// <summary> 获取将图像信息转换为字符串的方法 </summary>
    /// <param name="key"> 键名 </param>
    /// <param name="info"> 自定义信息 </param>
    /// <returns> 委托：输入<see cref="IExifProfile"/>，得到string </returns>
    public static Func<IExifProfile?, string> Get(string key, string info) =>
        Funcs.TryGetValue(key, out var func)
            ? func ?? (_ => info)
            : throw new KeyNotFoundException($"不存在获取`{key}`的方法");

    /// <summary> 获取曝光时间 </summary>
    private static string GetExposureTime(IExifProfile? exif) {
        var t = exif?.GetValue(ExifTag.ExposureTime)?.Value.ToDouble();
        return t switch {
            > 100 => $"{t:0} s", > 0.37 => $"{t:0.#} s", > 0 => $"1/{1 / t:0} s", _ => Tofu
        };
    }

    /// <summary> 获取实际焦距 </summary>
    private static string GetFocalLength(IExifProfile? exif) {
        var f = exif?.GetValue(ExifTag.FocalLength)?.Value.ToDouble();
        return f switch {
            >= 100 => $"{f:0} mm", >= 10 => $"{f:0.#} mm", > 0 => $"{f:0.##} mm", _ => Tofu
        };
    }

    /// <summary> 获取35mm等效焦距 </summary>
    private static string GetFocalLengthIn35MmFilm(IExifProfile? exif) {
        var f = exif?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value;
        return f > 0
            ? $"{f} mm"
            : Tofu;
    }

    /// <summary> 获取光圈F值 </summary>
    private static string GetFNumber(IExifProfile? exif) {
        var a = exif?.GetValue(ExifTag.FNumber)?.Value.ToDouble();
        return a switch { >= 8 => $"f/{a:0.#}", > 0 => $"f/{a:0.##}", _ => Tofu };
    }

    /// <summary> 获取ISO感光度 </summary>
    private static string GetIsoSpeedRating(IExifProfile? exif) {
        var iso = exif?.GetValue(ExifTag.ISOSpeedRatings)?.Value.ElementAtOrDefault(0);
        return iso > 0
            ? $"ISO {iso}"
            : Tofu;
    }

    /// <summary> 获取拍摄时间 </summary>
    private static string GetDateTimeOriginal(IExifProfile? exif) {
        var t = exif?.GetValue(ExifTag.DateTimeOriginal)?.Value;
        return string.IsNullOrWhiteSpace(t)
            ? Tofu
            : t;
    }
}
