using ImageMagick;
using System.IO;

namespace FrameSeal.Models;

internal class SettingsModel(
    IEnumerable<string> imagePaths,
    bool useIcon,
    string iconPath,
    string iconMargin,
    int formatIndex,
    string cornerRatio,
    string borderTop,
    string borderBottom,
    string borderLeft,
    string borderRight,
    string borderColor,
    string fontName,
    string textColor,
    string textRatio,
    string defaultFocalLength,
    string defaultExposureTime,
    string defaultAperture,
    string defaultISO,
    string defaultCreationTime)
{
    /// <summary>
    /// Input and output paths for each image
    /// </summary>
    internal IEnumerable<(FileInfo, FileInfo)> InputOutputPaths { get; } =
        imagePaths.Where(File.Exists)
            .ParseInputOutputPaths(formatIndex);

    /// <summary>
    /// Icon path, or null if not used
    /// </summary>
    internal MagickImage? Icon { get; } = LoadIcon(iconPath, useIcon);

    /// <summary>
    /// Parse an image from a path, or null if not used
    /// </summary>
    private static MagickImage? LoadIcon(string iconPath, bool useIcon)
    {
        try
        {
            return useIcon && File.Exists(iconPath)
                ? new(iconPath)
                : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// The ratio of the margin between the icon and the text to the text height
    /// </summary>
    internal double IconMargin { get; } =
        iconMargin.ClampOrDefault(double.MinValue, double.MaxValue, 0.8);

    /// <summary>
    /// The ratio of the rounded corner radius to the shorter side of the image
    /// </summary>
    internal double CornerRatio { get; } =
        cornerRatio.ClampOrDefault(0, 0.5, 0.04);

    /// <summary>
    /// The ratio of the top border's height to the original image height
    /// </summary>
    internal double BorderTop { get; } =
        borderTop.ClampOrDefault(0, 65535, 0.06);

    /// <summary>
    /// The ratio of the bottom border's height to the original image height
    /// </summary>
    internal double BorderBottom { get; } =
        borderBottom.ClampOrDefault(0, 65535, 0.1);

    /// <summary>
    /// The ratio of the left border's width to the original image width
    /// </summary>
    internal double BorderLeft { get; } =
        borderLeft.ClampOrDefault(0, 65535, 0.04);

    /// <summary>
    /// The ratio of the right border's width to the original image width
    /// </summary>
    internal double BorderRight { get; } =
        borderRight.ClampOrDefault(0, 65535, 0.04);

    /// <summary>
    /// The ratio of the text height to the bottom border's height
    /// </summary>
    internal double TextRatio { get; } = textRatio.ClampOrDefault(0, 1, 0.3);

    /// <summary>
    /// The color of the border
    /// </summary>
    internal MagickColor BorderColor { get; } = borderColor.ParseColorOrRed();

    /// <summary>
    /// The name of the font family to use
    /// </summary>
    internal string FontName { get; } = fontName;

    /// <summary>
    /// The color of the text
    /// </summary>
    internal MagickColor TextColor { get; } = textColor.ParseColorOrRed();

    /// <summary>
    /// The default focal length of the camera
    /// </summary>
    internal string DefaultFocalLength { get; } = defaultFocalLength;

    /// <summary>
    /// The default exposure time of the camera
    /// </summary>
    internal string DefaultExposureTime { get; } = defaultExposureTime;

    /// <summary>
    /// The default aperture of the camera
    /// </summary>
    internal string DefaultAperture { get; } = defaultAperture;

    /// <summary>
    /// The default ISO of the camera
    /// </summary>
    internal string DefaultISO { get; } = defaultISO;

    /// <summary>
    /// The default creation time of the image
    /// </summary>
    internal string DefaultCreationTime { get; } = defaultCreationTime;
}
