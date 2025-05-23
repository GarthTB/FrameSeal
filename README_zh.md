# 📸 Frame Seal 🖼

[![README English](https://img.shields.io/badge/README-English-blue)](https://github.com/GarthTB/FrameSeal/blob/master/README.md)
[![用前必读 中文](https://img.shields.io/badge/用前必读-中文-red)](https://github.com/GarthTB/FrameSeal/blob/master/README_zh.md)
[![开发框架 .NET 9.0](https://img.shields.io/badge/开发框架-.NET%209.0-indigo)](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)
[![最新版本 v1.1.0](https://img.shields.io/badge/最新版本-1.1.0-brightgreen)](https://github.com/GarthTB/FrameSeal/releases/latest)
[![开源协议 Apache-2.0](https://img.shields.io/badge/开源协议-Apache%202.0-royalblue)](https://www.apache.org/licenses/LICENSE-2.0)

## 简介

**Frame Seal** 是一款开源工具，用于为图片添加纯色边框（支持自定义颜色和宽度），并在下边框处嵌入作者信息及EXIF元数据。

## 功能特点

- 🖌️ **纯色边框**：自由设置边框颜色与宽度
- 📄 **EXIF信息展示**：自动提取并显示图片的EXIF数据（如拍摄设备、时间等）
- 🖼️ **WPF图形界面**：基于.NET 9.0 WPF框架开发，操作直观
- 📦 **轻量依赖**：仅依赖CommunityToolkit.Mvvm和Magick.NET

## 安装

1. 安装 [.NET 9.0 Runtime](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)
2. 下载 [最新版本](https://github.com/GarthTB/FrameSeal/releases/latest)
3. 解压即用

## 项目依赖

- [CommunityToolkit.Mvvm](https://github.com/MicrosoftDocs/CommunityToolkit/)
- [Magick.NET](https://github.com/dlemstra/Magick.NET)

## 开源协议

本项目以 [Apache-2.0 协议](https://www.apache.org/licenses/LICENSE-2.0) 开源，允许修改和分发。

## 贡献指南

[Github 仓库链接](https://github.com/GarthTB/FrameSeal)

欢迎提交 Issue 和 Pull Request！请遵循代码规范并与开发者Garth TB联系。

## 版本日志

### v1.1.0 (20250515)

- 新增：图标与文字的间距调节
- 新增：字体选择
- 修复：自动获取的日期样式

### v1.0.0 (20250507)

- 首个发布！
