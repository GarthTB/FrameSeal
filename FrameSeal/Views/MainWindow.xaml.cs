namespace FrameSeal.Views;

using System.IO;
using System.Windows;
using ViewModels;

/// <summary> 主窗口的交互逻辑 </summary>
public sealed partial class MainWindow
{
    public MainWindow() => InitializeComponent();

    /// <summary> 拖放待处理的图像 </summary>
    private void ImgPathsDrop(object sender, DragEventArgs e) {
        if (DataContext is not MainViewModel vm
         || e.Data.GetData(DataFormats.FileDrop) is not string[] paths
         || paths.Where(path => File.Exists(path) && !vm.ImgPaths.Contains(path)).ToArray() is not {
                Length: > 0
            } newPaths)
            return;
        foreach (var path in newPaths)
            vm.ImgPaths.Add(path);
        vm.RunCommand.NotifyCanExecuteChanged();
    }

    /// <summary> 拖放图标 </summary>
    private void IconPathDrop(object sender, DragEventArgs e) {
        if (DataContext is not MainViewModel vm
         || e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } paths
         || !File.Exists(paths[0]))
            return;
        vm.IconPath = paths[0];
    }
}
