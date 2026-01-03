namespace FrameSeal.Core;

using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;
using ImageMagick.Drawing;

/// <summary> 核心处理 </summary>
internal static class Processor
{
    /// <summary> 载入图像并缩小到不超过2^20像素 </summary>
    /// <param name="imgPath"> 图像路径 </param>
    /// <param name="token"> 取消令牌 </param>
    public static async Task<MagickImage> GetThumb(string imgPath, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        MagickImage img = new();
        await img.ReadAsync(imgPath, token);

        token.ThrowIfCancellationRequested();
        var (w, h) = (img.Width, img.Height);
        var scale = Math.Sqrt(1048576d / w / h);
        if (scale < 1)
            img.Resize(
                (uint)Math.Round(w * scale),
                (uint)Math.Round(h * scale),
                FilterType.Mitchell);
        return img;
    }

    /// <summary> 处理缩略图并生成用于显示的预览图 </summary>
    /// <param name="thumb"> 缩略图 </param>
    /// <param name="config"> 处理配置 </param>
    /// <param name="token"> 取消令牌 </param>
    public static async Task<BitmapImage> Preview(
        MagickImage thumb,
        Config config,
        CancellationToken token) {
        token.ThrowIfCancellationRequested();
        using (var icon = config.Icon)
            ProcOne(thumb, icon, config, token);

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
    /// <param name="config"> 处理配置 </param>
    /// <param name="save"> 保存图像的方法 </param>
    /// <returns> 处理失败的图像路径及异常信息 </returns>
    public static IReadOnlyList<string> Run(
        string[] imgPaths,
        Config config,
        Action<MagickImage, string> save) {
        if (imgPaths.Length == 0)
            throw new ArgumentException("没有待处理的图像", nameof(imgPaths));
        List<string> issues = new(imgPaths.Length);
        using var icon = config.Icon;
        foreach (var imgPath in imgPaths)
            try {
                using MagickImage img = new(imgPath);
                ProcOne(img, icon, config, null);
                save(img, imgPath);
            } catch (Exception ex) {
                issues.Add($"{imgPath}: {ex.Message}");
            }
        return issues;
    }

    /// <summary> 处理单个图像 </summary>
    /// <param name="img"> 待处理图像 </param>
    /// <param name="icon"> 图标（null时禁用） </param>
    /// <param name="config"> 处理配置 </param>
    /// <param name="token"> 取消令牌 </param>
    private static void ProcOne(
        MagickImage img,
        MagickImage? icon,
        Config config,
        CancellationToken? token) {
        var (oldW, oldH) = (img.Width, img.Height);
        var (t, r, b, l) = config.GetBorderWidth(oldW, oldH);
        var (newW, newH) = (r + oldW + l, t + oldH + b);

        AddBorder();
        if (config.GetCornerRadius(Math.Min(oldW, oldH)) is var radius and > 0)
            RoundCorner();

        if (b < 1)
            return;

        var text = GetText();
        var targetTextH = config.GetTextTargetH(b);
        var (setPen, textW, textH, ascent) = string.IsNullOrWhiteSpace(text)
            ? GetPenMeasureText()
            : (null, 0, targetTextH, 0);
        var (textX, textY) = ((newW - textW) / 2, newH - (b + textH) / 2);

        if (icon is {})
            textX += GetBiasAddIcon();
        token?.ThrowIfCancellationRequested();
        setPen?.Text(textX, textY + ascent, text).Draw(img);

        void AddBorder() {
            token?.ThrowIfCancellationRequested();
            img.Alpha(AlphaOption.Set);
            img.BackgroundColor = config.BorderColor;
            img.Extent((int)l, (int)t, newW, newH);
        }

        void RoundCorner() {
            token?.ThrowIfCancellationRequested();
            using MagickImage mask = new(MagickColors.White, newW, newH);
            new Drawables().FillColor(MagickColors.None)
                .RoundRectangle(l, t, l + oldW, t + oldH, radius, radius)
                .Draw(mask); // 内黑外白的圆角矩形

            token?.ThrowIfCancellationRequested();
            mask.GaussianBlur(1, 0.5); // 抗锯齿

            token?.ThrowIfCancellationRequested();
            using MagickImage border = new(config.BorderColor, newW, newH);
            border.SetWriteMask(mask);
            img.Composite(border, CompositeOperator.Over);
        }

        string GetText() {
            token?.ThrowIfCancellationRequested();
            var exif = img.GetExifProfile();
            var items = config.InfoFuncs.Select(func => func(exif))
                .Where(static s => !string.IsNullOrWhiteSpace(s));
            return string.Join("  ", items);
        }

        (IDrawables<ushort>, double, double, double) GetPenMeasureText() {
            var pen = new Drawables().FillColor(config.TextColor)
                .Font(config.FontName)
                .StrokeColor(config.TextColor)
                .StrokeOpacity(new(50));

            for (var (i, size) = (0, 14d); i < 5; i++) {
                token?.ThrowIfCancellationRequested();
                var m = pen.FontPointSize(size).StrokeWidth(0.02 * size).FontTypeMetrics(text)
                     ?? throw new InvalidOperationException("无法测量文字尺寸");
                if (Math.Abs(m.TextHeight - targetTextH) < targetTextH * 0.1)
                    return (pen, m.TextWidth, m.TextHeight, m.Ascent);
                size *= targetTextH / m.TextHeight;
            }
            throw new InvalidOperationException("5次尝试后仍无法找到合适的字号");
        }

        double GetBiasAddIcon() {
            token?.ThrowIfCancellationRequested();
            using var mIcon = icon.CloneAndMutate(m => m.Resize(
                uint.MaxValue,
                (uint)Math.Round(textH))); // 只按高缩放，忽略宽度

            token?.ThrowIfCancellationRequested();
            var gap = config.GetIconGap(textH);
            var iconX = (int)Math.Round(textX - (gap + mIcon.Width) / 2);
            var iconY = (int)Math.Round(textY);
            img.Composite(mIcon, iconX, iconY, CompositeOperator.Over);

            return (gap + mIcon.Width) / 2;
        }
    }
}
