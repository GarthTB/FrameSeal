// ReSharper disable UnusedParameterInPartialMethod

namespace FrameSeal.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core;
using ImageMagick;
using Microsoft.Win32;
using static Utils;

/// <summary> 主窗口的视图模型 </summary>
internal sealed partial class MainViewModel: ObservableObject
{
    #region 配置预处理

    /// <summary> 当前的配置 </summary>
    private Config CurConfig =>
        new(
            IconPath,
            double.Parse(IconGap),
            double.Parse(CornerRatio),
            (double.Parse(BorderT), double.Parse(BorderR), double.Parse(BorderB),
                double.Parse(BorderL)),
            BorderColor,
            FontNames[FontNameIdx],
            double.Parse(TextRatio),
            TextColor,
            [
                InfoFuncs.Get(InfoKeys[InfoKeyIdx1], InfoText1),
                InfoFuncs.Get(InfoKeys[InfoKeyIdx2], InfoText2),
                InfoFuncs.Get(InfoKeys[InfoKeyIdx3], InfoText3),
                InfoFuncs.Get(InfoKeys[InfoKeyIdx4], InfoText4),
                InfoFuncs.Get(InfoKeys[InfoKeyIdx5], InfoText5)
            ]);

    #endregion 配置预处理

    # region 添加或移除图像

    /// <summary> 图像路径 </summary>
    public ObservableCollection<string> ImgPaths { get; } = [];

    /// <summary> 选中图像路径的索引 </summary>
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RemovePathCommand))]
    private int _imgPathIdx = -1;

    partial void OnImgPathIdxChanged(int value) => _ = UpdatePreviewAsync(true);

    /// <summary> 是否选中了图像路径 </summary>
    private bool PathSelected => ImgPathIdx >= 0 && ImgPathIdx < ImgPaths.Count;

    /// <summary> 添加待处理图像 </summary>
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
                if (dialog.ShowDialog() != true
                 || dialog.FileNames.Where(path => File.Exists(path) && !ImgPaths.Contains(path))
                        .ToArray() is not { Length: > 0 } newPaths)
                    return;
                foreach (var path in newPaths)
                    ImgPaths.Add(path);
                RunCommand.NotifyCanExecuteChanged();
            });

    /// <summary> 移除选中的图像路径 </summary>
    [RelayCommand(CanExecute = nameof(PathSelected))]
    private void RemovePath() =>
        TryOrShow(
            "移除图像",
            () => {
                ImgPaths.RemoveAt(ImgPathIdx);
                ImgPathIdx = -1; // 避免连续移除
                RunCommand.NotifyCanExecuteChanged();
            });

    # endregion 添加或移除图像

    #region 启用或禁用图标

    /// <summary> 是否嵌入图标 </summary>
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(PickIconCommand))]
    private bool _useIcon;

    partial void OnUseIconChanged(bool value) {
        if (value && string.IsNullOrWhiteSpace(IconPath))
            PickIconCommand.Execute(null);
        _ = UpdatePreviewAsync();
    }

    /// <summary> 图标路径（null时禁用） </summary>
    [ObservableProperty]
    private string? _iconPath;

    partial void OnIconPathChanged(string? value) => _ = UpdatePreviewAsync();

    /// <summary> 选取要嵌入的图标 </summary>
    [RelayCommand(CanExecute = nameof(UseIcon))]
    private void PickIcon() =>
        TryOrShow(
            "选取图标",
            () => {
                OpenFileDialog dialog = new() {
                    Title = "选取要嵌入的图标",
                    Filter = "图像文件 (*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp)|"
                           + "*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp|"
                           + "所有文件 (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true && File.Exists(dialog.FileName))
                    IconPath = dialog.FileName;
            });

    #endregion 启用或禁用图标

    #region 文本框配置

    /// <summary> 图标与文字的间距（文字高度的倍数） </summary>
    [ObservableProperty]
    private string _iconGap = "1.25";

    /// <summary> 圆角比例（图像短边长的倍数）[0, 0.5] </summary>
    [ObservableProperty]
    private string _cornerRatio = "0.025";

    /// <summary> 边框比例（图像宽高的倍数）[0,) </summary>
    [ObservableProperty]
    private string _borderL = "0.02", _borderR = "0.02", _borderT = "0.03", _borderB = "0.06";

    /// <summary> 边框颜色字符串 </summary>
    [ObservableProperty]
    private string _borderColor = "#080808";

    /// <summary> 文字比例（下边框高的倍数）[0,) </summary>
    [ObservableProperty]
    private string _textRatio = "0.33";

    /// <summary> 文字颜色字符串 </summary>
    [ObservableProperty]
    private string _textColor = "#D0A010";

    partial void OnIconGapChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnCornerRatioChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnBorderLChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnBorderRChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnBorderTChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnBorderBChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnBorderColorChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnTextRatioChanged(string value) => _ = UpdatePreviewAsync();
    partial void OnTextColorChanged(string value) => _ = UpdatePreviewAsync();

    #endregion 文本框配置

    #region 复选框配置

    /// <summary> 所有可用的保存格式 </summary>
    public IReadOnlyList<string> SaveFormats { get; } = SaveActions.Keys;

    /// <summary> 选中保存格式的索引 </summary>
    public byte SaveFormatIdx { get; set; } = 4;

    /// <summary> 所有可用的字体系列 </summary>
    public IReadOnlyList<string> FontNames { get; } = MagickNET.FontFamilies;

    /// <summary> 选中字体系列的索引 </summary>
    [ObservableProperty]
    private byte _fontNameIdx;

    partial void OnFontNameIdxChanged(byte value) => _ = UpdatePreviewAsync();

    #endregion 复选框配置

    #region 信息配置

    /// <summary> 所有可用的图像信息 </summary>
    public IReadOnlyList<string> InfoKeys { get; } = InfoFuncs.Keys;

    /// <summary> 选中图像信息的索引 </summary>
    [ObservableProperty]
    private byte _infoKeyIdx1, _infoKeyIdx2, _infoKeyIdx3, _infoKeyIdx4, _infoKeyIdx5;

    /// <summary> 手动输入的文本 </summary>
    [ObservableProperty]
    private string _infoText1 = "",
        _infoText2 = "",
        _infoText3 = "",
        _infoText4 = "",
        _infoText5 = "";

    partial void OnInfoKeyIdx1Changed(byte value) => _ = UpdatePreviewAsync();
    partial void OnInfoKeyIdx2Changed(byte value) => _ = UpdatePreviewAsync();
    partial void OnInfoKeyIdx3Changed(byte value) => _ = UpdatePreviewAsync();
    partial void OnInfoKeyIdx4Changed(byte value) => _ = UpdatePreviewAsync();
    partial void OnInfoKeyIdx5Changed(byte value) => _ = UpdatePreviewAsync();
    partial void OnInfoText1Changed(string value) => _ = UpdatePreviewAsync();
    partial void OnInfoText2Changed(string value) => _ = UpdatePreviewAsync();
    partial void OnInfoText3Changed(string value) => _ = UpdatePreviewAsync();
    partial void OnInfoText4Changed(string value) => _ = UpdatePreviewAsync();
    partial void OnInfoText5Changed(string value) => _ = UpdatePreviewAsync();

    #endregion 信息配置

    #region 预览

    /// <summary> 原始图像的缩略图 </summary>
    private MagickImage? _thumb;

    /// <summary> 预览图像 </summary>
    [ObservableProperty]
    private BitmapImage? _previewImg;

    /// <summary> 是否启用预览 </summary>
    [ObservableProperty]
    private bool _enablePreview = true;

    partial void OnEnablePreviewChanged(bool value) => _ = UpdatePreviewAsync();

    /// <summary> 预览的取消令牌源 </summary>
    private CancellationTokenSource _cts = new();

    /// <summary> 打断并刷新预览 </summary>
    /// <param name="fileChanged"> 图像路径是否有变 </param>
    private Task UpdatePreviewAsync(bool fileChanged = false) =>
        TryOrShowAsync(
            "刷新预览",
            async () => {
                await _cts.CancelAsync();
                _cts.Dispose();
                var token = (_cts = new()).Token;
                if (!PathSelected)
                    (_thumb, PreviewImg) = (null, null);
                else if (EnablePreview) {
                    await Task.Delay(128, token); // 防抖
                    if (fileChanged || _thumb is null)
                        _thumb = await Processor.GetThumb(ImgPaths[ImgPathIdx], token);
                    using var thumb = (MagickImage)_thumb.Clone();
                    PreviewImg = await Processor.Preview(thumb, CurConfig, token);
                } else
                    PreviewImg = null;
            });

    #endregion 预览

    #region 执行处理

    /// <summary> 是否可以运行 </summary>
    private bool CanRun => ImgPaths.Count > 0;

    /// <summary> 打断预览并执行处理 </summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run() =>
        TryOrShow(
            "执行处理",
            () => {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new();
                var save = SaveActions.Get(SaveFormats[SaveFormatIdx]);
                var issues = Processor.Run(ImgPaths, CurConfig, save);
                if (issues.Count > 0)
                    throw new AggregateException($"以下图像处理异常：\n{string.Join('\n', issues)}");
                ShowInfo("成功", "所有图像处理完成！");
            });

    #endregion 执行处理
}
