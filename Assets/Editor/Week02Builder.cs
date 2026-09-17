#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace CourseSetup
{
    /// <summary>用 Unity API 和官方样例生成课程场景；不写入手工 GUID。</summary>
    public static class Week02Builder
    {
        public const string ScenePath = "Assets/01_Scenes/Week02_XR_Basics.unity";
        private const string XriPackage = "com.unity.xr.interaction.toolkit";
        private const string XriVersion = "3.3.2";
        private const string ResumeKey = "CourseSetup.Week02Builder.Resume";
        private static readonly string[] CourseFolders = {
            "01_Scenes", "02_Scripts", "03_Prefabs", "04_Images",
            "05_Models", "06_Animations", "07_Fonts", "08_Sounds"
        };
        private static readonly string[] SampleNames = { "Starter Assets", "XR Interaction Simulator" };

        [MenuItem("Course/Week 02/1. Import Official Samples")]
        public static void ImportSamples()
        {
            RequirePackageVersion();
            bool changed = ImportMissingSamples();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            WriteStatus(changed ? "样例已导入；请等待脚本编译完成后执行 Build。" : "所需官方样例已导入。", false);
        }

        [MenuItem("Course/Week 02/2. Build Course Scene")]
        public static void Build()
        {
            EnsureNoUnsavedScenes();
            WriteStatus("正在构建；本次验证尚未完成。", false);
            RequirePackageVersion();
            foreach (string folder in CourseFolders) EnsureFolder("Assets/" + folder);
            EnsureFolder("Assets/03_Prefabs/Materials");

            if (ImportMissingSamples())
            {
                WriteStatus("官方样例刚导入，场景尚未构建；等待编译后重新执行 Build。", false);
                if (Application.isBatchMode)
                    throw new InvalidOperationException("样例需要先编译。先调用 CourseSetup.Week02Builder.ImportSamples，下一次 Editor 进程再调用 Build。");
                SessionState.SetBool(ResumeKey, true);
                AssetDatabase.Refresh();
                EditorApplication.delayCall += ResumeAfterCompile;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Unity 正在导入或编译；完成后再构建。不会覆盖当前场景。");

            var pipeline = GraphicsSettings.currentRenderPipeline ?? GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null || !pipeline.GetType().Name.Contains("UniversalRenderPipeline"))
                throw new InvalidOperationException("当前不是 Universal 3D / URP 项目，请先配置 URP。不会用 Built-in 材质冒充课程环境。");

            string rigPath = FindOfficialPrefab("Starter Assets", "XR Origin (XR Rig)");
            string simulatorPath = FindOfficialPrefab("XR Interaction Simulator", "XR Interaction Simulator");
            GameObject rigAsset = LoadReadyPrefab(rigPath);
            GameObject simulatorAsset = LoadReadyPrefab(simulatorPath);
            BackupExistingScene();

            // 只在所有打开的场景均已保存时切换；原有已保存文件不会被本步骤改写。
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, scene);
            var simulatorObject = (GameObject)PrefabUtility.InstantiatePrefab(simulatorAsset, scene);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            simulatorObject.name = "XR Interaction Simulator";
            XROrigin origin = rig.GetComponent<XROrigin>();
            XRInteractionSimulator simulator = simulatorObject.GetComponent<XRInteractionSimulator>();
            if (origin == null || origin.Camera == null || simulator == null)
                throw new InvalidOperationException("官方 prefab 缺少 XROrigin / Camera / XRInteractionSimulator，停止构建。");
            simulator.cameraTransform = origin.Camera.transform;
            origin.Camera.tag = "MainCamera";
            origin.Camera.clearFlags = CameraClearFlags.SolidColor;
            origin.Camera.backgroundColor = new Color(0.055f, 0.08f, 0.13f);

            XRInteractionManager[] existingManagers = ComponentsInScene<XRInteractionManager>(scene);
            if (existingManagers.Length > 1) throw new InvalidOperationException("官方 prefab 意外包含多个 Interaction Manager。");
            XRInteractionManager manager = existingManagers.Length == 1 ? existingManagers[0]
                : new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            foreach (XRBaseInteractor interactor in rig.GetComponentsInChildren<XRBaseInteractor>(true))
                interactor.interactionManager = manager;

            // 场景从空场景开始，因此只有 XR Rig 内相机，不保留普通 Main Camera。
            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.53f);
            RenderSettings.sun = light;

            Material floorMaterial = EnsureMaterial("Floor_Gray", new Color(0.22f, 0.27f, 0.34f));
            Material cyanMaterial = EnsureMaterial("Grab_Cyan", Color.cyan);
            Material orangeMaterial = EnsureMaterial("Extension_Orange", new Color(1f, 0.42f, 0.08f));
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            floor.GetComponent<Collider>().isTrigger = false;
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            GameObject cube = MakeGrabObject("PracticeCube", PrimitiveType.Cube,
                new Vector3(0f, 1f, 2f), Vector3.one, 1f, cyanMaterial, manager, true);
            RotateObject rotation = cube.AddComponent<RotateObject>();
            rotation.rotationSpeed = 45f;
            rotation.enabled = false;
            MakeGrabObject("GrabCube_02", PrimitiveType.Cube,
                new Vector3(1.35f, 1f, 2.15f), Vector3.one * 0.8f, 1f, cyanMaterial, manager, true);
            // 扩展任务独立创建球体：形状、大小、位置、材质、质量均与基础立方体不同。
            MakeGrabObject("ExtensionObject", PrimitiveType.Sphere,
                new Vector3(-1.35f, 1.3f, 2.35f), Vector3.one * 0.65f, 2.5f, orangeMaterial, manager, false);

            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Unity 未能保存课程场景。");

            List<string> issues;
            string report = BuildValidation(scene, rigPath, simulatorPath, out issues);
            WriteReport(report, issues.Count == 0);
            if (issues.Count > 0)
                throw new InvalidOperationException("场景已保存，但静态结构验证未通过：" + string.Join("；", issues));
            Debug.Log("Week02 场景已保存，静态结构验证通过。动态交互、真实头显和录屏仍未验证。报告：CourseValidation/Week02_Structure.md");
        }

        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (SessionState.GetBool(ResumeKey, false)) EditorApplication.delayCall += ResumeAfterCompile;
        }

        private static void ResumeAfterCompile()
        {
            if (!SessionState.GetBool(ResumeKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ResumeAfterCompile;
                return;
            }
            SessionState.SetBool(ResumeKey, false);
            try { Build(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        private static void RequirePackageVersion()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(XRInteractionManager).Assembly);
            if (package == null || package.name != XriPackage || package.version != XriVersion)
                throw new InvalidOperationException("本构建器需要已解析的 XR Interaction Toolkit 3.3.2。");
        }

        private static bool ImportMissingSamples()
        {
            Sample[] samples = (Sample.FindByPackage(XriPackage, XriVersion) ?? Enumerable.Empty<Sample>()).ToArray();
            bool changed = false;
            foreach (string name in SampleNames)
            {
                Sample[] matches = samples.Where(item => item.displayName == name).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("找不到唯一官方样例：" + name);
                Sample sample = matches[0];
                if (sample.isImported) continue;
                // 不覆盖已经导入的样例，保留用户对样例的修改。
                if (!sample.Import((Sample.ImportOptions)0))
                    throw new IOException("官方样例导入失败：" + name);
                changed = true;
            }
            return changed;
        }

        private static string FindOfficialPrefab(string sampleName, string exactName)
        {
            string root = "Assets/Samples/XR Interaction Toolkit/" + XriVersion + "/" + sampleName;
            if (!AssetDatabase.IsValidFolder(root)) throw new DirectoryNotFoundException(root);
            string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == exactName).ToArray();
            if (paths.Length != 1) throw new InvalidOperationException("官方样例 prefab 不是唯一匹配：" + exactName);
            return paths[0];
        }

        private static GameObject LoadReadyPrefab(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException("无法加载官方 prefab：" + path);
            int missing = asset.GetComponentsInChildren<Transform>(true)
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            if (missing != 0) throw new InvalidOperationException("官方 prefab 存在未解析脚本；等待样例编译完成：" + path);
            return asset;
        }

        private static void EnsureNoUnsavedScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty || (string.IsNullOrEmpty(scene.path) && scene.rootCount > 0))
                    throw new InvalidOperationException("请先保存或自行关闭未保存场景：" + scene.name + "。构建器不会自动丢弃它。");
            }
        }

        private static void BackupExistingScene()
        {
            if (!File.Exists(ScenePath)) return;
            string backupRoot = Path.Combine(ProjectRoot, "CourseValidation", "SceneBackups");
            Directory.CreateDirectory(backupRoot);
            File.Copy(ScenePath, Path.Combine(backupRoot, "Week02_XR_Basics_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff") + ".unity"));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Material EnsureMaterial(string name, Color color)
        {
            string path = "Assets/03_Prefabs/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit Shader 不可用。");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject MakeGrabObject(string name, PrimitiveType shape, Vector3 position,
            Vector3 scale, float mass, Material material, XRInteractionManager manager, bool feedbackEnabled)
        {
            GameObject item = GameObject.CreatePrimitive(shape);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            Collider collider = item.GetComponent<Collider>();
            collider.isTrigger = false;
            item.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.isKinematic = false;
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            XRGrabInteractable grab = item.AddComponent<XRGrabInteractable>();
            grab.interactionManager = manager;
            grab.colliders.Clear();
            grab.colliders.Add(collider);
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            if (feedbackEnabled)
            {
                GrabVisualFeedback feedback = item.AddComponent<GrabVisualFeedback>();
                UnityEventTools.AddVoidPersistentListener(grab.hoverEntered, feedback.OnHoverEntered);
                UnityEventTools.AddVoidPersistentListener(grab.hoverExited, feedback.OnHoverExited);
                UnityEventTools.AddVoidPersistentListener(grab.selectEntered, feedback.OnSelectEntered);
                UnityEventTools.AddVoidPersistentListener(grab.selectExited, feedback.OnSelectExited);
                UnityEventTools.AddVoidPersistentListener(grab.activated, feedback.OnActivated);
                UnityEventTools.AddVoidPersistentListener(grab.deactivated, feedback.OnDeactivated);
            }
            return item;
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static string BuildValidation(Scene scene, string rigPath, string simulatorPath, out List<string> issues)
        {
            issues = new List<string>();
            var text = new StringBuilder();
            text.AppendLine("# Week 02 场景静态结构验证");
            text.AppendLine("生成时间（UTC）：" + DateTime.UtcNow.ToString("O"));
            text.AppendLine("Unity：" + Application.unityVersion + "；XRI：" + XriVersion);
            text.AppendLine("场景：" + ScenePath);
            text.AppendLine("官方 XR Origin 来源：" + rigPath);
            text.AppendLine("官方 Simulator 来源：" + simulatorPath);
            text.AppendLine();
            int origins = ComponentsInScene<XROrigin>(scene).Length;
            int simulators = ComponentsInScene<XRInteractionSimulator>(scene).Length;
            int managers = ComponentsInScene<XRInteractionManager>(scene).Length;
            Camera[] cameras = ComponentsInScene<Camera>(scene);
            text.AppendLine($"XR Origin={origins}；Simulator={simulators}；Manager={managers}；Camera={cameras.Length}");
            if (origins != 1 || simulators != 1 || managers != 1) issues.Add("XR Origin / Simulator / Manager 数量不是各 1");
            if (cameras.Length != 1 || cameras[0].GetComponentInParent<XROrigin>() == null) issues.Add("存在普通相机或缺少 XR Rig 相机");
            int missing = ComponentsInScene<Transform>(scene).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            text.AppendLine("Missing Script 数量：" + missing);
            if (missing != 0) issues.Add("存在 Missing Script");
            GameObject floor = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "Floor");
            Collider floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
            bool floorReady = floorCollider != null && !floorCollider.isTrigger && floor.GetComponent<Rigidbody>() == null;
            text.AppendLine("Floor 静态非 Trigger 碰撞地面：" + floorReady);
            if (!floorReady) issues.Add("Floor 静态地面不完整");
            XRGrabInteractable[] allGrabs = ComponentsInScene<XRGrabInteractable>(scene);
            text.AppendLine("可抓取物体数量：" + allGrabs.Length);
            if (allGrabs.Length != 3) issues.Add("可抓取物体数量不是 3");
            foreach (string folder in CourseFolders)
            {
                bool exists = AssetDatabase.IsValidFolder("Assets/" + folder);
                text.AppendLine("目录 " + folder + "：" + exists);
                if (!exists) issues.Add("缺目录 " + folder);
            }
            InputActionManager[] inputManagers = ComponentsInScene<InputActionManager>(scene);
            if (inputManagers.Length < 2) issues.Add("XR Rig 或 Simulator 缺 InputActionManager");
            foreach (InputActionManager input in inputManagers)
            {
                text.AppendLine("InputActionManager：" + input.name + "；资源数量=" + input.actionAssets.Count);
                if (input.actionAssets.Count == 0 || input.actionAssets.Any(asset => asset == null)) issues.Add("InputActionManager 缺输入资源");
            }
            foreach (string name in new[] { "PracticeCube", "GrabCube_02", "ExtensionObject" })
            {
                GameObject item = scene.GetRootGameObjects().SingleOrDefault(root => root.name == name);
                if (item == null) { issues.Add("缺物体 " + name); continue; }
                Rigidbody body = item.GetComponent<Rigidbody>();
                Collider collider = item.GetComponent<Collider>();
                XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
                Material material = item.GetComponent<Renderer>()?.sharedMaterial;
                text.AppendLine($"{name}：位置={item.transform.position}；缩放={item.transform.localScale}；Collider={collider?.GetType().Name}；Trigger={collider?.isTrigger}；Gravity={body?.useGravity}；Kinematic={body?.isKinematic}；Mass={body?.mass}；Material={material?.name}；XRGrab={grab != null}");
                if (body == null || !body.useGravity || body.isKinematic || collider == null || collider.isTrigger || grab == null) issues.Add(name + " 物理或抓取配置不完整");
                if (name == "PracticeCube")
                {
                    if (Vector3.Distance(item.transform.position, new Vector3(0f, 1f, 2f)) > 0.001f) issues.Add("PracticeCube 初始位置不正确");
                    RotateObject rotation = item.GetComponent<RotateObject>();
                    text.AppendLine("RotateObject：存在=" + (rotation != null) + "；Enabled=" + rotation?.enabled + "；Speed=" + rotation?.rotationSpeed);
                    if (rotation == null || rotation.enabled || !Mathf.Approximately(rotation.rotationSpeed, 45f)) issues.Add("旋转组件最终状态不正确");
                }
                if (name != "ExtensionObject" && grab != null)
                {
                    GrabVisualFeedback feedback = item.GetComponent<GrabVisualFeedback>();
                    UnityEventBase[] events = { grab.hoverEntered, grab.hoverExited, grab.selectEntered, grab.selectExited, grab.activated, grab.deactivated };
                    string[] methods = { "OnHoverEntered", "OnHoverExited", "OnSelectEntered", "OnSelectExited", "OnActivated", "OnDeactivated" };
                    for (int i = 0; i < events.Length; i++)
                    {
                        bool linked = events[i].GetPersistentEventCount() == 1 && events[i].GetPersistentTarget(0) == feedback && events[i].GetPersistentMethodName(0) == methods[i];
                        text.AppendLine(name + " / " + methods[i] + "：" + linked);
                        if (!linked || feedback == null) issues.Add(name + " 缺事件连接 " + methods[i]);
                    }
                }
            }
            GameObject practice = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "PracticeCube");
            GameObject extension = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "ExtensionObject");
            if (practice != null && extension != null)
            {
                int differences = 0;
                if (practice.GetComponent<Collider>()?.GetType() != extension.GetComponent<Collider>()?.GetType()) differences++;
                if (practice.transform.position != extension.transform.position) differences++;
                if (practice.transform.localScale != extension.transform.localScale) differences++;
                if (practice.GetComponent<Renderer>()?.sharedMaterial != extension.GetComponent<Renderer>()?.sharedMaterial) differences++;
                if (practice.GetComponent<Rigidbody>()?.mass != extension.GetComponent<Rigidbody>()?.mass) differences++;
                text.AppendLine("扩展物体与基础物体的可观察配置差异数：" + differences);
                if (extension.GetComponent<SphereCollider>() == null || differences < 2) issues.Add("扩展任务球体/至少两项差异未满足");
            }
            text.AppendLine();
            text.AppendLine("静态结构结论：" + (issues.Count == 0 ? "通过" : "未通过"));
            foreach (string issue in issues) text.AppendLine("- " + issue);
            text.AppendLine("尚未验证：Play Mode 运行、鼠标键盘模拟抓取、颜色切换、抛掷、Android 构建、真实头显及课程提交。");
            return text.ToString();
        }

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static void WriteStatus(string status, bool passed)
        {
            WriteReport("# Week 02 准备状态\n\n" + status + "\n\n尚未完成场景动态测试。\n", passed);
        }

        private static void WriteReport(string report, bool passed)
        {
            string directory = Path.Combine(ProjectRoot, "CourseValidation");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "Week02_Structure.md"), report, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "Week02_StaticStatus.txt"), passed ? "STATIC_PASS\nDYNAMIC_NOT_TESTED\n" : "STATIC_NOT_COMPLETE\nDYNAMIC_NOT_TESTED\n", new UTF8Encoding(false));
        }
    }
}
#endif
