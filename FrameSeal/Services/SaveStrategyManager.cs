using ImageMagick;
using System.IO;
using SaveStrategy = System.Action<ImageMagick.MagickImage, string>;

namespace FrameSeal.Services;

/// <summary> 用于保存图像的策略管理器 </summary>
internal static class SaveStrategyManager
{
    /// <returns> 所有可用的保存格式 </returns>
    public static string[] SaveFormats => [.. _strategies.Keys];

    /// <returns> 用于保存为特定格式的方法 </returns>
    public static SaveStrategy GetSaveStrategy(string key)
        => _strategies.TryGetValue(key, out var action)
        ? action
        : throw new KeyNotFoundException($"不支持保存为 {key} 格式");

    /// <summary> 用于保存为各种格式的方法 </summary>
    private static readonly Dictionary<string, SaveStrategy> _strategies = new()
    {
        ["BMP"] = SaveAsBmp,
        ["JPG 95质量"] = SaveAsJpeg95,
        ["JPG 100质量"] = SaveAsJpeg100,
        ["PNG 8位RGB"] = SaveAsPng24,
        ["PNG 8位RGBA"] = SaveAsPng32,
        ["PNG 16位RGB"] = SaveAsPng48,
        ["PNG 16位RGBA"] = SaveAsPng64,
        ["TIF ZIP压缩"] = SaveAsTiff,
        ["WebP 无损"] = SaveAsWebP,
    };

    /// <summary> 生成输出路径 </summary>
    private static string GenOutputPath(string inputPath, string ext)
    {
        var dir = Path.GetDirectoryName(inputPath);
        if (!Path.Exists(dir))
            throw new ArgumentException("无法获取输入路径的目录");
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var outputPath = Path.Combine(dir, $"{name}_FrameSeal{ext}");
        for (var i = 2; File.Exists(outputPath); i++)
            outputPath = Path.Combine(dir, $"{name}_FrameSeal_{i}{ext}");
        return outputPath;
    }

    /// <summary> 保存为BMP格式 </summary>
    private static void SaveAsBmp(MagickImage image, string inputPath)
    {
        var outputPath = GenOutputPath(inputPath, ".bmp");
        image.Write(outputPath, MagickFormat.Bmp3);
    }

    /// <summary> 保存为JPEG格式，95质量 </summary>
    private static void SaveAsJpeg95(MagickImage image, string inputPath)
    {
        image.Quality = 95;
        image.Settings.Interlace = Interlace.Plane;
        image.Settings.SetDefine(MagickFormat.Jpeg, "optimize-coding", true);
        image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
        var outputPath = GenOutputPath(inputPath, ".jpg");
        image.Write(outputPath, MagickFormat.Jpeg);
    }

    /// <summary> 保存为JPEG格式，100质量 </summary>
    private static void SaveAsJpeg100(MagickImage image, string inputPath)
    {
        image.Quality = 100;
        image.Settings.Interlace = Interlace.Plane;
        image.Settings.SetDefine(MagickFormat.Jpeg, "optimize-coding", true);
        image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
        var outputPath = GenOutputPath(inputPath, ".jpg");
        image.Write(outputPath, MagickFormat.Jpeg);
    }

    /// <summary> 保存为8位RGB PNG格式 </summary>
    private static void SaveAsPng24(MagickImage image, string inputPath)
    {
        image.Quality = 100;
        image.Settings.Compression = CompressionMethod.Zip;
        var outputPath = GenOutputPath(inputPath, ".png");
        image.Write(outputPath, MagickFormat.Png24);
    }

    /// <summary> 保存为8位RGBA PNG格式 </summary>
    private static void SaveAsPng32(MagickImage image, string inputPath)
    {
        image.Quality = 100;
        image.Settings.Compression = CompressionMethod.Zip;
        var outputPath = GenOutputPath(inputPath, ".png");
        image.Write(outputPath, MagickFormat.Png32);
    }

    /// <summary> 保存为16位RGB PNG格式 </summary>
    private static void SaveAsPng48(MagickImage image, string inputPath)
    {
        image.Quality = 100;
        image.Settings.Compression = CompressionMethod.Zip;
        var outputPath = GenOutputPath(inputPath, ".png");
        image.Write(outputPath, MagickFormat.Png48);
    }

    /// <summary> 保存为16位RGBA PNG格式 </summary>
    private static void SaveAsPng64(MagickImage image, string inputPath)
    {
        image.Quality = 100;
        image.Settings.Compression = CompressionMethod.Zip;
        var outputPath = GenOutputPath(inputPath, ".png");
        image.Write(outputPath, MagickFormat.Png64);
    }

    /// <summary> 保存为TIFF格式 </summary>
    private static void SaveAsTiff(MagickImage image, string inputPath)
    {
        image.Settings.Compression = CompressionMethod.Zip;
        var outputPath = GenOutputPath(inputPath, ".tif");
        image.Write(outputPath, MagickFormat.Tiff);
    }

    /// <summary> 保存为WebP格式 </summary>
    private static void SaveAsWebP(MagickImage image, string inputPath)
    {
        image.Quality = 100;
        image.Settings.SetDefine(MagickFormat.WebP, "lossless", true);
        var outputPath = GenOutputPath(inputPath, ".webp");
        image.Write(outputPath, MagickFormat.WebP);
    }
}
