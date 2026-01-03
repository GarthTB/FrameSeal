namespace FrameSeal.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary> 主窗口的视图模型 </summary>
internal sealed partial class MainViewModel: ObservableObject
{
    /// <summary> 图标路径（null时禁用） </summary>
    [ObservableProperty]
    private string? _iconPath;

    /// <summary> 待处理图像的路径 </summary>
    public ObservableCollection<string> ImgPaths { get; } = [];

    /// <summary> 判断是否可以运行 </summary>
    private bool CanRun => ImgPaths.Count > 0;

    /// <summary> 打断预览并执行处理 </summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run() {}
}
