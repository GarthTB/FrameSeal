using FrameSeal.Models;
using ImageMagick;
using ImageMagick.Drawing;

namespace FrameSeal.Services;

/// <summary> 核心处理方法 </summary>
internal static class CoreProcessor
{
    /// <summary> 处理图像 </summary>
    public static MagickImage Process(
        this MagickImage image, Settings settings, CancellationToken? ct)
    {
        settings.ThrowIfInvalid();

        ct?.ThrowIfCancellationRequested();
        if (settings.CornerRadius > 0)
            image.RoundCorner(settings.CornerRadius);
        else if (settings.BorderSize
            is { Top: 0, Right: 0, Bottom: 0, Left: 0 }) // 无圆角也无边框
            return image;

        ct?.ThrowIfCancellationRequested();
        var result = image.AddBorder(
            settings.BorderColor, settings.BorderSize);
        var bottomH = image.Height * settings.BorderSize.Bottom;
        if (bottomH == 0) // 无下边框
            return result;

        ct?.ThrowIfCancellationRequested();
        var exif = image.GetExifProfile();
        var items = settings.ExifFuncs.Select(func => func(exif))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var info = string.Join("  ", items);
        if (settings.Icon is null
            && string.IsNullOrWhiteSpace(info)) // 无图标也无文字
            return result;

        ct?.ThrowIfCancellationRequested();
        var targetHeight = settings.TextHeight * bottomH;
        var (pen, ascent, textW, textH) = string.IsNullOrWhiteSpace(info)
            ? (null, 0, 0, targetHeight)
            : GetPen(info, settings.FontFamily, settings.TextColor, targetHeight);
        var iconY = Math.Round(result.Height - bottomH / 2 - textH / 2);
        var textX = result.Width / 2 - textW / 2;

        ct?.ThrowIfCancellationRequested();
        if (settings.Icon is MagickImage icon) // 会修改textX
        {
            var h = Math.Round(textH);
            var w = (uint)Math.Round(icon.Width * h / icon.Height);
            using var mIcon = icon.CloneAndMutate(
                icon => icon.Resize(w + 1, (uint)h)); // 忽略宽度，只按高缩放

            var gapW = settings.IconGap * textH;
            textX += gapW / 2 + mIcon.Width / 2;
            var iconX = (int)Math.Round(textX - gapW / 2 - mIcon.Width / 2);
            result.Composite(mIcon, iconX, (int)iconY, CompositeOperator.Over);
        }

        ct?.ThrowIfCancellationRequested();
        _ = pen?.Text(textX, iconY + ascent, info).Draw(result);

        return result;
    }

    /// <summary> 将图像切去圆角 </summary>
    private static void RoundCorner(
        this MagickImage image, double cornerRadius)
    {
        var (w, h) = (image.Width, image.Height);
        var r = cornerRadius * Math.Min(w, h);

        using var factor = new MagickImage(MagickColors.None, w, h)
            .Separate(Channels.Alpha)[0];
        _ = new Drawables().FillColor(MagickColors.White)
            .RoundRectangle(0, 0, w - 1, h - 1, r, r)
            .Draw(factor); // 内不透外透的圆角矩形
        factor.GaussianBlur(0.5, 0.5); // 抗锯齿

        image.Alpha(AlphaOption.Set);
        using var alpha = image.Separate(Channels.Alpha)[0];
        alpha.Composite(factor, CompositeOperator.Multiply);
        image.Composite(alpha, CompositeOperator.CopyAlpha);
    }

    /// <summary> 将图像贴在纯色背景上以实现边框 </summary>
    private static MagickImage AddBorder(
        this MagickImage image,
        MagickColor borderColor,
        (double Top, double Right, double Bottom, double Left) borderSize)
    {
        var (w, h) = (image.Width, image.Height);
        var x = (int)Math.Round(w * borderSize.Left);
        var y = (int)Math.Round(h * borderSize.Top);
        var w2 = (uint)Math.Round(w * (1 + borderSize.Left + borderSize.Right));
        var h2 = (uint)Math.Round(h * (1 + borderSize.Top + borderSize.Bottom));

        MagickImage result = new(borderColor, w2, h2);
        result.Composite(image, x, y, CompositeOperator.Over);

        return result;
    }

    /// <returns> 设置好的文字画笔、文字的上行高和宽高 </returns>
    private static (IDrawables<ushort>, double, double, double) GetPen(
        string info, string fontFamily, MagickColor textColor, double targetHeight)
    {
        var pen = new Drawables().EnableStrokeAntialias()
            .FillColor(textColor)
            .Font(fontFamily)
            .StrokeColor(textColor)
            .StrokeOpacity(new Percentage(50));
        var size = 24.0;
        for (var i = 0; i < 5; i++)
        {
            var m = pen.FontPointSize(size)
                .StrokeWidth(0.016 * size)
                .FontTypeMetrics(info)
                ?? throw new ArgumentException("无法测量文字尺寸");
            if (Math.Abs(m.TextHeight - targetHeight) > targetHeight * 0.0456)
                size *= targetHeight / m.TextHeight;
            else return (pen, m.Ascent, m.TextWidth, m.TextHeight);
        }
        throw new ArgumentException("5次尝试后仍无法找到合适的字号");
    }
}
