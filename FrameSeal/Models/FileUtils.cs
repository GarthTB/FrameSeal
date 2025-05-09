using Microsoft.Win32;
using System.IO;

namespace FrameSeal.Models;

/// <summary>
/// Utilities for file operations
/// </summary>
internal static class FileUtils
{
    /// <summary>
    /// Opens a file dialog to select one or multiple files
    /// </summary>
    internal static string[] GetFiles(string title, bool multiple)
    {
        OpenFileDialog dialog = new()
        {
            Title = title,
            Multiselect = multiple,
            Filter = "All files (*.*)|*.*|"
            + "JPEG files (*.jpg, *.jpeg)|*.jpg;*.jpeg|"
            + "PNG files (*.png)|*.png|"
            + "TIFF files (*.tif, *.tiff)|*.tif;*.tiff|"
            + "WebP files (*.webp)|*.webp"
        };
        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : [];
    }

    /// <summary>
    /// Parses input and output paths from a list of input paths and a format index
    /// </summary>
    internal static IEnumerable<(FileInfo, FileInfo)> ParseInputOutputPaths(
        this IEnumerable<string> inputPaths, int formatIndex)
    {
        var ext = formatIndex switch
        {
            0 => ".jpg",
            1 => ".png",
            2 => ".tif",
            3 => ".webp",
            _ => throw new ArgumentException("Invalid format index"),
        };

        HashSet<string> existingOutputPaths = [];
        bool Exists(string outputPath)
            => File.Exists(outputPath)
            || existingOutputPaths.Contains(outputPath);

        return inputPaths.Select(inputPath =>
        {
            var dir = Path.GetDirectoryName(inputPath)
                ?? throw new ArgumentException("Invalid input path.");
            var name = Path.GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(dir, $"{name}_FrameSeal{ext}");
            for (int i = 2; Exists(outputPath); i++)
                outputPath = Path.Combine(dir, $"{name}_FrameSeal_{i}{ext}");
            _ = existingOutputPaths.Add(outputPath);
            return (new FileInfo(inputPath), new FileInfo(outputPath));
        });
    }
}
