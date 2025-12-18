using ExifStrategy = System.Func<ImageMagick.IExifProfile?, string?>;

namespace FrameSeal.Models;

using ImageMagick;

/// <summary> 处理配置 </summary>
/// <param name="Icon"> 图标（禁用则为null） </param>
/// <param name="IconGap"> 图标与文字的间距（文字高度的倍数）(任意数) </param>
/// <param name="CornerRadius"> 圆角半径（图像短边长的倍数）[0, 0.5] </param>
/// <param name="BorderSize"> 边框尺寸（图像宽高的倍数）[0,) </param>
/// <param name="BorderColor"> 边框颜色 </param>
/// <param name="FontFamily"> 字体系列名称 </param>
/// <param name="TextHeight"> 文字高度（下边框高的倍数）(0,) </param>
/// <param name="TextColor"> 文字颜色 </param>
/// <param name="ExifStrategies"> 一系列用于提取特定EXIF元数据的方法 </param>
internal sealed record Settings(
    MagickImage? Icon,
    double IconGap,
    double CornerRadius,
    (double Top, double Right, double Bottom, double Left) BorderSize,
    MagickColor BorderColor,
    string FontFamily,
    double TextHeight,
    MagickColor TextColor,
    IEnumerable<ExifStrategy> ExifStrategies)
{
    /// <summary> 若配置无效则抛出 <see cref="ArgumentException"/> </summary>
    public void ThrowIfInvalid() {
        if (CornerRadius is < 0 or > 0.5)
            throw new ArgumentException("圆角半径超出 [0, 0.5] 范围", nameof(CornerRadius));
        if (BorderSize is not { Top: >= 0, Right: >= 0, Bottom: >= 0, Left: >= 0 })
            throw new ArgumentException("边框尺寸不能为负数", nameof(BorderSize));
        if (TextHeight <= 0)
            throw new ArgumentException("文字高度必须为正数", nameof(TextHeight));
    }
}
