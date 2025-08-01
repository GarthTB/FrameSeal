using FrameSeal.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FrameSeal.Views;

/// <summary> MainWindow.xaml 的交互逻辑 </summary>
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    /// <summary> 向ListBox中拖拽待处理的图像 </summary>
    private void ListBox_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel vm
            && sender is ListBox
            && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            foreach (var path in paths)
                if (!vm.ImagePaths.Contains(path))
                    vm.ImagePaths.Add(path);
            vm.RunCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary> 向CheckBox中拖拽图标 </summary>
    private void CheckBox_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel vm
            && sender is CheckBox
            && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            try { vm.Icon = new(paths[0]); }
            catch { vm.Icon = null; }
    }
}
