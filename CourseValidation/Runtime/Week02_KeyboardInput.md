# 第 2 周虚拟键盘事件注入自动验证

结果：**PASS**。2026-09-17，在 Unity 6000.3.24f1 的 `Week02_XR_Basics` 干净 Play 会话中执行一次最小输入链检查，通过后已退出 Play。

## 本次验证做了什么

使用 Unity Input System 官方 API 创建一个临时虚拟 Keyboard，向它注入 G、G+T、G、全部释放四种按键状态，让正常 PlayerLoop 逐帧处理。输入经过项目现有的官方 action 绑定、XR Interaction Simulator、官方右手 XRSimulatedController 和 NearFarInteractor，最终触发目标 `PracticeCube` 的真实交互事件。

没有直接调用 XRInteractionManager 的 SelectEnter/SelectExit，没有直接触发交互事件，也没有直接修改模拟控制器的 grip/trigger 状态。为避免额外调试鼠标瞄准，运行时临时平移了 XROrigin，使现有右手射线朝向目标，先等待真实 Hover 后才注入 G。没有保存这次运行时位置调整。

## 实际观察

- 第 177 帧：真实 Hover，未按 G/T，未选中。
- 第 180 帧：G 为按下状态，模拟器 grip 与官方右手 controller grip 均为 true；真实 Select 事件累计 1 次，目标已选中。
- 第 183 帧：G+T 均按下，模拟器和官方 controller 的 grip/trigger 均为 true；真实 Activate 事件累计 1 次，目标仍选中。
- 第 186 帧：保留 G、释放 T，模拟器和 controller 的 trigger 均为 false；真实 Deactivate 事件累计 1 次，目标仍选中。
- 第 189 帧：G/T 全部释放，模拟器和 controller 的 grip/trigger 均为 false；真实 SelectExit 事件累计 1 次，目标不再选中，恢复 Hover。

每个阶段之间至少经过 3 个正常运行帧。四类交互事件都来自同一个真实右手 NearFarInteractor，最终计数各为 1。

## 清理与最终状态

- 已停止 Play，当前场景回到 `Week02_XR_Basics` 编辑状态。
- 场景 `isDirty=false`，未保存 Play Mode 的临时位置变化。
- 临时虚拟键盘数量为 0，临时测试对象数量为 0。
- Input System `backgroundBehavior` 恢复为 `ResetAndDisableNonBackgroundDevices`。
- Editor 输入焦点规则恢复为 `PointersAndKeyboardsRespectGameViewFocus`。
- 两项设置均与测试前逐值一致。
- 测试脚本编译通过；最终编译失败标志为 false，Console 错误数量为 0。

## 证据范围

**这是临时虚拟键盘事件注入的自动验证，不是学生亲手操作，也不是物理键盘、鼠标或 Quest 真机录像。** 它补充证明了 G/T action 到实际 XR 交互的完整输入链；没有验证全部原生窗口焦点、光标锁定和鼠标移动操作，本次没有新增操作录像；已有连续抓取视频来自此前的自动 API 验证。

此前 79 项自动验证结果保持独立，本次没有重复运行或扩大那组检查。机器可读的逐阶段状态保存在同目录 `Week02_KeyboardInput.json`。
