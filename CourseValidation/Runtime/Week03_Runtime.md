# 第 3 周：电脑端 Meta XR Simulator 实测记录

验证日期：2026-09-17。Unity 6000.3.24f1 / Meta XR Core 与 Interaction SDK 205 / Standalone Simulator 205，Windows Editor。

本次已完成真实模拟控制器驱动的热点传送、任务球抓取、移动与释放。另一个独立会话验证了关闭 Operator 后普通 Simulator Play 的稳定运行。没有 Quest，因此不包含头显舒适度、手部真实追踪或 Android APK 的验证。

## 可复核结果

1. StartZone 与 TargetZone 均使用正式 Meta TeleportHotspot。右控制器摇杆 Y 轴推至 1 打开传送弧线，回到 0 确认；先到绿色 StartZone，再到蓝色 TargetZone。
2. TargetZone 候选由运行中的 TeleportInteractor 读回为 `ISDK_TeleportInteraction / TargetZone`。真实传送后头部从 `(-1.4035, 1.7215, 0.7500)` 到 `(1.4035, 1.7215, 0.7500)`，朝向由 90° 转为 270°。热点中心间距 3 m；实际 rig/head 含角色身体偏移，不把其坐标偏差隐去。
3. 控制器靠近橙球后 GrabInteractor 从 Hover 变为 Select，选中父对象 `MissionObject`，球刚体变为运动学。将控制器上移 0.60 m、侧移 0.20 m 后，球跟随到 `(1.6789, 0.8065, 1.2394)`。
4. Grip 归零后交互器退出 Select，球恢复 `isKinematic=false / useGravity=true`，最后在 `(1.6423, 0.2065, 1.3126)` 落到平台上。
5. 运行输入由官方 Meta XR Operator 的 OpenXR 工具注入；读取姿态时用 Unity Pipeline 校准世界/追踪空间。只设置了头部朝向，没有用头部位置代替传送，没有在 Play 中直接改球或 rig 的 Transform。
6. 移动模式使用官方 `MovingSetting.ControllerMovement.Value` 设为 Teleport。此次没有把 UI 模式按钮点击、键盘鼠标操作过程冒充已测试。

## 场景布局修正

原目标靠近建筑门柱，球视线被遮挡。已在非 Play 状态把路线移到官方场景的开阔平台，并保存课程副本；官方原始示例场景保留。

- StartZone：`(-1.5, 0.08903508, 0.75)`，朝向 +X。
- TargetZone：`(1.5, 0.08903508, 0.75)`，朝向 -X。
- MissionObject 初始：`(1.5, 1.139035, 1.15)`；进入 Play 后受重力落到蓝台上，因此抓取时需低头并让控制器靠近球。
- 场景仍保留官方 rig、环境和 Slide / Turn / Teleport 设置，不叠加第二个 rig。

## 日志兼容与普通 Play 对照

首次未加兼容层时，Editor.log 记录 `Is Terminating: True` 与 `Marshal.PtrToStringAnsi → OVRManager.OVRPluginLogCallback` 的非法字节序列异常，实时进程和 Unity status 也确认 Editor 已退出。不能只凭早先的旧 PID 判断崩溃。

核心交互证据是在临时关闭 native callback 的诊断会话中完成的。随后已安装项目内 `Meta205EditorLogCompatibility.cs`，它仅在 Windows Editor + Core 205 生效，通过公开 SetLogCallback2 替换解码回调，保存原始字节与 Console 日志，不改 vendor cache、系统区域设置或系统 OpenXR runtime。

- 直接 Play、Operator 仍开启：未手工 null 回调，持续 107.7 秒 / 6,319 帧；观测时收取 72 条真实 native 日志，退出后共 104 条，callbackErrors=0。
- 关闭 Operator 并关闭 API Layers feature 后普通 Play：94.4 秒 / 5,301 帧时 XR active=true、ControllersOnly、41 条真实 native 日志、0 callback error；随后继续运行至 125.6 秒 / 7,401 帧。
- 单独注入合法分配缓冲区中的非法 UTF-8 字节 `53 59 4E 54 48 20 FF C3 28`，兼容层识别 malformed=1，callbackErrors=0，JSONL 完整保留 `U1lOVEgg/8Mo`。这是明确的合成测试，不是原生崩溃自然复现。该会话停止后的总日志数 71（含这 1 条合成记录）。
- 结束时正常退出 Play，Console 的实际 ground truth：编译失败=false、error=0；仍有 33 条 SDK/诊断 warning，未宣称无警告。

兼容层无法保证保护其安装之前的 SDK 初始化窗口。早期另一次重入 Play 曾遇到 SES 连接失败并自动停 Play，后续两次重入正常。若再次发生自动停止，保留日志后关闭并重新打开 Editor/Simulator，不把它称为已永久消除的问题。

## 第 3 周测试会话结束时配置

- Meta XR Simulator 保持启用；Operator 与 API Layers 已关闭，不是课程日常必装项。
- Meta XR Feature 与 Oculus Touch profile 保持启用。
- 项目 handTrackingSupport 最终为 ControllersOnly；曾被 Meta 自动改为 ControllersAndHands，已使用官方 ProjectConfig API 恢复并保存。
- Unity 外部脚本编辑器已通过官方 API 设置并读回 `D:\Microsoft VS Code\Code.exe`。
- 本机 Simulator 205 绑定：M=菜单、Y=摇杆向上、I=按下摇杆、U=Grip、T=Trigger。I 不等于摇杆向上；当前示例中摇杆点击会取消传送。键位以 Simulator Input Bindings 和本附带 JSON 为准。
- Point & Click / Compatibility / ISDK pointer offset / Action Right 的 UI 开关状态未由本测试确认；请按课堂操作说明手动核对，不把建议等同已验证状态。

## 截图与原始证据

`Week03_01` 到 `Week03_06` 是输入会话关键帧；`Week03_07` 是最终无 Operator 普通 Play 画面。它们是离散截图，不是连续操作录像。

原始输入、读取状态、回调日志、最终配置与按键文件在 `Week03_Evidence/`，汇总机器可读结果在 `Week03_Runtime.json`。证据清单 `Week03_Evidence_SHA256.json` 给出文件哈希。

待人工/硬件完成：在 Simulator UI 中亲自复练键盘鼠标路线、真实 Quest 体验与眩晕/高度检查、Android 构建及头显运行、小组分工、课程平台的最终提交。这些均未以本报告代替。

官方依据：[Meta XR Operator](https://developers.meta.com/horizon/documentation/unity/meta-xr-operator/) 与 [Simulator 工作流](https://developers.meta.com/horizon/documentation/unity/meta-xr-operator/xr-simulator/)。具体请求参数以本次真实 tools/list 与 Core 205 源码为准。

