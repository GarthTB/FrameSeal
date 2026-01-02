namespace FrameSeal.Core;

using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;

/// <summary> 核心处理 </summary>
internal static class Processor
{
    /// <summary> 载入图像并缩小 </summary>
    /// <param name="imgPath"> 图像路径 </param>
    /// <param name="token"> 取消令牌 </param>
    /// <returns> 不超过2^20像素的小图 </returns>
    public static async Task<MagickImage> GetThumb(string imgPath, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        MagickImage img = new();
        await img.ReadAsync(imgPath, token);

        token.ThrowIfCancellationRequested();
        var (w, h) = (img.Width, img.Height);
        var scale = Math.Sqrt(1048576.0 / w / h);
        if (scale < 1)
            img.Resize(
                (uint)Math.Round(w * scale),
                (uint)Math.Round(h * scale),
                FilterType.Mitchell);
        return img;
    }

    /// <summary> 处理缩略图并生成预览图 </summary>
    /// <param name="thumb"> 缩略图 </param>
    /// <param name="cfg"> 处理配置 </param>
    /// <param name="token"> 取消令牌 </param>
    /// <returns> 可直接显示在界面上的预览图 </returns>
    public static async Task<BitmapImage> Preview(
        MagickImage thumb,
        Cfg cfg,
        CancellationToken token) {
        token.ThrowIfCancellationRequested();
        cfg.ThrowIfInvalid();
        Proc(thumb, cfg, token);

        using MemoryStream ms = new();
        await thumb.WriteAsync(ms, MagickFormat.Bmp, token);
        ms.Position = 0;

        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary> 处理图像 </summary>
    /// <param name="imgPaths"> 待处理图像的路径 </param>
    /// <param name="cfg"> 处理配置 </param>
    /// <param name="save"> 保存图像的方法 </param>
    /// <returns> 处理失败的图像路径及错误信息 </returns>
    public static IReadOnlyList<string> Run(
        string[] imgPaths,
        Cfg cfg,
        Action<MagickImage, string> save) {
        if (imgPaths.Length == 0)
            throw new ArgumentException("没有待处理的图像", nameof(imgPaths));
        cfg.ThrowIfInvalid();
        List<string> issues = new(imgPaths.Length);
        foreach (var imgPath in imgPaths)
            try {
                using MagickImage img = new(imgPath);
                Proc(img, cfg, null);
                save(img, imgPath);
            } catch (Exception ex) {
                issues.Add($"{imgPath}: {ex.Message}");
            }
        return issues;
    }

    /// <summary> 处理单个图像 </summary>
    /// <param name="img"> 待处理图像 </param>
    /// <param name="cfg"> 处理配置 </param>
    /// <param name="token"> 取消令牌 </param>
    private static void Proc(MagickImage img, Cfg cfg, CancellationToken? token) {
        throw new NotImplementedException();
    }
}
