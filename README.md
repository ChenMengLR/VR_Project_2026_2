# VR Project 2026 2

WANG HAOBIN 的个人 VR 课程项目。第 2 周使用 Unity XR Interaction Toolkit 学习抓取，第 3 周使用 Meta XR Interaction SDK 学习移动与传送。当前没有 Quest 设备，真机验证与小组任务仍待完成。

## 打开和练习

本机项目：`C:\Users\31797\Desktop\VR_Project_2026_2`。双击根目录 `Open-Course.cmd`，或通过 Unity Hub 打开本目录。

编辑器启动完成后，先确认 Console 没有编译错误。保存当前场景、退出 Play，再用以下菜单切换：

- `Course > Open Week 02 Computer Practice`：打开第 2 周场景、关闭 Meta 模拟器及桌面 OpenXR 自动启动，使用 XRI 键鼠模拟器。
- `Course > Open Week 03 Computer Practice`：打开第 3 周场景，启用桌面 OpenXR 自动启动及本机 Meta 模拟器。

第 2 周在 Game 窗口取得焦点后，可用 H、[、] 选择头/左右手，WASD 移动，鼠标右键旋转，G 抓取，抓取中 T 激活，松开 G 释放。具体输入以官方模拟器屏幕提示为准。

第 3 周使用 Meta 模拟器自身的 Input Bindings，打开课程示例的 Locomotion Settings 切换 Slide/Teleport、Snap/Smooth Turn。不要将第 2 周按键直接套用到第 3 周。

## 环境版本

- Unity Editor `6000.3.24f1`，Universal 3D / URP `17.3.0`。
- Input System `1.20.0`；XR Interaction Toolkit `3.3.2`；OpenXR `1.18.0`。
- Meta XR All-in-One / Interaction SDK `205.0.0`；独立 Windows Meta XR Simulator `205.0`。
- Android Build Support；OpenJDK `17.0.18+8`；NDK `27.2.12479018`（r27c）；SDK Build/Platform Tools `36.0.0`；Command-line Tools `16.0`；CMake `3.22.1`。

Windows 和 Android 已配置 OpenXR/Oculus Touch，Android 启用 Meta Quest Support。课程使用 ControllersOnly，不启用真实手追踪。Android 工具链已核验，但尚未进行 APK 构建或 Quest 真机测试。

## 场景和任务

`Assets/01_Scenes/Week02_XR_Basics.unity` 包含一个官方 XR Origin、一个 XR Interaction Simulator、静态地面、两个方块和独立球体 ExtensionObject。颜色依次表现普通、悬停、选择、激活状态。RotateObject 脚本保留，最终抓取场景中禁用旋转组件。

`Assets/01_Scenes/Week03_Meta_Locomotion.unity` 基于官方 LocomotionExamples 副本，保留原来的 Meta Rig、移动环境与设置 UI。新增绿色 StartZone、蓝色 TargetZone、支持 SnapPositionAndRotation 的热点，以及带真实 Meta Grab/Rigidbody 的 MissionObject。官方原样例未改动。

## 核验记录

- 两周场景均已编译、执行构建器并通过结构检查，Missing Script 为 0。
- 第 2 周在真实 Play Mode 中完成 79 项自动 API 交互检查，全部通过；检查物理落地、六类 XRI 事件、材质颜色、实际跟随、释放与物体独立性，无运行错误。
- [第 2 周运行报告](CourseValidation/Runtime/Week02_Runtime.md)、[机器可读结果](CourseValidation/Runtime/Week02_Runtime.json)、[连续相机测试视频](CourseValidation/Week02_Automated_Play_Test.mp4)。视频 1280×720，8 fps，约 15.9 秒，记录自动验证过程。
- 第 3 周真实模拟器运行验证正在处理，后续状态写入单独运行报告。结构通过不等于传送抓取运行通过。

测试视频和截图属于自动协助验证，不能声称为学生本人键鼠演示或 Quest 真机录像。真实运行报告会明确记录验证方式及范围。

## 提交作业

第 2 周：个人仓库链接、ExtensionObject 的 Inspector 截图和短抓取视频。

第 3 周：个人仓库链接、Quest 真机连续演示 Start → Teleport → Grab → Release。电脑模拟器证据不能替代课程要求的真机视频。

GitHub 上传与 Notion 作业提交是两个步骤。当前尚未填写个人作业卡，也未完成团队选题、团队卡与真机小组演示。仓库为私有，老师需获得访问权限后才能查看。

## 保存并上传新版本

在项目根目录打开终端，先保存 Unity 场景与代码，再执行：

```powershell
git status
git add Assets Packages ProjectSettings README.md .gitignore .gitattributes
git diff --cached --name-only
git commit -m "Describe the practice completed today"
git push
```

提交 `Assets` 及其 `.meta`、`Packages`、`ProjectSettings`。`Library`、`Temp`、`Logs`、`UserSettings` 和构建输出由 `.gitignore` 排除。较大二进制资源按 `.gitattributes` 使用 Git LFS；不要删除或遗漏 `.meta`。

换电脑先安装相同 Editor 和必要模块，再 `git clone`、`git lfs pull`，通过 Hub 打开项目，等待包下载与资源导入。`Open-Course.cmd` 中的编辑器路径是本机路径，换电脑后按实际位置修改或直接使用 Hub。

## 课程来源

- [第 2 周 Unity Editor Basics and First XR Interaction](https://app.notion.com/p/3cf7c00fe5ad8118803cedb669e28446)
- [第 3 周 Meta XR Locomotion Lab](https://app.notion.com/p/3cf7c00fe5ad8111b891c9900e29a307)

Unity 与 Meta 官方导入资源保留其原许可与来源。`Assets/Editor` 的 Course 工具用于搭建、切换和自动检查；初次学习按说明理解组件后再修改，不应把构建器的输出误写成独立完成的学习经历。
