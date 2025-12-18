namespace FrameSeal.Services;

using ImageMagick;
using ImageMagick.Drawing;
using Models;

/// <summary> 核心处理方法 </summary>
internal static class CoreProcessor
{
    /// <returns> 设置好的文字画笔、文字的上行高和宽高 </returns>
    private static (IDrawables<ushort>, double, double, double) GetPen(
        string info,
        string fontFamily,
        MagickColor textColor,
        double targetHeight) {
        var pen = new Drawables().EnableStrokeAntialias()
            .FillColor(textColor)
            .Font(fontFamily)
            .StrokeColor(textColor)
            .StrokeOpacity(new(50));
        for (var (i, size) = (0, 14d); i < 5; i++) {
            var m = pen.FontPointSize(size).StrokeWidth(0.018 * size).FontTypeMetrics(info)
                 ?? throw new InvalidOperationException("无法测量文字尺寸");
            if (Math.Abs(m.TextHeight - targetHeight) > targetHeight * 0.0468)
                size *= targetHeight / m.TextHeight;
            else
                return (pen, m.Ascent, m.TextWidth, m.TextHeight);
        }
        throw new InvalidOperationException("5次尝试后仍无法找到合适的字号");
    }

    extension(MagickImage image)
    {
        /// <summary> 处理图像 </summary>
        public MagickImage Process(Settings settings, CancellationToken? ct) {
            settings.ThrowIfInvalid();

            ct?.ThrowIfCancellationRequested();
            if (settings.CornerRadius > 0)
                image.RoundCorner(settings.CornerRadius);
            else if (settings.BorderSize is { Top: 0, Right: 0, Bottom: 0, Left: 0 }) // 无圆角也无边框
                return image;

            ct?.ThrowIfCancellationRequested();
            var result = image.AddBorder(settings.BorderColor, settings.BorderSize);
            var exif = image.GetExifProfile();
            if (exif is {})
                result.SetProfile(exif); // 转移原EXIF
            if (settings.BorderSize.Bottom == 0) // 无下边框
                return result;

            ct?.ThrowIfCancellationRequested();
            var items = settings.ExifStrategies.Select(func => func(exif))
                .Where(static s => !string.IsNullOrWhiteSpace(s));
            var info = string.Join("  ", items);
            if (settings.Icon is null && string.IsNullOrWhiteSpace(info)) // 无图标也无文字
                return result;

            ct?.ThrowIfCancellationRequested();
            var bottomHeight = image.Height * settings.BorderSize.Bottom;
            var targetHeight = settings.TextHeight * bottomHeight;
            var (pen, ascent, textW, textH) = string.IsNullOrWhiteSpace(info)
                ? (null, 0, 0, targetHeight)
                : GetPen(info, settings.FontFamily, settings.TextColor, targetHeight);
            var iconY = Math.Round(result.Height - bottomHeight / 2 - textH / 2);
            var textX = result.Width / 2d - textW / 2;

            ct?.ThrowIfCancellationRequested();
            if (settings.Icon is {} icon) // 会修改textX
            {
                var h = Math.Round(textH);
                var w = Math.Round(icon.Width * h / icon.Height);
                using var mIcon = icon.CloneAndMutate(x => x.Resize(
                    (uint)w + 1,
                    (uint)h)); // 忽略宽度，只按高缩放

                var gapW = settings.IconGap * textH;
                var iconX = Math.Round(textX - gapW / 2 - mIcon.Width / 2d);
                result.Composite(mIcon, (int)iconX, (int)iconY, CompositeOperator.Over);

                textX += gapW / 2 + mIcon.Width / 2d;
            }

            ct?.ThrowIfCancellationRequested();
            _ = pen?.Text(textX, iconY + ascent, info).Draw(result);

            return result;
        }

        /// <summary> 将图像切去圆角 </summary>
        private void RoundCorner(double cornerRadius) {
            var (w, h) = (image.Width, image.Height);
            var r = cornerRadius * Math.Min(w, h);

            using var factor = new MagickImage(MagickColors.None, w, h).Separate(Channels.Alpha)[0];
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
        private MagickImage AddBorder(
            MagickColor borderColor,
            (double Top, double Right, double Bottom, double Left) borderSize) {
            var (w, h) = (image.Width, image.Height);
            var x = Math.Round(w * borderSize.Left);
            var y = Math.Round(h * borderSize.Top);
            var w2 = Math.Round(w * (1 + borderSize.Left + borderSize.Right));
            var h2 = Math.Round(h * (1 + borderSize.Top + borderSize.Bottom));

            MagickImage result = new(borderColor, (uint)w2, (uint)h2);
            result.Composite(image, (int)x, (int)y, CompositeOperator.Over);

            return result;
        }
    }
}
