namespace FrameSeal.Core;

using System.IO;
using ImageMagick;

/// <summary> 处理配置 </summary>
/// <param name="iconPath"> 图标路径（null时禁用） </param>
/// <param name="iconGap"> 图标与文字的间距（文字高度的倍数） </param>
/// <param name="cornerRatio"> 圆角比例（图像短边长的倍数）[0, 0.5] </param>
/// <param name="borderRatio"> 边框比例（图像宽高的倍数）[0,) </param>
/// <param name="borderColor"> 边框颜色字符串 </param>
/// <param name="fontName"> 字体名称 </param>
/// <param name="textRatio"> 文字比例（下边框高的倍数）[0,) </param>
/// <param name="textColor"> 文字颜色字符串 </param>
/// <param name="infoFuncs"> 将图像信息转换为字符串的方法 </param>
internal sealed class Config(
    string? iconPath,
    double iconGap,
    double cornerRatio,
    (double T, double R, double B, double L) borderRatio,
    string borderColor,
    string fontName,
    double textRatio,
    string textColor,
    Func<IExifProfile?, string>[] infoFuncs)
{
    /// <summary> 边框比例（图像宽高的倍数）[0,) </summary>
    private readonly (double T, double R, double B, double L) _borderRatio
        = borderRatio is not { T: >= 0, R: >= 0, B: >= 0, L: >= 0 } or { T: 0, R: 0, B: 0, L: 0 }
            ? throw new ArgumentOutOfRangeException(
                nameof(borderRatio),
                $"边框比例`{borderRatio}`包含负数或全为0")
            : borderRatio;

    /// <summary> 圆角比例（图像短边长的倍数）[0, 0.5] </summary>
    private readonly double _cornerRatio = cornerRatio is < 0 or > 0.5
        ? throw new ArgumentOutOfRangeException(
            nameof(cornerRatio),
            $"圆角比例`{cornerRatio}`超出[0, 0.5]范围")
        : cornerRatio;

    /// <summary> 图标路径（null时禁用） </summary>
    private readonly string? _iconPath = iconPath;

    /// <summary> 文字比例（下边框高的倍数）[0,) </summary>
    private readonly double _textRatio = textRatio < 0
        ? throw new ArgumentOutOfRangeException(nameof(textRatio), $"文字比例`{textRatio}`小于0")
        : textRatio;

    /// <summary> 边框颜色 </summary>
    public readonly MagickColor BorderColor = ParseColorOrRed(borderColor);

    /// <summary> 字体名称 </summary>
    public readonly string FontName = fontName;

    /// <summary> 图标与文字的间距（文字高度的倍数） </summary>
    public readonly double IconGap = iconGap;

    /// <summary> 将图像信息转换为字符串的方法 </summary>
    public readonly Func<IExifProfile?, string>[] InfoFuncs = infoFuncs;

    /// <summary> 文字颜色 </summary>
    public readonly MagickColor TextColor = ParseColorOrRed(textColor);

    /// <summary> 载入图标 </summary>
    public MagickImage? Icon =>
        File.Exists(_iconPath)
            ? new(_iconPath)
            : null;

    /// <summary> 获取圆角半径（像素） </summary>
    /// <param name="minSide"> 图像的短边长（像素） </param>
    public double GetCornerRadius(double minSide) => _cornerRatio * minSide;

    /// <summary> 获取边框宽度（像素） </summary>
    /// <param name="w"> 图像宽度 </param>
    /// <param name="h"> 图像高度 </param>
    public (uint T, uint R, uint B, uint L) GetBorderWidth(uint w, uint h) =>
        ((uint)Math.Round(_borderRatio.T * h), (uint)Math.Round(_borderRatio.R * w),
            (uint)Math.Round(_borderRatio.B * h), (uint)Math.Round(_borderRatio.L * w));

    /// <summary> 获取文字高度（像素） </summary>
    /// <param name="bottom"> 下边框高度（像素） </param>
    public double GetTextTargetH(double bottom) => _textRatio * bottom;

    /// <summary> 解析颜色字符串，无效则返回红色 </summary>
    /// <param name="colorStr"> 解析颜色字符串 </param>
    private static MagickColor ParseColorOrRed(string colorStr) {
        try {
            return new(colorStr);
        } catch {
            return MagickColors.Red;
        }
    }
}
