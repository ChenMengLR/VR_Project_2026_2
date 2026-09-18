# VR第2和第3周课程操作指南

适用对象：第一次使用 Unity、目前只有 Windows 电脑、暂时没有 Quest 头显的个人课程实践。

本机环境与两个课程场景已经准备好。本文说明怎样打开项目、亲自完成电脑模拟练习、保存版本以及提交作业。自动搭建和测试的范围会单独注明；运行结果、截图视频与最终推送记录单独列在“当前状态与下一步”中。

## 首页速查：先做什么、最后交什么

本课程使用 Unity 制作可以移动、抓取物体的 VR 场景。第 2 周先掌握 Unity 场景和 XR Interaction Toolkit 的基本交互；第 3 周在同一个课程项目中学习 Meta 的移动与交互示例。

**你现在可以先在电脑上完成环境准备和模拟器练习。第 3 周的最终作业明确要求 Quest 真机连续演示，电脑模拟器录像只能证明练习过程，不能替代这个提交要求。**

建议依次完成：

1. 双击桌面课程项目中的 `Open-Course.cmd`，打开已配置好的 Unity 项目。
2. 从 `Course → Open Week 02 Computer Practice` 进入第 2 周场景 `Week02_XR_Basics`，用 Unity 的 XR Interaction Simulator 练习抓取和颜色反馈。
3. 检查已创建的扩展球体，理解与原方块的差异，保存 Inspector 截图和短抓取视频。
4. 保存第 2 周 Git 版本并上传个人 GitHub 仓库。
5. 从 `Course → Open Week 03 Computer Practice` 进入已经准备的第 3 周场景 `Week03_Meta_Locomotion`。
6. 在电脑上练习从起点传送到目标区，再抓取并释放任务物体。
7. 获得 Quest 使用条件后，连接并验证真机，录制第 3 周正式提交视频。

第 2 周个人提交内容：**个人 GitHub 仓库链接、ExtensionObject 的 Inspector 截图、短抓取视频。**

第 3 周个人提交内容：**个人 GitHub 仓库链接、Quest 真机连续视频**。视频顺序为 Start → Teleport → Grab → Release，提交环境填写实际使用的 Quest。

团队事项另行安排。小组与题目尚未确定时，先完成个人练习，不要把未讨论的构想写成团队已经确认的决定。

## 一、先认识这些工具

**Unity Hub** 管理 Unity Editor 的安装和项目列表，本机为 3.21.3。平时可以从 Hub 进入项目，也可以双击课程根目录的 `Open-Course.cmd` 直接打开。

**Unity Editor** 是真正编辑和运行场景的软件。在里面摆放物体、添加组件、编写交互并点击 Play 测试。本机已安装 Unity 6 LTS `6000.3.24f1`，位置为 `D:\Unity\Editors\6000.3.24f1`，已通过批处理编译检查。

**Visual Studio Code** 用于编辑 C# 脚本。本机为 1.138，C# 与 Unity 扩展已经安装。以后修改脚本后，仍需回到 Unity 查看 Console，确认新代码编译正常。

**XR Interaction Toolkit（XRI）** 提供第 2 周的交互组件。Interactor 是发起交互的一方，例如控制器射线或抓取器；Interactable 是可以被交互的一方，例如加了 XR Grab Interactable 的方块。

**Unity XR Interaction Simulator** 是第 2 周使用的电脑模拟工具，属于 XRI 的学习流程。

**Meta XR Simulator** 是第 3 周使用的独立 Windows 模拟器，配合 Meta 的交互和移动示例。本机已正式安装 205，位置为 `C:\Program Files\MetaXRSimulator\v205.0`。它与 Unity XR Interaction Simulator 是两套工具，课程菜单会帮助切换相应的电脑练习配置。

**OpenXR** 负责 Unity 与 XR 运行环境之间的连接。课程需要核对 Windows、Android 的 OpenXR 设置及对应交互配置。

**Meta Horizon Link** 用于后续连接 Quest。现在没有 Quest 时，可以先完成电脑实践；不能在提交材料中把当前环境写成已通过 Quest 验证。

**Git / GitHub / Git LFS** 分别负责本地版本记录、远程代码仓库、较大二进制资源的版本管理。GitHub 保存课程项目，不能直接代替 Unity 运行程序。本项目已经初始化 Git 和 Git LFS，配置提交署名和二进制资源跟踪规则。

**Unity CLI** 是可选的维护辅助工具，本机版本为 1.0.0-beta.10。它曾遇到凭据保存问题，但 Editor 许可证与批处理运行已经成功。正常打开本项目和上课不依赖 CLI，不需要为此反复登录。

## 二、项目与文件夹约定

课程项目名：`VR_Project_2026_2`，已创建在 `C:\Users\31797\Documents\ChatGPT\作业 3\VR_Project_2026_2`。

当前项目使用 **Unity 6 LTS → Universal 3D**，URP 17.3，Input System 1.20。直接打开现有项目即可。下面的建场景步骤用于理解和复现课程内容，不要求再建一份同名项目。

最方便的打开方式是双击根目录 `Open-Course.cmd`。进入 Unity 后，使用菜单切换课程：

- `Course → Open Week 02 Computer Practice`：打开第 2 周场景，关闭 Meta 模拟器和 Windows XR 自动启动，使用 XRI 键鼠模拟。
- `Course → Open Week 03 Computer Practice`：打开第 3 周场景，启用 Meta 模拟器和 Windows XR 自动启动，进入 Meta 电脑练习配置。

两个场景均位于 `Assets/01_Scenes/`。切换前退出 Play，并保存希望保留的编辑。

在 Project 面板的 `Assets` 下建立以下八个文件夹：

```text
Assets/
├── 01_Scenes/
├── 02_Scripts/
├── 03_Prefabs/
├── 04_Images/
├── 05_Models/
├── 06_Animations/
├── 07_Fonts/
└── 08_Sounds/
```

文件夹前的数字用于保持顺序。场景放 `01_Scenes`，自己的 C# 脚本放 `02_Scripts`。Unity 自动生成的 `.meta` 文件记录资源标识，不能随意删除，也需要提交到 Git。

Unity 中最常用的几个区域：

- **Hierarchy**：当前场景有哪些物体，父子关系是什么。
- **Scene**：编辑场景、移动视角和摆放物体。
- **Game**：运行时相机看到的画面，也是模拟器接收输入的重要窗口。
- **Inspector**：选中物体后，查看和修改它的组件、位置及参数。
- **Project**：项目资源与文件夹。
- **Console**：编译错误、警告和运行消息。出现红色错误时先解决，再继续验证交互。

点击 Play 进入测试，再点击一次退出。**在 Play 模式中临时修改的场景数值，通常不会作为编辑结果保留。需要保留的修改应退出 Play 后重新设置并保存。**

## 三、第 2 周：按顺序搭建基本抓取场景

### 1. 准备 XRI 与练习场景

在 Package Manager 中核对 `XR Interaction Toolkit 3.3.2`，并导入课程需要的 `Starter Assets` 和 `XR Interaction Simulator` 示例。

本项目已经配置 XRI 3.3.2 和课程场景。先通过 `Course → Open Week 02 Computer Practice` 打开现有练习，下面逐项说明场景为何这样设置，以及需要亲自验证的动作。

本周场景保存于 `Assets/01_Scenes/Week02_XR_Basics.unity`。如果重新做练习，保存时也应使用这个课程命名约定；保留原场景的复练可以另用文件名。编辑前核对当前场景名，避免改错场景。

按课程使用官方提供的 XR Origin 预制体，并配置 XR Interaction Manager。尽量沿用配套官方预制体的输入与交互组件，避免自行拼出一个外形相同但缺少控制器配置的对象。

### 2. 建立地面和两个方块

新建练习时创建 Plane 作为地面；本项目中的对应物体名称为 `Floor`。

创建 Cube，命名为 `PracticeCube`，把 Transform 的 Position 设置为 `(0, 1, 2)`。再准备第二个方块 `GrabCube_02`，位置与第一个分开，避免重叠。

两个方块都需要：

- 实体 Collider，`Is Trigger` 关闭。
- Rigidbody，`Use Gravity` 开启，`Is Kinematic` 关闭。
- XR Grab Interactable。
- 课程的 `GrabVisualFeedback` 组件。

Collider 决定物体怎样碰撞，Rigidbody 让物体参与物理运动，XR Grab Interactable 使物体能够被抓取。三者作用不同，只添加其中一个不能完成整套抓取行为。

### 3. 练习脚本的运行时机

PracticeCube 已挂载 `RotateObject`，当前速度为 45 度/秒，绕本地 Y 轴旋转，组件暂时禁用。练习时先启用组件，再分别尝试 `120`、`15`、`-45`，观察旋转快慢与方向变化。

`Start()` 在脚本开始运行时执行一次，适合初始化；`Update()` 每帧执行，适合持续更新。练习中要把“修改哪个参数”和“物体出现什么变化”对应起来。

完成旋转练习后，**保留 RotateObject 组件，但关闭组件启用状态**，避免后续抓取时物体同时自动旋转。不是删除脚本，也不是禁用整个方块。

### 4. 设置抓取颜色反馈

按照课程设置 `GrabVisualFeedback` 的四种状态颜色：

- Idle：青色，表示没有正在发生的悬停或抓取交互。
- Hover：黄色，表示交互器已经指向或接触可交互对象。
- Select：绿色，表示物体被选中或抓取。
- Activate：洋红色，表示在已选中的情况下触发激活动作。

这些颜色要与实际交互事件绑定。仅把材质改成某个固定颜色，不能证明交互反馈已经正确连接。

### 5. 使用 Unity XR Interaction Simulator

确认第 2 周场景使用 Unity 的 XR Interaction Simulator，运行后点击 Game 窗口使其获得输入焦点。课程按键说明如下，实际仍以当前导入示例和界面提示为准：

- `W / A / S / D`：移动当前操控目标。FPS 模式移动头部与双控制器整体；设备模式只移动选中设备。`Q / E` 可上下移动。
- `H`：切换到仅控制头部；再按一次返回之前的操控目标。
- `[` 选择左设备，`]` 选择右设备。保持 Controller 模式；重复按已选中的同一个括号键会切换 Controller / Hand。
- 按住鼠标右键并移动鼠标：旋转当前操控目标。
- 按住鼠标右键并滚动滚轮：前后移动；单设备模式还会带来滚转，单纯前后移动建议使用 `W/S`。
- 按住 `G`：右控制器抓取；松开 `G`：释放。左控制器用 `Shift + G`，释放 G 后再松开 Shift。
- 抓取时按 `T`：右控制器激活；松开 T：取消激活。左控制器用 `Shift + T`。选择操控哪只手的位置，不会改变 G/T 默认发给右手的规则。
- `R`：把双控制器及模拟手放回头部前方，不会重置头部或整个场景。
- `Tab`：切换 FPS 整体操控与设备操控方式。

逐项验证：能够接近物体；指向时变黄；抓取时变绿；激活时变洋红；释放后物体按物理规则运动，原有交互状态恢复。

### 6. 制作自己的 ExtensionObject

当前 ExtensionObject 已独立创建为 Sphere，位置 `(-1.35, 1.3, 2.35)`，缩放 `(0.65, 0.65, 0.65)`，质量 2.5，颜色为橙色。相对基础 Cube，形状、缩放、位置、颜色与质量五项均已改变，无需再创建同名物体。

从零复现时，独立新建 **Sphere 或 Capsule**，命名为 `ExtensionObject`，而不是复制已有 Cube。扩展球体不挂颜色反馈脚本，保持橙色；前述四态变色由两个练习方块演示。

把它的高度设置为 `Y ≥ 1`，并在以下方面至少改变两项：形状、缩放、位置、颜色、质量。为了截图时容易判断，建议主动记下自己改了哪两项及对应数值。

为它配置实体 Collider、Rigidbody 和 XR Grab Interactable，再运行测试确认它确实可以被抓取和释放。它外观改变并不自动代表交互设置完整。

### 7. 保存第 2 周证据

退出 Play，保存场景与脚本。选中 `ExtensionObject`，让 Inspector 清楚显示名称、Transform、Collider、Rigidbody、XR Grab Interactable，以及自己修改的相关参数，保存截图。

录制一段短视频，展示实际抓取和释放。画面需要让人看清你在操作哪个物体以及它的响应，只有静态场景或 Inspector 不能证明交互运行成功。

最后将代码提交并推送至个人仓库，在个人作业卡中填写仓库链接、上传截图和视频。团队卡还需要说明交互设计、Interactor 与 Interactable 的角色，以及一句话描述想让用户获得的体验；小组尚未确定时保留为待办。

## 四、第 3 周：Meta 移动、传送与任务物体

### 1. 保留第 2 周场景，另存第 3 周副本

继续使用同一个 `VR_Project_2026_2` 项目。当前已从 Meta 官方 `LocomotionExamples` 建立自己的场景副本，保存为 `Assets/01_Scenes/Week03_Meta_Locomotion.unity`。通过 `Course → Open Week 03 Computer Practice` 打开。

如果从零复现这个步骤，应先打开官方 `LocomotionExamples`，然后**另存副本**到自己的场景文件夹，命名为 `Week03_Meta_Locomotion`。

不要直接覆盖包内官方示例，也不要覆盖 `Week02_XR_Basics`。保留官方示例有助于对照设置和排查问题。

### 2. 安装并核对 Meta 组件

课程路线使用 Unity 默认 Registry 可获取的 Meta All-in-One SDK，版本路线为 `205`。电脑模拟使用独立的 **Meta XR Simulator 205 Windows**，不要换用旧版 Asset Store 模拟器包。

本机已完成：

- Meta All-in-One SDK：205。
- 独立 Meta XR Simulator：205，`C:\Program Files\MetaXRSimulator\v205.0`。
- OpenXR：1.18。
- 官方场景的课程副本：`Assets/01_Scenes/Week03_Meta_Locomotion.unity`；已检查官方原始示例文件的哈希未改变。

### 3. 核对 OpenXR 设置

核对 Windows 与 Android 的 OpenXR 配置，添加课程指定的 Oculus Touch Profile，Android 侧启用 Meta Quest Support。

本次课程配置需要关闭手追踪相关 Features，不选择 Mock HMD。菜单名称可能随版本变化；需要找到对应功能再核对，不要仅凭名字相似就启用多个选项。

第 3 周使用 Meta 模拟器时，停用第 2 周的 Unity XR Interaction Simulator，避免两套模拟输入同时控制场景。

### 4. 先认识四种移动方式

- **Physical**：通过头显与控制器的真实空间运动改变位置或朝向。
- **Slide**：通过控制输入连续移动。
- **Turn**：通过控制输入改变朝向。
- **Teleport**：指定目标后跳转到目标位置。

目前没有 Quest 时，可观察和练习对应的模拟效果；“Physical 的电脑模拟”不能证明真实活动空间与头显追踪已经测试。

### 5. 建立起点与目标区

创建绿色、较扁的 Cylinder，命名为 `StartZone`。本项目已在 StartZone 和 TargetZone 都添加 Hotspot，以便往返练习；两者相距 3 米，放置在没有门柱遮挡的庭院地面。

蓝色目标区名为 `TargetZone`。当前起点坐标约为 `(-1.5, 0.089, 0.75)`，目标区为 `(1.5, 0.089, 0.75)`。从零添加传送交互时，按课程路线使用：

`Meta Quick Action → Add Teleport Interaction → Hotspot`

选择 `Snap Position And Rotation`，按界面提示执行 `Fix All` / `Create`，补齐课程工具要求的对象和组件。完成后检查生成结果，不要把“点击了创建”直接当作传送已验证成功。

### 6. 创建 MissionObject

在 `TargetZone` 上方放置任务物体，命名为 `MissionObject`，为它提供 Collider 和 Rigidbody。

通过 Meta 的 `Add Grab Interaction` 工具添加抓取，选择课程要求的 `Controllers And Hands / All`，按提示执行 `Fix All` / `Create`。

第 2 周使用的 XR Grab Interactable 与这里 Meta 的抓取配置不同。应按当前场景的交互体系连接，不能只把第 2 周组件复制过来就假定已满足第 3 周任务。

### 7. 启动 Meta XR Simulator

使用本项目课程菜单即可进入第 3 周配置。手动激活菜单为 `Meta → Meta XR Simulator → Activate`。左右 Controller 已核实；其余四项界面开关在开始键鼠练习时按课程逐一核对：

- Inputs 中左右输入均使用 Controller。
- `Point Click`：ON。
- `Compatibility`：ON。
- `ISDK pointer offset`：ON。
- `Action`：Right Controller。

运行场景后，用 `M` 打开 Locomotion Settings，把 Movement Style 设置为 `Teleport`。

传送采用“按住摇杆向上动作 → 指向目标 → 松开”。**本机 Simulator 205 的实际绑定为 Y = 摇杆向上、U = Grip 抓握、T = Trigger、M = Menu。I 是按下摇杆，不是向上，在示例里可能取消传送。** 这些按键已与安装目录中的 keybindings.json 核对；若以后重新绑定，以模拟器 Inputs 为准。第 2 周的 G 抓取键不适用于这里。

### 8. 验证电脑练习流程

按下面顺序完整走一遍，并记录失败发生在哪一步：

1. 从 `StartZone` 开始。
2. 激活传送指示，指向 `TargetZone`。
3. 松开对应动作，确认用户位置移动到目标区。
4. 抓取 `MissionObject`。
5. 移动物体并释放，确认抓取状态结束，物理行为正常。

电脑上完整运行通过后，可以保存模拟器录像用于个人复习和故障说明，但文件说明应明确标注“电脑模拟器实践”。

### 9. 获得 Quest 后完成正式验证

使用 Quest 前，先在 `Meta → Meta XR Simulator` 菜单中选择 **Deactivate**，再按课程使用 Meta Horizon Link 连接 Quest。

连接成功后，在真实头显环境重复 Start → Teleport → Grab → Release 全流程，录制一段连续视频，并按实际设备填写提交环境。

团队第 3 周需要实际使用至少一种移动方式和一个核心交互，在 Quest 上完成 2–3 分钟演示。你的小组和选题尚未确定，这部分先作为明确待办，不在个人环境部署中代替团队做决定。

## 五、保存、上传与交作业是不同的动作

**保存 Unity 场景与代码**：把当前编辑写入本机文件。运行测试前后都应核对是否有未保存改动。

**Git commit**：在本机记录一份版本。提交后仍可能只存在于你的电脑上。

**Git push**：把已提交的版本上传到 GitHub。成功后需要打开仓库核对文件与最新提交。

**提交作业**：到个人作业卡填写仓库链接，并按当周要求附上截图或视频。GitHub 上传成功不会自动填写课程作业卡。

课程仓库应保留 `Assets/` 及其中全部 `.meta`、`Packages/`、`ProjectSettings/`，以及项目说明和版本控制配置。应忽略 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、构建输出、`Recordings/` 等自动生成或录制文件，避免把大型缓存和私人本机配置上传。

Git LFS 用于需要保留的大型二进制资源。当前电脑已安装 LFS 3.7.1，本项目已经完成本地初始化和二进制跟踪配置；`.gitattributes` 需要随项目保存。录屏和构建包按照提交要求单独管理，不因安装了 LFS 就全部加入代码仓库。

个人私有仓库已经创建：[ChenMengLR/VR_Project_2026_2](https://github.com/ChenMengLR/VR_Project_2026_2)。私有仓库只有获得授权的账号才能查看，给老师发送链接后，还需要为老师的 GitHub 账号提供访问权限。

Git 提交署名为 `WANG HAOBIN`，邮箱为 `chenmenglr@gmail.com`。提交身份已经配置；远程仓库创建与首次推送是两个状态，最终推送结果见下一节。

后续日常更新可在项目根目录执行：

```powershell
git status
git add .
git diff --cached --name-only
git commit -m "Complete Week 2 grab interaction practice"
git push
```

提交说明应描述本次真正完成的内容，不能在只有环境安装时写“完成全部课程”。每次 commit 前先检查暂存清单，确认没有误加入缓存、私人文件或尚不应公开的视频。没有新修改时，不需要重复创建提交。

### 换电脑后怎样继续

先在新电脑安装 Git、Git LFS、Unity Hub 与同版本 Unity Editor，按课程需求补 Android 模块；使用有权访问私有仓库的 GitHub 账号登录。确认项目最新修改已推送后，在准备存放项目的位置执行：

```powershell
git lfs install
git clone https://github.com/ChenMengLR/VR_Project_2026_2.git
cd VR_Project_2026_2
git lfs pull
```

然后在 Unity Hub 中添加这个项目文件夹，使用 6000.3.24f1 打开。首次打开时需要重新生成 Library 缓存并还原 Packages，等待导入和编译完成。

根目录的 `Open-Course.cmd` 当前使用本机 `D:\Unity\Editors` 路径；如果另一台电脑的 Editor 位置不同，直接通过 Hub 打开即可。Meta 独立模拟器也需要在新电脑安装，不能只复制项目文件夹。

既有克隆后续同步可先运行 `git status`，在保存并提交自己的修改后再执行 `git pull` 与 `git lfs pull`。如果出现冲突，先确认两边改动，不要用强制覆盖代替判断。

## 六、当前状态与下一步

已经完成的环境与结构检查：

- Unity Hub 3.21.3、Unity Editor 6000.3.24f1、VS Code 1.138 与 C#、Unity 扩展已经安装。
- Android Build Support 已安装；OpenJDK 17.0.18+8、NDK r27c、SDK Build/Platform Tools 36.0.0、Command-line Tools 16.0、CMake 3.22.1 已补齐。关键文件和 Java、Javac、ADB、Clang、AAPT2、CMake、sdkmanager 的运行检查通过。
- Meta XR Simulator 205 已正式安装。
- 桌面项目采用 Universal 3D，URP 17.3、Input System 1.20、XRI 3.3.2、OpenXR 1.18、Meta All-in-One 205 已配置。
- Unity 批处理编译成功；第 2、3 周场景均已创建，结构检查均为 0 Missing Script。官方第 3 周原始示例文件保持不变。
- Git、Git LFS 与提交身份已初始化；个人私有仓库已经创建。

最终运行与交付记录：

- 运行验证结果：**第 2 周真实 Play Mode 的 79 项自动 API 交互检查全部通过，无运行错误。第 3 周已通过正式模拟控制器输入完成 StartZone → TargetZone 传送、抓握、抬升约 0.60 米和释放落地。** 第 3 周使用 Meta Operator 注入控制器输入，没有直接移动玩家或任务球来替代交互。
- 截图与视频：**第 2 周已生成 1280×720 的实际相机截图，以及约 15.9 秒、8 fps 的连续自动测试视频 CourseValidation/Week02_Automated_Play_Test.mp4。它记录真实场景中的自动 API 交互，不是键鼠实操录像；ExtensionObject 的 Inspector 界面截图仍需补充。第 3 周真机视频待获得 Quest 后完成**。
- 版本保存：**个人私有仓库为 ChenMengLR/VR_Project_2026_2。首次源代码检查点 ae12f0d 已推送；最终修复、运行报告与本文一起保存，准确提交号见随附部署结果及 GitHub 最新 main**。

本机曾遇到 Meta 205 原生日志的字符解码异常导致 Editor 退出。项目现有仅在 Windows Editor 生效的兼容脚本，保留原始日志并安全显示，重新 Play 的验证已通过；它不修改 SDK 包文件，也不进入 Quest 构建。正式练习已关闭自动测试使用的 Operator。

上课前建议亲自完成一次模拟器操作，理解抓取、激活、传送与释放的行为。自动测试报告用于检查项目功能，不能说明你是否已经熟悉按键。

当前没有 Quest，因此第 3 周真机验证仍需在获得设备后进行。小组与选题尚未确定，团队卡和小组真机演示随后安排。已经找到 Wang Haobin 的个人卡及第 2、3 周提交卡，两周均显示“未提交”。当前浏览器没有提供可编辑控件，Notion 连接器也无法访问该卡，因此尚未代填或上传；需先由课程管理员确认当前账号的编辑权限。文末提供了三个准确入口。

## 七、常见问题

**Hub 已登录，CLI 却提示未登录。**

本机 CLI 曾在保存 Windows 登录凭据时失败，但 Editor 的许可证验证、编译与运行已经成功。课程可通过 Hub 或 Open-Course.cmd 继续，不需要为这个辅助工具反复登录。CLI 的独立认证状态与 Editor 可用状态分别判断。

**项目首次打开很慢，是否失败？**

Unity 可能正在导入资源或下载包。查看底部进度与 Console；等待导入完成后再操作。不要仅因界面暂时不响应就连续创建多个项目。

**点击 Play 后按键没有反应。**

先点击 Game 窗口；确认当前使用哪一个模拟器以及是否已启用；检查控制目标是头部还是左、右控制器；最后核对本机 bindings。第 2、3 周按键不能混记。

**能看见方块，但抓不起来。**

在停止运行后核对 Collider、Rigidbody、抓取组件与 Interactor 设置，确认 `Is Trigger`、`Use Gravity`、`Is Kinematic` 符合对应练习要求。查看 Console 是否有报错。可见的物体并不一定是已配置的 Interactable。

**方块掉到地面下或一直落下。**

检查地面与物体的实体 Collider 是否存在、是否误设为 Trigger，以及物体初始位置是否嵌入其他碰撞体。不要仅通过关闭重力掩盖地面碰撞问题。

**抓取后颜色没有变化。**

检查 GrabVisualFeedback 是否已挂载、颜色字段和交互事件是否连接，核对是否修改了当前运行物体使用的材质或渲染器。

**方块被抓住还不停转。**

退出 Play，关闭练习用 RotateObject 组件的启用状态，保留组件本身，然后重新测试。

**Meta 传送线出现，但不能到目标区。**

检查 Movement Style 是否为 Teleport、目标区是否按 Hotspot 生成传送交互，以及是否正确完成“按住、指向、松开”。也要核对 Action 的控制器选择和当前 bindings。

**启动 Quest 后仍然显示模拟器。**

先停止运行并 Deactivate Meta XR Simulator，再检查 Meta Horizon Link 与真实设备连接，确认运行时使用正确的设备环境。

**为什么 GitHub 没有 Library 文件夹？**

Library 属于 Unity 可重新生成的缓存，忽略它是正常做法。需要保存的是资源及元数据、Packages 与 ProjectSettings。把缓存传上去会增加体积，也不能替代必要的项目源文件。

**电脑练习都通过后，能否直接提交第 3 周正式作业？**

还需要完成课程明确要求的 Quest 真机连续演示。电脑练习可以提前发现问题并保存进度，正式材料仍应反映真实设备与实际完成状态。

## 课程原文与提交入口

- [第 2 周课程原文](https://app.notion.com/p/3cf7c00fe5ad8118803cedb669e28446)
- [第 3 周课程原文](https://app.notion.com/p/3cf7c00fe5ad8111b891c9900e29a307)
- [Wang Haobin 个人作业卡](https://app.notion.com/p/Wang-Haobin-3d67c00fe5ad81288de5d923b15cf0d2)
- [第 2 周提交卡](https://app.notion.com/p/2-2-3dd7c00fe5ad8163920ed7bbd12cd78f)
- [第 3 周提交卡](https://app.notion.com/p/3-3-3dd7c00fe5ad8183a849f8b55c3b8133)
