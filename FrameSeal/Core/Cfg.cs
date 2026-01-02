namespace FrameSeal.Core;

using ImageMagick;

/// <summary> 处理配置 </summary>
/// <param name="ImgPaths"> 待处理图像的路径 </param>
/// <param name="IconPath"> 图标路径（null时禁用） </param>
/// <param name="IconGap"> 图标与文字的间距（文字高度的倍数） </param>
/// <param name="CornerRatio"> 圆角比例（图像短边长的倍数）[0, 0.5] </param>
/// <param name="BorderRatio"> 边框比例（图像宽高的倍数）[0,) </param>
/// <param name="BorderColor"> 边框颜色 </param>
/// <param name="FontName"> 字体名称 </param>
/// <param name="TextRatio"> 文字比例（下边框高的倍数）[0,) </param>
/// <param name="TextColor"> 文字颜色 </param>
/// <param name="InfoFuncs"> 将图像信息转换为字符串的方法 </param>
/// <param name="SaveAsync"> 保存图像的方法 </param>
internal sealed record Cfg(
    string[] ImgPaths,
    string? IconPath,
    double IconGap,
    double CornerRatio,
    (double T, double R, double B, double L) BorderRatio,
    string BorderColor,
    string FontName,
    double TextRatio,
    string TextColor,
    Func<IExifProfile?, string>[] InfoFuncs,
    Func<MagickImage, string, Task> SaveAsync)
{
    /// <summary> 检查配置，若无效则抛出异常 </summary>
    public void ThrowIfInvalid() {
        if (ImgPaths.Length == 0)
            throw new ArgumentException("没有待处理的图像", nameof(ImgPaths));
        if (CornerRatio is < 0 or > 0.5)
            throw new ArgumentOutOfRangeException(
                nameof(CornerRatio),
                $"圆角比例`{CornerRatio}`超出[0, 0.5]范围");
        if (BorderRatio is not { T: >= 0, R: >= 0, B: >= 0, L: >= 0 })
            throw new ArgumentOutOfRangeException(nameof(BorderRatio), $"边框比例`{BorderRatio}`包含负数");
        if (TextRatio < 0)
            throw new ArgumentOutOfRangeException(nameof(TextRatio), $"文字比例`{TextRatio}`小于0");
    }
}
