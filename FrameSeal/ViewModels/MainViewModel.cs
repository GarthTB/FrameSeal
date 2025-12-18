// ReSharper disable UnusedParameterInPartialMethod

namespace FrameSeal.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using Microsoft.Win32;
using Models;
using Services;
using ExifStrategy = Func<ImageMagick.IExifProfile?, string?>;
using SaveStrategy = Action<ImageMagick.MagickImage, string>;

internal sealed partial class MainViewModel: ObservableObject
{
    #region 辅助方法

    /// <summary> 执行一个Action，若有异常则弹窗提示 </summary>
    private static void TryOrShow(string actionName, Action action) {
        try {
            action();
        } catch (Exception ex) {
            var msg = string.IsNullOrWhiteSpace(ex.Message)
                ? "无详细错误信息"
                : string.IsNullOrWhiteSpace(ex.StackTrace)
                    ? ex.Message
                    : $"{ex.Message}\n\n栈跟踪：\n\n{ex.StackTrace}";
            _ = MessageBox.Show(
                $"{actionName} 出错：\n{msg}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <returns> 字符串指定的特定范围的double </returns>
    private static double ParseDoubleAndClamp(
        string s,
        double defaultValue,
        double min = double.MinValue,
        double max = double.MaxValue) =>
        double.TryParse(s.Trim(), out var result)
            ? Math.Clamp(result, min, max)
            : defaultValue;

    /// <returns> 字符串指定的MagickColor，无效则为红色 </returns>
    private static MagickColor ParseColorOrRed(string s) {
        try {
            return new(s.Trim());
        } catch {
            return MagickColors.Red;
        }
    }

    #endregion

    #region 待处理图像路径

    /// <summary> 待处理图像的路径 </summary>
    public ObservableCollection<string> ImagePaths { get; } = [];

    /// <summary> 选中的待处理图像路径的索引 </summary>
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RemovePathCommand))]
    private int _selectedPathIndex = -1;

    partial void OnSelectedPathIndexChanged(int value) => _ = UpdatePreviewAsync();

    /// <returns> 是否选中了待处理图像路径 </returns>
    private bool HasSelectedPath => SelectedPathIndex >= 0 && SelectedPathIndex < ImagePaths.Count;

    /// <summary> 添加待处理图像路径 </summary>
    [RelayCommand]
    private void AddPath() =>
        TryOrShow(
            "添加图像",
            () => {
                OpenFileDialog dialog = new() {
                    Title = "添加待处理的图像",
                    Filter = "图像文件 (*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp)|"
                           + "*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp|"
                           + "所有文件 (*.*)|*.*",
                    Multiselect = true
                };
                if (dialog.ShowDialog() == true)
                    foreach (var path in dialog.FileNames.Where(path => !ImagePaths.Contains(path)))
                        ImagePaths.Add(path);
                RunCommand.NotifyCanExecuteChanged();
            });

    /// <summary> 移除选中的待处理图像路径 </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedPath))]
    private void RemovePath() =>
        TryOrShow(
            "移除图像",
            () => {
                ImagePaths.RemoveAt(SelectedPathIndex);
                SelectedPathIndex = -1; // 避免连续移除
                RunCommand.NotifyCanExecuteChanged();
            });

    #endregion

    #region 选取嵌入图标

    /// <summary> 是否嵌入图标 </summary>
    [ObservableProperty]
    private bool _useIcon;

    partial void OnUseIconChanged(bool value) {
        if (value && string.IsNullOrWhiteSpace(IconPath))
            SelectIconCommand.Execute(null);
        SelectIconCommand.NotifyCanExecuteChanged();
        _ = UpdatePreviewAsync();
    }

    /// <summary> 嵌入的图标 </summary>
    [ObservableProperty]
    private MagickImage? _icon;

    // ReSharper disable once SuggestBaseTypeForParameter
    partial void OnIconChanged(MagickImage? value) {
        UseIcon = value is {};
        OnPropertyChanged(nameof(IconPath));
        _ = UpdatePreviewAsync();
    }

    /// <summary> 嵌入图标路径 </summary>
    public string IconPath => Icon?.FileName ?? "";

    /// <summary> 选取要嵌入的图标 </summary>
    [RelayCommand(CanExecute = nameof(UseIcon))]
    private void SelectIcon() =>
        TryOrShow(
            "选取图标",
            () => {
                OpenFileDialog dialog = new() {
                    Title = "选择要嵌入的图标",
                    Filter = "图像文件 (*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp)|"
                           + "*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp|"
                           + "所有文件 (*.*)|*.*"
                };
                if (dialog.ShowDialog() != true)
                    return;

                try {
                    Icon = new(dialog.FileName);
                } catch {
                    Icon = null;
                }
            });

    #endregion

    #region 文本型配置

    /// <summary> 图标与文字的间距（文字高度的倍数）(任意数) </summary>
    [ObservableProperty]
    private string _iconGap = "1.2";

    partial void OnIconGapChanged(string value) => _ = UpdatePreviewAsync();

    /// <returns> 图标与文字的间距值 </returns>
    private double IconGapValue => ParseDoubleAndClamp(IconGap, 1);

    /// <summary> 圆角半径（图像短边长的倍数）[0, 0.5] </summary>
    [ObservableProperty]
    private string _cornerRadius = "0.025";

    partial void OnCornerRadiusChanged(string value) => _ = UpdatePreviewAsync();

    /// <returns> 圆角半径值 </returns>
    private double CornerRadiusValue => ParseDoubleAndClamp(CornerRadius, 0.025, 0, 0.5);

    /// <summary> 左边框尺寸（图像宽的倍数）[0,) </summary>
    [ObservableProperty]
    private string _borderLeft = "0.02";

    partial void OnBorderLeftChanged(string value) => _ = UpdatePreviewAsync();

    /// <summary> 右边框尺寸（图像宽的倍数）[0,) </summary>
    [ObservableProperty]
    private string _borderRight = "0.02";

    partial void OnBorderRightChanged(string value) => _ = UpdatePreviewAsync();

    /// <summary> 上边框尺寸（图像高的倍数）[0,) </summary>
    [ObservableProperty]
    private string _borderTop = "0.03";

    partial void OnBorderTopChanged(string value) => _ = UpdatePreviewAsync();

    /// <summary> 下边框尺寸（图像高的倍数）[0,) </summary>
    [ObservableProperty]
    private string _borderBottom = "0.06";

    partial void OnBorderBottomChanged(string value) => _ = UpdatePreviewAsync();

    /// <returns> 边框尺寸值 </returns>
    private (double, double, double, double) BorderSize =>
        (ParseDoubleAndClamp(BorderTop, 0.03, 0), ParseDoubleAndClamp(BorderRight, 0.02, 0),
            ParseDoubleAndClamp(BorderBottom, 0.06, 0), ParseDoubleAndClamp(BorderLeft, 0.02, 0));

    /// <summary> 边框颜色 </summary>
    [ObservableProperty]
    private string _borderColor = "#080808FF";

    partial void OnBorderColorChanged(string value) => _ = UpdatePreviewAsync();

    /// <returns> 边框颜色值 </returns>
    private MagickColor BorderColorValue => ParseColorOrRed(BorderColor);

    /// <summary> 文字高度（下边框高的倍数）(0,) </summary>
    [ObservableProperty]
    private string _textHeight = "0.36";

    partial void OnTextHeightChanged(string value) => _ = UpdatePreviewAsync();

    /// <returns> 文字高度值 </returns>
    private double TextHeightValue =>
        ParseDoubleAndClamp(TextHeight, 0.36, 0) is var height and > 0
            ? height
            : 0.36;

    /// <summary> 文字颜色 </summary>
    [ObservableProperty]
    private string _textColor = "#C0A208FF";

    partial void OnTextColorChanged(string value) => _ = UpdatePreviewAsync();

    /// <returns> 文字颜色值 </returns>
    private MagickColor TextColorValue => ParseColorOrRed(TextColor);

    #endregion

    #region 选项型配置

    /// <summary> 所有可用的保存格式 </summary>
    public string[] SaveFormats { get; } = SaveStrategyManager.SaveFormats;

    /// <summary> 选中的保存格式的索引 </summary>
    [ObservableProperty]
    private byte _selectedFormatIndex = 1;

    /// <returns> 选中的保存格式对应的保存方法 </returns>
    private SaveStrategy Save =>
        SelectedFormatIndex < SaveFormats.Length
            ? SaveStrategyManager.GetSaveStrategy(SaveFormats[SelectedFormatIndex])
            : throw new InvalidOperationException("保存格式的索引错误");

    /// <summary> 字体系列名称 </summary>
    public string[] FontFamilies { get; }
        = [.. Fonts.SystemFontFamilies.Select(static f => f.Source).Order()];

    /// <summary> 选中的字体系列的索引 </summary>
    [ObservableProperty]
    private byte _selectedFontIndex;

    partial void OnSelectedFontIndexChanged(byte value) => _ = UpdatePreviewAsync();

    /// <returns> 选中的字体系列名称 </returns>
    private string FontFamily =>
        SelectedFontIndex < FontFamilies.Length
            ? FontFamilies[SelectedFontIndex]
            : throw new InvalidOperationException("字体系列的索引错误");

    #endregion

    #region 复合型配置

    /// <summary> 所有可用的EXIF元数据项 </summary>
    public string[] ExifKeys { get; } = ExifStrategyManager.ExifKeys;

    /// <returns> 选中的EXIF提取方法 </returns>
    private ExifStrategy GetExifStrategy(byte index, string manualText) =>
        index < ExifKeys.Length
            ? ExifStrategyManager.GetExifStrategy(ExifKeys[index], manualText)
            : throw new InvalidOperationException("EXIF提取方法的索引错误");

    /// <summary> 选中的EXIF元数据项的索引1 </summary>
    [ObservableProperty]
    private byte _selectedExifIndex1;

    partial void OnSelectedExifIndex1Changed(byte value) => _ = UpdatePreviewAsync();

    /// <summary> 手动输入的文本1 </summary>
    [ObservableProperty]
    private string _exifText1 = "";

    partial void OnExifText1Changed(string value) => _ = UpdatePreviewAsync();

    /// <summary> 选中的EXIF元数据项的索引2 </summary>
    [ObservableProperty]
    private byte _selectedExifIndex2;

    partial void OnSelectedExifIndex2Changed(byte value) => _ = UpdatePreviewAsync();

    /// <summary> 手动输入的文本2 </summary>
    [ObservableProperty]
    private string _exifText2 = "";

    partial void OnExifText2Changed(string value) => _ = UpdatePreviewAsync();

    /// <summary> 选中的EXIF元数据项的索引3 </summary>
    [ObservableProperty]
    private byte _selectedExifIndex3;

    partial void OnSelectedExifIndex3Changed(byte value) => _ = UpdatePreviewAsync();

    /// <summary> 手动输入的文本3 </summary>
    [ObservableProperty]
    private string _exifText3 = "";

    partial void OnExifText3Changed(string value) => _ = UpdatePreviewAsync();

    /// <summary> 选中的EXIF元数据项的索引4 </summary>
    [ObservableProperty]
    private byte _selectedExifIndex4;

    partial void OnSelectedExifIndex4Changed(byte value) => _ = UpdatePreviewAsync();

    /// <summary> 手动输入的文本4 </summary>
    [ObservableProperty]
    private string _exifText4 = "";

    partial void OnExifText4Changed(string value) => _ = UpdatePreviewAsync();

    /// <summary> 选中的EXIF元数据项的索引5 </summary>
    [ObservableProperty]
    private byte _selectedExifIndex5;

    partial void OnSelectedExifIndex5Changed(byte value) => _ = UpdatePreviewAsync();

    /// <summary> 手动输入的文本5 </summary>
    [ObservableProperty]
    private string _exifText5 = "";

    partial void OnExifText5Changed(string value) => _ = UpdatePreviewAsync();

    /// <returns> 选中的各项EXIF提取方法 </returns>
    private IEnumerable<ExifStrategy> ExifStrategies => [
        GetExifStrategy(SelectedExifIndex1, ExifText1),
        GetExifStrategy(SelectedExifIndex2, ExifText2),
        GetExifStrategy(SelectedExifIndex3, ExifText3),
        GetExifStrategy(SelectedExifIndex4, ExifText4),
        GetExifStrategy(SelectedExifIndex5, ExifText5)
    ];

    #endregion

    #region 实时预览

    /// <summary> 是否实时预览 </summary>
    [ObservableProperty]
    private bool _enablePreview = true;

    partial void OnEnablePreviewChanged(bool value) => _ = UpdatePreviewAsync();

    /// <summary> 预览图像 </summary>
    [ObservableProperty]
    private BitmapImage? _previewImage;

    /// <summary> 预览MagickImage图像 </summary>
    private void Display(MagickImage image) {
        using MemoryStream ms = new();
        image.Write(ms, MagickFormat.Tiff);
        ms.Position = 0;

        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();

        PreviewImage = bitmap;
    }

    /// <summary> 预览的CancellationTokenSource </summary>
    private CancellationTokenSource? _previewCts;

    /// <summary> 刷新实时预览 </summary>
    private async Task UpdatePreviewAsync() {
        try {
            if (_previewCts is {}) {
                await _previewCts.CancelAsync();
                _previewCts.Dispose();
            }
            if (HasSelectedPath && EnablePreview) {
                _previewCts = new();
                var token = _previewCts.Token;
                await Task.Delay(160, token).ConfigureAwait(true); // 防抖
                await Task.Run(
                        () => {
                            using MagickImage image = new(ImagePaths[SelectedPathIndex]);
                            token.ThrowIfCancellationRequested();
                            var (w, h) = (image.Width, image.Height);
                            image.Resize(Math.Min(w, 1024), Math.Min(h, 1024));
                            token.ThrowIfCancellationRequested();
                            using var result = image.Process(Settings, token);
                            token.ThrowIfCancellationRequested();
                            Display(result);
                        },
                        token)
                    .ConfigureAwait(true);
            } else
                (PreviewImage, _previewCts) = (null, null);
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            var msg = string.IsNullOrWhiteSpace(ex.Message)
                ? "无详细错误信息"
                : string.IsNullOrWhiteSpace(ex.StackTrace)
                    ? ex.Message
                    : $"{ex.Message}\n\n栈跟踪：\n\n{ex.StackTrace}";
            _ = MessageBox.Show(
                $"刷新实时预览 出错：\n{msg}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #endregion

    #region 执行处理

    /// <returns> 当前的处理配置 </returns>
    private Settings Settings =>
        new(
            UseIcon
                ? Icon
                : null,
            IconGapValue,
            CornerRadiusValue,
            BorderSize,
            BorderColorValue,
            FontFamily,
            TextHeightValue,
            TextColorValue,
            ExifStrategies);

    /// <summary> 是否可以执行处理 </summary>
    private bool CanRun => ImagePaths.Count > 0;

    /// <summary> 打断预览并执行处理（同步） </summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run() =>
        TryOrShow(
            "执行处理",
            () => {
                _previewCts?.Cancel();
                _previewCts?.Dispose();
                _previewCts = null;
                var save = Save;
                var settings = Settings;
                List<string> issues = ["以下文件处理失败：\n\n"];

                foreach (var path in ImagePaths)
                    try {
                        using MagickImage image = new(path);
                        save(image.Process(settings, null), path);
                    } catch (Exception ex) {
                        issues.Add($"{path}：\n{ex.Message}");
                    }

                if (issues.Count == 1)
                    _ = MessageBox.Show(
                        "所有图像处理完成！",
                        "成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                else
                    throw new AggregateException(string.Join("\n\n", issues));
            });

    #endregion
}
