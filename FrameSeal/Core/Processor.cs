namespace FrameSeal.Core;

using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;
using ImageMagick.Drawing;

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
        var scale = Math.Sqrt(1048576d / w / h);
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
        using MagickImage? icon = cfg.IconPath is null
            ? null
            : new(cfg.IconPath);
        Proc(thumb, icon, cfg, token);

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
        using MagickImage? icon = cfg.IconPath is null
            ? null
            : new(cfg.IconPath);
        List<string> issues = new(imgPaths.Length);
        foreach (var imgPath in imgPaths)
            try {
                using MagickImage img = new(imgPath);
                Proc(img, icon, cfg, null);
                save(img, imgPath);
            } catch (Exception ex) {
                issues.Add($"{imgPath}: {ex.Message}");
            }
        return issues;
    }

    /// <summary> 处理单个图像 </summary>
    /// <param name="img"> 待处理图像 </param>
    /// <param name="icon"> 图标（null时禁用） </param>
    /// <param name="cfg"> 处理配置 </param>
    /// <param name="token"> 取消令牌 </param>
    private static void Proc(
        MagickImage img,
        MagickImage? icon,
        Cfg cfg,
        CancellationToken? token) {
        var (w, h) = (img.Width, img.Height);
        var x = (int)Math.Round(w * cfg.BorderRatio.L);
        var y = (int)Math.Round(h * cfg.BorderRatio.T);
        var bottom = Math.Round(h * cfg.BorderRatio.B);
        var newW = (uint)(x + w + Math.Round(w * cfg.BorderRatio.R));
        var newH = (uint)(y + h + bottom);

        var borderColor = ParseColorOrRed(cfg.BorderColor);
        AddBorder();
        if (Math.Min(w, h) * cfg.CornerRatio is var radius and > 0)
            RoundCorner();

        if (bottom < 1)
            return;
        var text = GetText();
        if (text.Length == 0 && icon is null)
            return;
        Drawables pen = new();
        var (textW, textH, ascent) = SetPenMeasureText();
        var textX = newW / 2d - textW / 2;
        var iconY = Math.Round(newH - bottom / 2 - textH / 2);

        if (icon is {})
            textX += AddIconGetGap();
        if (text.Length > 0)
            return;
        token?.ThrowIfCancellationRequested();
        _ = pen.Text(textX, iconY + ascent, text).Draw(img);
        return;

        static MagickColor ParseColorOrRed(string colorStr) {
            try {
                return new(colorStr);
            } catch {
                return MagickColors.Red;
            }
        }

        void AddBorder() {
            token?.ThrowIfCancellationRequested();
            img.Alpha(AlphaOption.Set);
            img.BackgroundColor = borderColor;
            img.Extent(x, y, newW, newH);
        }

        void RoundCorner() {
            token?.ThrowIfCancellationRequested();
            using MagickImage mask = new(MagickColors.White, newW, newH);
            mask.Draw( // 内黑外白的圆角矩形
                new DrawableFillColor(MagickColors.None),
                new DrawableRoundRectangle(x, y, x + img.Width, y + img.Height, radius, radius));

            token?.ThrowIfCancellationRequested();
            mask.GaussianBlur(1, 0.5); // 抗锯齿

            token?.ThrowIfCancellationRequested();
            using MagickImage border = new(borderColor, newW, newH);
            border.SetWriteMask(mask);
            img.Composite(border, CompositeOperator.Over);
        }

        string GetText() {
            token?.ThrowIfCancellationRequested();
            var exif = img.GetExifProfile();
            var items = cfg.InfoFuncs.Select(func => func(exif))
                .Where(static s => !string.IsNullOrWhiteSpace(s));
            return string.Join("  ", items);
        }

        (double, double, double) SetPenMeasureText() {
            var textColor = ParseColorOrRed(cfg.TextColor);
            pen.EnableStrokeAntialias()
                .FillColor(textColor)
                .Font(cfg.FontName)
                .StrokeColor(textColor)
                .StrokeOpacity(new(50));

            for (var (i, size, tgt) = (0, 14d, bottom * cfg.TextRatio); i < 5; i++) {
                token?.ThrowIfCancellationRequested();
                var m = pen.FontPointSize(size).StrokeWidth(0.02 * size).FontTypeMetrics(text)
                     ?? throw new InvalidOperationException("无法测量文字尺寸");
                if (Math.Abs(m.TextHeight - tgt) > tgt * 0.05)
                    size *= tgt / m.TextHeight;
                else
                    return (m.TextWidth, m.TextHeight, m.Ascent);
            }
            throw new InvalidOperationException("5次尝试后仍无法找到合适的字号");
        }

        double AddIconGetGap() {
            token?.ThrowIfCancellationRequested();
            var iconH = (uint)Math.Round(textH);
            using var mIcon = icon.CloneAndMutate(m => m.Resize(uint.MaxValue, iconH)); // 只按高缩放

            token?.ThrowIfCancellationRequested();
            var gapW = cfg.IconGap * textH;
            var iconX = Math.Round(textX - gapW / 2 - mIcon.Width / 2d);
            img.Composite(mIcon, (int)iconX, (int)iconY, CompositeOperator.Over);

            return gapW / 2 + mIcon.Width / 2d;
        }
    }
}
