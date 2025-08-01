namespace FrameSeal.Utils;

/// <summary> 用于简化Try-Catch块的工具类 </summary>
internal static class Try
{
    /// <summary> 执行一个Action，若有异常则弹窗提示 </summary>
    public static void Do(string actionName, Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            var msg = string.IsNullOrWhiteSpace(ex.Message)
                ? "无详细错误信息"
                : string.IsNullOrWhiteSpace(ex.StackTrace)
                ? ex.Message
                : $"{ex.Message}\n\n栈跟踪：\n\n{ex.StackTrace}";
            MsgBox.Error($"{actionName} 出错：\n{msg}");
        }
    }
}
