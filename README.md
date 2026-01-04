# 📸 Frame Seal 图像边框工具 🖼

`Frame Seal` 是一个 Windows x64 单平台的 GUI 应用，用于
向图像四周添加仿胶片外观的纯色边框，并在下边框处嵌入图标、
EXIF 元数据及自定义信息。通过高度自由的参数调整及高性能的
实时预览，用户可以轻松装裱图像，以优化发布工作。

[![框架 .NET 10.0](https://img.shields.io/badge/框架-.NET%20%2010.0-blueviolet)](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)
[![语言 C# 14.0](https://img.shields.io/badge/语言-C%23%2014.0-navy.svg)](https://github.com/dotnet/csharplang)
[![许可 MIT](https://img.shields.io/badge/许可-MIT-brown)](https://mit-license.org)
[![平台 Windows x64](https://img.shields.io/badge/平台-Windows%20x64-orange.svg)](https://github.com/GarthTB/FrameSeal)
[![版本 1.3.0](https://img.shields.io/badge/版本-1.3.0-brightgreen)](https://github.com/GarthTB/FrameSeal/releases/latest)

## ✨ 特点

- 🔢 **丰富参数**
    - 自由调整尺寸、颜色、字体、信息内容
    - 多种高质量保存格式
- 🧠 **自动功能**
    - 自动提取多种 EXIF 元数据
    - 自动输出到同目录中，无覆盖风险
- ⚡ **实时预览** 调整立即响应，所见即所得
- 🏭 **批量处理** 同一参数，多图共用

## 📥 安装与使用

### 系统要求

- 操作系统：Windows 10 或更高版本
- 架构：x64
- 运行依赖：[.NET 10.0 运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

### 使用步骤

1. 下载 [最新版本压缩包](https://github.com/GarthTB/FrameSeal/releases/latest) 并解压
2. 运行 `Frame Seal.exe`
3. 添加图像，调整参数，执行处理

## 🛠 技术栈

- **框架**：.NET 10.0 WPF
- **语言**：C# 14.0
- **依赖**：
    - [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
    - [Magick.NET-Q16-x64](https://github.com/dlemstra/Magick.NET)

## 📜 开源信息

- **作者**：Garth TB | 天卜 <g-art-h@outlook.com>
- **许可证**：[MIT 许可证](https://mit-license.org/)
- **项目地址**：https://github.com/GarthTB/FrameSeal

## 📝 更新日志

### v1.3.0 (20260104)

- **修复异步打断逻辑（严重）**
- 重构处理核心，大幅提升性能
- 优化字体和输出格式选项
- 取消非 x64 处理器支持
- Magick.NET安全性更新

### v1.2.4 (20251218)

- Magick.NET安全性更新，并修复输出格式

### v1.2.3 (20250901)

- 修复：Magick.NET安全性更新
- 优化：保留原图的 EXIF 信息
- 新增：95 质量 JPG 保存格式

### v1.2.2 (20250826)

- Magick.NET安全性更新，并修复图标位置错误

### v1.2.1 (20250801)

- 修复：嵌入图标的一系列错误
- 优化：切割圆角时保留原图的透明度

### v1.2.0 (20250731)

- 新增：多种保存格式选项
- 新增：多种 EXIF 信息选项
- 优化：大幅提升实时预览性能
- 修改：使用 .NET 10.0 框架，专注中文界面，提升可维护性

### v1.1.0 (20250515)

- 修复：EXIF 拍摄时间的格式
- 新增：图标间距参数
- 新增：字体选项

### v1.0.0 (20250507)

- 首个发布！
