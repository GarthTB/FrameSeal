using FrameSeal.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FrameSeal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void ListBox_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel vm
            && sender is ListBox
            && e.Data.GetData(DataFormats.FileDrop) is string[] files)
            vm.AddImages(files);
    }
}
