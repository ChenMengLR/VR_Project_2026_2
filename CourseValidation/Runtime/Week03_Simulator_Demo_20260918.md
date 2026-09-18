# Week 03 Meta XR Simulator 电脑端演示（个人实践）

- 记录时间：2026-09-18
- 运行方式：Unity Play Mode + Meta XR Simulator（OpenXR Operator 注入模拟头显和控制器输入）
- 设备边界：本次没有 Quest 真机，因此视频是电脑端模拟器演示；是否满足教师要求仍以教师确认结果为准。
- 范围：个人实践；第 3 周小组作业按用户要求不纳入。

## 演示链路

1. **开始状态**：Unity Game 视图显示 StartZone、TargetZone、MissionObject。
2. **真实传送**：左手控制器摇杆 Y=1，运行中的左手 `TeleportControllerInteractor` 进入 `Hover`，候选点为 TargetZone `(1.50, 0.09, 0.75)`；释放摇杆后相机从 `(-1.5000, 1.7215, 0.7500)` 移到 `(1.4137, 1.7430, 0.7500)`。
3. **真实抓取**：右手抓取目标进入 `Hover`，按下 Grip 后为 `Select`，`grabSelected=true`。
4. **移动物体**：保持 Grip，任务球从 `(1.5000, 0.2065, 1.1500)` 移到 `(1.7049, 0.6565, 1.1730)`，高度增加约 `0.450` m。
5. **释放**：释放 Grip 后抓取状态回到 `Normal`，任务球在重力下继续运动；最终状态为 `(1.4621, 0.1440, 1.7697)`。

## 运行证据

- `evidence-00-start.json`：开始状态。
- `evidence-01-teleport-hover.json`：传送候选 `Hover`。
- `evidence-02-after-teleport.json`：传送提交后的相机位置。
- `evidence-03-grab-hover.json`：抓取候选 `Hover`。
- `evidence-04-grab-select.json`：Grip 选中任务球。
- `evidence-05-grab-move.json`：保持 Grip 移动物体。
- `evidence-06-before-release.json`：释放前物体仍被选中。
- `evidence-07-release.json`：释放后回到 `Normal`。
- `evidence-08-final.json`：最终运行状态。

## 视频

`CourseValidation/Week03_Simulator_Demo.mp4` 是从 Unity 窗口 **Game** 视图录制的 20.2 秒 MP4，分辨率 1280×720、15 fps。视频中可见 Unity Game 视图、传送弧线/目标区、抓取控制器和任务球运动。视频没有 Quest 真机画面。
