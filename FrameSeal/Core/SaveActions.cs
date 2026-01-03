namespace FrameSeal.Core;

using System.IO;
using ImageMagick;

/// <summary> 提供保存图像的方法 </summary>
internal static class SaveActions
{
    /// <summary> 保存图像的方法 </summary>
    private static readonly Dictionary<string, Action<MagickImage, string>> Actions = new() {
        ["BMP"] = static (img, inPath) => img.Write(GenOutPath(inPath, "bmp"), MagickFormat.Bmp),
        ["JPG 70质量"] = static (img, inPath) => SaveJpg(img, inPath, 70),
        ["JPG 80质量"] = static (img, inPath) => SaveJpg(img, inPath, 80),
        ["JPG 90质量"] = static (img, inPath) => SaveJpg(img, inPath, 90),
        ["JPG 96质量"] = static (img, inPath) => SaveJpg(img, inPath, 96),
        ["JPG 100质量"] = static (img, inPath) => SaveJpg(img, inPath, 100),
        ["PNG 8位RGB"] = static (img, inPath) => SavePng(img, inPath, MagickFormat.Png24),
        ["PNG 8位RGBA"] = static (img, inPath) => SavePng(img, inPath, MagickFormat.Png32),
        ["PNG 16位RGB"] = static (img, inPath) => SavePng(img, inPath, MagickFormat.Png48),
        ["PNG 16位RGBA"] = static (img, inPath) => SavePng(img, inPath, MagickFormat.Png64),
        ["WebP 无损"] = static (img, inPath) => {
            img.Settings.SetDefine(MagickFormat.WebP, "lossless", true);
            img.Write(GenOutPath(inPath, "webp"), MagickFormat.WebP);
        }
    };

    /// <summary> 所有可用方法的键名 </summary>
    public static IReadOnlyCollection<string> Keys => Actions.Keys;

    /// <summary> 获取保存图像的方法 </summary>
    /// <param name="key"> 键名 </param>
    /// <returns> 委托：输入<see cref="MagickImage"/>和源路径，保存至同目录 </returns>
    public static Action<MagickImage, string> Get(string key) =>
        Actions.TryGetValue(key, out var action)
            ? action
            : throw new KeyNotFoundException($"不支持保存为`{key}`");

    /// <summary> 生成输出路径 </summary>
    /// <param name="inPath"> 原始图像路径 </param>
    /// <param name="ext"> 输出文件的扩展名 </param>
    private static string GenOutPath(string inPath, string ext) {
        var dir = Path.GetDirectoryName(inPath);
        if (!Path.Exists(dir))
            throw new DirectoryNotFoundException($"找不到路径`{inPath}`的目录");
        var name = Path.GetFileNameWithoutExtension(inPath);
        var outPath = Path.Combine(dir, $"{name}_FrameSeal.{ext}");
        for (var i = 2; File.Exists(outPath); i++)
            outPath = Path.Combine(dir, $"{name}_FrameSeal_{i}.{ext}");
        return outPath;
    }

    /// <summary> 保存为特定质量的JPG </summary>
    /// <param name="img"> 待保存的图像 </param>
    /// <param name="inPath"> 原始图像路径 </param>
    /// <param name="quality"> JPG质量 </param>
    private static void SaveJpg(MagickImage img, string inPath, uint quality) {
        img.Quality = quality;
        img.Settings.SetDefine(MagickFormat.Pjpeg, "arithmetic-coding", true);
        img.Settings.SetDefine(MagickFormat.Pjpeg, "sampling-factor", "4:2:0");
        img.Write(GenOutPath(inPath, "jpg"), MagickFormat.Pjpeg);
    }

    /// <summary> 保存为特定位深的PNG </summary>
    /// <param name="img"> 待保存的图像 </param>
    /// <param name="inPath"> 原始图像路径 </param>
    /// <param name="format"> 保存格式 </param>
    private static void SavePng(MagickImage img, string inPath, MagickFormat format) {
        img.Quality = 100;
        img.Write(GenOutPath(inPath, "png"), format);
    }
}
