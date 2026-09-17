# Week 02 真实 Play Mode 自动验证

结果：**RUNTIME_API_PASS**
时间 (UTC)：2026-09-17T06:21:10.4121432Z；Unity：6000.3.24f1
渲染设备：Direct3D11 / NVIDIA GeForce RTX 5080 Laptop GPU

验证范围：实际 Unity Play Mode、PhysX 重力与落地、官方 XRInteractionManager Hover/Select/Release、官方 XRBaseInputInteractor ManualValue 输入激活/取消激活、实际 Renderer 属性和 URP Camera 渲染。没有直接调用 GrabVisualFeedback 方法或 Invoke 其事件。

**未验证：真人键鼠操作 XR Interaction Simulator、头显/手柄硬件、学生个人演示与课堂录屏。这份报告不能替代上述演示或作业提交。**

Rig Interactor 和 Simulator 仅在自动测试期间暂停，以避免争抢；结束时恢复，并退出 Play Mode。测试没有保存运行时场景修改。

录像来源：实际观察相机连续帧，Time.captureFramerate=8 固定模拟时间步；共 127 帧，约 15.88 秒。录制墙钟耗时可能更长。此录像应标为“自动 API 交互测试”，不能标为学生亲自操作录像。
帧目录：`C:\Users\31797\Desktop\VR_Project_2026_2\CourseValidation\Frames\Week02\20260917-062111-704`

- [x] **PracticeCube dynamic Rigidbody and solid Collider** — gravity=True, kinematic=False, mass=1.00
- [x] **GrabCube_02 dynamic Rigidbody and solid Collider** — gravity=True, kinematic=False, mass=1.00
- [x] **ExtensionObject dynamic Rigidbody and solid Collider** — gravity=True, kinematic=False, mass=2.50
- [x] **PracticeCube gravity moved object down** — startY=1.000, endY=0.500, drop=0.500 m
- [x] **PracticeCube initial floor landing** — collider.bottomY=0.0000 m, linearSpeed=0.0000 m/s, sleeping=True
- [x] **GrabCube_02 gravity moved object down** — startY=1.000, endY=0.400, drop=0.600 m
- [x] **GrabCube_02 initial floor landing** — collider.bottomY=0.0000 m, linearSpeed=0.0000 m/s, sleeping=True
- [x] **ExtensionObject gravity moved object down** — startY=1.300, endY=0.325, drop=0.975 m
- [x] **ExtensionObject initial floor landing** — collider.bottomY=0.0000 m, linearSpeed=0.0000 m/s, sleeping=True
- [x] **Camera render 01_initial_landed** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **PracticeCube feedback exists** — GrabVisualFeedback component
- [x] **PracticeCube manager HoverEnter** — hoverEntered=1, hoverExited=0, selectEntered=0, selectExited=0, activated=0, deactivated=0
- [x] **PracticeCube hover: yellow** — Renderer _BaseColor actual=RGBA(1.000, 0.922, 0.016, 1.000), expected=RGBA(1.000, 0.922, 0.016, 1.000); XRI hovered=True, selected=False
- [x] **Camera render PracticeCube_02_hover** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **PracticeCube manager SelectEnter** — hoverEntered=1, hoverExited=0, selectEntered=1, selectExited=0, activated=0, deactivated=0
- [x] **PracticeCube selection takes priority over hover: green** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 0.000, 1.000), expected=RGBA(0.000, 1.000, 0.000, 1.000); XRI hovered=True, selected=True
- [x] **PracticeCube actual XRI follow** — objectMoved=1.230 m, attachError=0.000 m; only interactor transform moved
- [x] **Camera render PracticeCube_03_selected_follow** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **PracticeCube official input Activate** — hoverEntered=1, hoverExited=0, selectEntered=1, selectExited=0, activated=1, deactivated=0
- [x] **PracticeCube activation takes priority: magenta** — Renderer _BaseColor actual=RGBA(1.000, 0.000, 1.000, 1.000), expected=RGBA(1.000, 0.000, 1.000, 1.000); XRI hovered=True, selected=True
- [x] **Camera render PracticeCube_04_activated** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **PracticeCube selected and activated after hover exit** — hoverEntered=1, hoverExited=1, selectEntered=1, selectExited=0, activated=1, deactivated=0
- [x] **PracticeCube hover exit must preserve activation: magenta** — Renderer _BaseColor actual=RGBA(1.000, 0.000, 1.000, 1.000), expected=RGBA(1.000, 0.000, 1.000, 1.000); XRI hovered=False, selected=True
- [x] **PracticeCube official input Deactivate** — hoverEntered=1, hoverExited=1, selectEntered=1, selectExited=0, activated=1, deactivated=1
- [x] **PracticeCube deactivated but selected: green** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 0.000, 1.000), expected=RGBA(0.000, 1.000, 0.000, 1.000); XRI hovered=False, selected=True
- [x] **PracticeCube hover enter must preserve selection: green** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 0.000, 1.000), expected=RGBA(0.000, 1.000, 0.000, 1.000); XRI hovered=True, selected=True
- [x] **PracticeCube manager SelectExit** — hoverEntered=2, hoverExited=1, selectEntered=1, selectExited=1, activated=1, deactivated=1
- [x] **PracticeCube released while hovered: yellow** — Renderer _BaseColor actual=RGBA(1.000, 0.922, 0.016, 1.000), expected=RGBA(1.000, 0.922, 0.016, 1.000); XRI hovered=True, selected=False
- [x] **PracticeCube hover exited after release: cyan** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 1.000, 1.000), expected=RGBA(0.000, 1.000, 1.000, 1.000); XRI hovered=False, selected=False
- [x] **PracticeCube all six real XRI events** — hoverEntered=2, hoverExited=2, selectEntered=1, selectExited=1, activated=1, deactivated=1
- [x] **PracticeCube released gravity** — releaseY=1.600, landedY=0.500, drop=1.100 m
- [x] **PracticeCube released floor landing** — collider.bottomY=0.0000 m, linearSpeed=0.0000 m/s, sleeping=True
- [x] **PracticeCube independent from GrabCube_02** — other object moved=0.000 m
- [x] **PracticeCube independent from ExtensionObject** — other object moved=0.000 m
- [x] **Camera render PracticeCube_05_released_landed** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **GrabCube_02 feedback exists** — GrabVisualFeedback component
- [x] **GrabCube_02 manager HoverEnter** — hoverEntered=1, hoverExited=0, selectEntered=0, selectExited=0, activated=0, deactivated=0
- [x] **GrabCube_02 hover: yellow** — Renderer _BaseColor actual=RGBA(1.000, 0.922, 0.016, 1.000), expected=RGBA(1.000, 0.922, 0.016, 1.000); XRI hovered=True, selected=False
- [x] **Camera render GrabCube_02_02_hover** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **GrabCube_02 manager SelectEnter** — hoverEntered=1, hoverExited=0, selectEntered=1, selectExited=0, activated=0, deactivated=0
- [x] **GrabCube_02 selection takes priority over hover: green** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 0.000, 1.000), expected=RGBA(0.000, 1.000, 0.000, 1.000); XRI hovered=True, selected=True
- [x] **GrabCube_02 actual XRI follow** — objectMoved=1.230 m, attachError=0.000 m; only interactor transform moved
- [x] **Camera render GrabCube_02_03_selected_follow** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **GrabCube_02 official input Activate** — hoverEntered=1, hoverExited=0, selectEntered=1, selectExited=0, activated=1, deactivated=0
- [x] **GrabCube_02 activation takes priority: magenta** — Renderer _BaseColor actual=RGBA(1.000, 0.000, 1.000, 1.000), expected=RGBA(1.000, 0.000, 1.000, 1.000); XRI hovered=True, selected=True
- [x] **Camera render GrabCube_02_04_activated** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **GrabCube_02 selected and activated after hover exit** — hoverEntered=1, hoverExited=1, selectEntered=1, selectExited=0, activated=1, deactivated=0
- [x] **GrabCube_02 hover exit must preserve activation: magenta** — Renderer _BaseColor actual=RGBA(1.000, 0.000, 1.000, 1.000), expected=RGBA(1.000, 0.000, 1.000, 1.000); XRI hovered=False, selected=True
- [x] **GrabCube_02 official input Deactivate** — hoverEntered=1, hoverExited=1, selectEntered=1, selectExited=0, activated=1, deactivated=1
- [x] **GrabCube_02 deactivated but selected: green** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 0.000, 1.000), expected=RGBA(0.000, 1.000, 0.000, 1.000); XRI hovered=False, selected=True
- [x] **GrabCube_02 hover enter must preserve selection: green** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 0.000, 1.000), expected=RGBA(0.000, 1.000, 0.000, 1.000); XRI hovered=True, selected=True
- [x] **GrabCube_02 manager SelectExit** — hoverEntered=2, hoverExited=1, selectEntered=1, selectExited=1, activated=1, deactivated=1
- [x] **GrabCube_02 released while hovered: yellow** — Renderer _BaseColor actual=RGBA(1.000, 0.922, 0.016, 1.000), expected=RGBA(1.000, 0.922, 0.016, 1.000); XRI hovered=True, selected=False
- [x] **GrabCube_02 hover exited after release: cyan** — Renderer _BaseColor actual=RGBA(0.000, 1.000, 1.000, 1.000), expected=RGBA(0.000, 1.000, 1.000, 1.000); XRI hovered=False, selected=False
- [x] **GrabCube_02 all six real XRI events** — hoverEntered=2, hoverExited=2, selectEntered=1, selectExited=1, activated=1, deactivated=1
- [x] **GrabCube_02 released gravity** — releaseY=1.500, landedY=0.400, drop=1.100 m
- [x] **GrabCube_02 released floor landing** — collider.bottomY=0.0000 m, linearSpeed=0.0000 m/s, sleeping=True
- [x] **GrabCube_02 independent from PracticeCube** — other object moved=0.000 m
- [x] **GrabCube_02 independent from ExtensionObject** — other object moved=0.000 m
- [x] **Camera render GrabCube_02_05_released_landed** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **ExtensionObject manager HoverEnter** — hoverEntered=1, hoverExited=0, selectEntered=0, selectExited=0, activated=0, deactivated=0
- [x] **Camera render ExtensionObject_02_hover** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **ExtensionObject manager SelectEnter** — hoverEntered=1, hoverExited=0, selectEntered=1, selectExited=0, activated=0, deactivated=0
- [x] **ExtensionObject actual XRI follow** — objectMoved=1.230 m, attachError=0.000 m; only interactor transform moved
- [x] **Camera render ExtensionObject_03_selected_follow** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **ExtensionObject official input Activate** — hoverEntered=1, hoverExited=0, selectEntered=1, selectExited=0, activated=1, deactivated=0
- [x] **Camera render ExtensionObject_04_activated** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **ExtensionObject selected and activated after hover exit** — hoverEntered=1, hoverExited=1, selectEntered=1, selectExited=0, activated=1, deactivated=0
- [x] **ExtensionObject official input Deactivate** — hoverEntered=1, hoverExited=1, selectEntered=1, selectExited=0, activated=1, deactivated=1
- [x] **ExtensionObject manager SelectExit** — hoverEntered=2, hoverExited=1, selectEntered=1, selectExited=1, activated=1, deactivated=1
- [x] **ExtensionObject all six real XRI events** — hoverEntered=2, hoverExited=2, selectEntered=1, selectExited=1, activated=1, deactivated=1
- [x] **ExtensionObject released gravity** — releaseY=1.425, landedY=0.325, drop=1.100 m
- [x] **ExtensionObject released floor landing** — collider.bottomY=0.0000 m, linearSpeed=0.0000 m/s, sleeping=True
- [x] **ExtensionObject independent from PracticeCube** — other object moved=0.000 m
- [x] **ExtensionObject independent from GrabCube_02** — other object moved=0.000 m
- [x] **Camera render ExtensionObject_05_released_landed** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **Camera render 09_all_released** — Actual URP Camera -> RenderTexture -> PNG, 1280 x 720
- [x] **Continuous camera frames** — frames=127, fps=8, simulated seconds=15.750; directory=C:\Users\31797\Desktop\VR_Project_2026_2\CourseValidation\Frames\Week02\20260917-062111-704
- [x] **No Unity Error/Exception during validation** — 

## 本次实际相机截图

- [01_initial_landed.png](01_initial_landed.png)
- [PracticeCube_02_hover.png](PracticeCube_02_hover.png)
- [PracticeCube_03_selected_follow.png](PracticeCube_03_selected_follow.png)
- [PracticeCube_04_activated.png](PracticeCube_04_activated.png)
- [PracticeCube_05_released_landed.png](PracticeCube_05_released_landed.png)
- [GrabCube_02_02_hover.png](GrabCube_02_02_hover.png)
- [GrabCube_02_03_selected_follow.png](GrabCube_02_03_selected_follow.png)
- [GrabCube_02_04_activated.png](GrabCube_02_04_activated.png)
- [GrabCube_02_05_released_landed.png](GrabCube_02_05_released_landed.png)
- [ExtensionObject_02_hover.png](ExtensionObject_02_hover.png)
- [ExtensionObject_03_selected_follow.png](ExtensionObject_03_selected_follow.png)
- [ExtensionObject_04_activated.png](ExtensionObject_04_activated.png)
- [ExtensionObject_05_released_landed.png](ExtensionObject_05_released_landed.png)
- [09_all_released.png](09_all_released.png)
