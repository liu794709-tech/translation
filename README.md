# 划词翻译 & OCR 工具 (Screen Translation & OCR Tool)

[![Language](https://img.shields.io/badge/Language-C%23-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Framework](https://img.shields.io/badge/Framework-.NET%207%20%7C%20WPF-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](#)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

一款强大而便捷的 Windows 划词翻译与 OCR 识别工具。只需拖动鼠标，即可轻松识别并翻译屏幕上任何区域的文本——无论是图片、视频、受保护的文档还是游戏界面。

---



## ✨ 主要功能

- 🖥️ **屏幕任意区域识别 (OCR)**: 基于 Tesseract 引擎，能够轻松识别图片、视频、PDF 等不可复制的文本。
- 🌐 **即时翻译**: 集成百度翻译 API，提供快速准确的翻译结果。（需要用户自行配置 API 密钥）
- 🖱️ **高度自定义触发**: 支持多种鼠标/键盘组合键触发（如 `鼠标中键`、`鼠标右键`、`Alt + 鼠标左键`），满足不同用户习惯。
- 📚 **历史记录回顾**: 自动保存每一次的翻译结果，方便后续复习和查找。
- ⚙️ **系统托盘集成**: 在后台安静运行，通过托盘图标轻松访问设置和历史记录，不打扰工作流程。
-  moveable **可交互的结果窗口**: 翻译结果窗口不仅会自动调整位置防止超出屏幕，还可以手动拖动，并支持复制其中的文本。
- ⏱️ **阅后即焚**: 结果窗口在弹出 5 秒后会自动关闭，当鼠标悬停时会暂停计时，体验流畅。
- 🔒 **单例模式**: 无论双击多少次，保证只有一个程序实例在运行，防止资源浪费和冲突。
- 🚀 **绿色便携**: 无需安装，下载解压后，运行一次初始化脚本即可使用。

## 🚀 如何使用 (面向用户)

1.  **下载**
    *   前往本项目的 [**Releases**](https://github.com/liu794709-tech/translation) 页面。
    *   下载最新的 `translation.zip` 文件。

2.  **安装 (初始化)**
    *   将下载的 `.zip` 文件解压到一个您喜欢的位置。
    *   打开解压后的文件夹，您会看到 `🚀 一键启动.bat` 和一个 `App` 文件夹。
    *   **双击运行 `🚀 一键启动.bat`**。这个脚本会自动为您完成两件事：
        1.  在 C 盘配置 OCR 所需的语言文件。
        2.  在您的桌面上创建一个名为“划词翻译工具”的快捷方式。

3.  **配置 API 密钥 (非常重要！)**
    *   本工具需要您提供自己的百度翻译 API 密钥才能工作。
    *   **第一步：申请密钥**
        *   访问 [百度翻译开放平台](https://fanyi-api.baidu.com/)，注册并登录。
        *   开通“通用文本翻译”服务，您将获得一个 `APP ID` 和 `密钥 (Secret Key)`。
    *   **第二步：填入工具**
        *   启动本程序（通过桌面快捷方式）。
        *   在屏幕右下角的系统托盘区，右键点击程序图标，选择“设置”。
        *   将您的 `APP ID` 和 `Secret Key` 填入并保存。

4.  **开始使用！**
    *   程序在后台运行时，按住您设定的快捷键（默认为鼠标中键），在屏幕上拖拽出一个矩形区域，松开鼠标即可看到翻译结果！

## 🛠️ 面向开发者

本项目使用 Visual Studio 2022 和 .NET 7 构建。

1.  **环境准备**
    *   安装 [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)，并确保已安装“.NET 桌面开发”工作负载
    *   确保已安装 [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)。

2.  **构建项目**
    *   克隆本仓库：`git clone https://github.com/liu794709-tech/translation`
    *   用 Visual Studio 打开 `translation.sln` 解决方案文件。
    *   等待 NuGet 自动还原所有依赖包。
    *   直接按 `F5` 或点击“启动”按钮即可在 Debug 模式下运行。

3.  **关于 `tessdata`**
    *   本项目的 `.csproj` 文件已配置为在编译时自动将 `tessdata` 文件夹复制到输出目录，因此您无需手动处理.

## 🔧 技术栈

- **核心框架**: .NET 7, C# 11, WPF
- **OCR 引擎**: [Tesseract.NET](https://github.com/charlesw/tesseract) (Tesseract 4.1.1 的 .NET 封装)
- **JSON 处理**: [Newtonsoft.Json](https://www.newtonsoft.com/json)
- **系统托盘**: [Hardcodet.NotifyIcon.Wpf](http://www.hardcodet.net/wpf-notifyicon)
- **翻译服务**: [百度翻译 API](https://fanyi-api.baidu.com/)

## 📄 许可证 (License)

本项目采用 [MIT License](LICENSE) 开源。