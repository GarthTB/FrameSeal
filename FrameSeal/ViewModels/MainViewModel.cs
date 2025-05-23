using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameSeal.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FrameSeal.ViewModels;

/// <summary>
/// View model for MainWindow
/// </summary>
public partial class MainViewModel : ObservableObject
{
    #region 绑定属性

    [ObservableProperty]
    private string _title = "Frame Seal 1.1.0 - 作者：Garth TB";

    public ObservableCollection<string> ImagePaths { get; } = [];

    [ObservableProperty]
    private int _selectedImagePathIndex = -1;

    partial void OnSelectedImagePathIndexChanged(int value)
    {
        RemoveImagesCommand.NotifyCanExecuteChanged();
        UpdatePreviewAsync();
    }

    [ObservableProperty]
    private bool _useIcon = false;

    partial void OnUseIconChanged(bool value)
    {
        if (value && string.IsNullOrEmpty(IconPath))
            ChooseIcon();
        else UpdatePreviewAsync();
    }

    [ObservableProperty]
    private string _iconPath = "";

    partial void OnIconPathChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _iconMargin = "0.8";

    partial void OnIconMarginChanged(string value) => UpdatePreviewAsync();

    public ObservableCollection<string> OutputFormats { get; } =
        ["JPEG, 质量 98", "PNG", "TIFF", "WebP"];

    [ObservableProperty]
    private int _selectedOutputFormatIndex = 0;

    public ObservableCollection<string> FontNames { get; private set; } = [];

    [ObservableProperty]
    private int _selectedFontNameIndex = 0;

    partial void OnSelectedFontNameIndexChanged(int value) => UpdatePreviewAsync();

    [ObservableProperty]
    private bool _previewEnabled = true;

    partial void OnPreviewEnabledChanged(bool value)
    {
        if (_tempPreviewImage is null)
            UpdatePreviewAsync();
        else PreviewImage = value && SelectedImagePathIndex >= 0
            ? _tempPreviewImage
            : null;
    }

    [ObservableProperty]
    private BitmapImage? _previewImage;

    private BitmapImage? _tempPreviewImage;

    [ObservableProperty]
    private string _cornerRatio = "0.04";

    partial void OnCornerRatioChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _borderTop = "0.06";

    partial void OnBorderTopChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _borderBottom = "0.1";

    partial void OnBorderBottomChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _borderLeft = "0.04";

    partial void OnBorderLeftChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _borderRight = "0.04";

    partial void OnBorderRightChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _borderColor = "101010FF";

    partial void OnBorderColorChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _textColor = "F0D010FF";

    partial void OnTextColorChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _textRatio = "0.3";

    partial void OnTextRatioChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _defaultFocalLength = "35 mm";

    partial void OnDefaultFocalLengthChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _defaultExposureTime = "1/50 s";

    partial void OnDefaultExposureTimeChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _defaultAperture = "f/2.8";

    partial void OnDefaultApertureChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _defaultISO = "ISO 100";

    partial void OnDefaultISOChanged(string value) => UpdatePreviewAsync();

    [ObservableProperty]
    private string _defaultCreationTime = "2025-01-01 00:00:00";

    partial void OnDefaultCreationTimeChanged(string value) => UpdatePreviewAsync();

    #endregion

    public MainViewModel()
    {
        var fontNames = Fonts.SystemFontFamilies
             .Select(f => f.Source)
             .Order();
        foreach (var name in fontNames)
            FontNames.Add(name);
    }

    #region 绑定方法

    [RelayCommand]
    private void AddImages()
        => AddImages(FileUtils.GetFiles("请选择图片文件", true));

    public void AddImages(string[] paths)
    {
        var newPaths = paths.Where(path => !ImagePaths.Contains(path));
        foreach (var path in newPaths)
            ImagePaths.Add(path);
        RunCommand.NotifyCanExecuteChanged();
    }

    private bool CanRemoveImages() => SelectedImagePathIndex >= 0;

    [RelayCommand(CanExecute = nameof(CanRemoveImages))]
    private void RemoveImages()
    {
        var i = SelectedImagePathIndex;
        ImagePaths.RemoveAt(SelectedImagePathIndex);
        SelectedImagePathIndex = Math.Min(i, ImagePaths.Count - 1);
        RunCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ChooseIcon()
    {
        var files = FileUtils.GetFiles("请选择图标文件", false);
        if (files.Length > 0)
            IconPath = files[0];
    }

    private bool CanRun() => ImagePaths.Count > 0;

    #endregion

    #region 处理图片

    private CancellationTokenSource? _previewCts;

    private async void UpdatePreviewAsync()
    {
        _previewCts?.Cancel();
        _previewCts = new();
        try
        {
            _tempPreviewImage = PreviewImage =
                PreviewEnabled && SelectedImagePathIndex >= 0
                ? await Task.Run(() =>
                {
                    Title = "Frame Seal 1.1.0 - 作者：Garth TB - 生成预览...";
                    var image = Core.ProcessPreview(GetSettings(
                        [ImagePaths[SelectedImagePathIndex]]));
                    Title = "Frame Seal 1.1.0 - 作者：Garth TB";
                    return image;
                }).ConfigureAwait(true)
                : null;
        }
        catch (TaskCanceledException) { } // ignore cancellation
        catch (Exception ex)
        {
            _ = MessageBox.Show(
                $"生成预览失败：\n{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run()
    {
        _previewCts?.Cancel();
        Title = "Frame Seal 1.1.0 - 作者：Garth TB - 正在处理...";
        var settings = GetSettings(ImagePaths);
        var count = Core.ProcessImages(settings);
        Title = "Frame Seal 1.1.0 - 作者：Garth TB";
        _ = count == ImagePaths.Count
            ? MessageBox.Show(
                $"{count} 张图片全部处理完成。",
                "完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information)
            : MessageBox.Show(
                $"{count} 张图片处理完成，{ImagePaths.Count - count} 张照片未能处理。",
                "未全部处理",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
    }

    private SettingsModel GetSettings(IEnumerable<string> inputPaths)
        => new(inputPaths,
            UseIcon,
            IconPath,
            IconMargin,
            SelectedOutputFormatIndex,
            CornerRatio,
            BorderTop,
            BorderBottom,
            BorderLeft,
            BorderRight,
            BorderColor,
            FontNames[SelectedFontNameIndex],
            TextColor,
            TextRatio,
            DefaultFocalLength,
            DefaultExposureTime,
            DefaultAperture,
            DefaultISO,
            DefaultCreationTime);

    #endregion
}
