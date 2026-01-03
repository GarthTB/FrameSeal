namespace FrameSeal.ViewModels;

using System.Windows;

/// <summary> 视图模型的工具 </summary>
internal static class Utils
{
    /// <summary> 弹窗提示信息 </summary>
    /// <param name="title"> 弹窗标题 </param>
    /// <param name="msg"> 提示信息 </param>
    public static void ShowInfo(string title, string msg) =>
        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);

    /// <summary> 尝试执行动作，静默处理打断，其他异常弹窗提示 </summary>
    /// <param name="name"> 动作名称 </param>
    /// <param name="action"> 动作 </param>
    public static void TryOrShow(string name, Action action) {
        try {
            action();
        } catch (Exception ex) {
            if (ex is not OperationCanceledException)
                return;
            var msg = $"{name}时出错{FormatExMsg(ex.Message, ex.StackTrace)}";
            MessageBox.Show(msg, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary> 尝试等待异步动作，静默处理打断，其他异常弹窗提示 </summary>
    /// <param name="name"> 动作名称 </param>
    /// <param name="func"> 动作 </param>
    public static async Task TryOrShowAsync(string name, Func<Task> func) {
        try {
            await func();
        } catch (Exception ex) {
            if (ex is not OperationCanceledException)
                return;
            var msg = $"{name}时出错{FormatExMsg(ex.Message, ex.StackTrace)}";
            MessageBox.Show(msg, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary> 格式化异常信息 </summary>
    /// <param name="exMsg"> 异常描述 </param>
    /// <param name="stackTrace"> 栈追踪 </param>
    private static string FormatExMsg(string exMsg, string? stackTrace) {
        var msg = string.IsNullOrWhiteSpace(exMsg)
            ? ""
            : $": {exMsg}";
        if (!string.IsNullOrWhiteSpace(stackTrace))
            msg += $"\n栈追踪：\n{stackTrace}";
        return msg;
    }
}
