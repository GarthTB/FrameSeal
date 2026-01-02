namespace FrameSeal.Views;

using ViewModels;

/// <summary> 主窗口的交互逻辑 </summary>
public sealed partial class MainWindow
{
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
