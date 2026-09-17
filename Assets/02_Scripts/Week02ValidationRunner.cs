#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CourseSetup
{
    /// <summary>仅验证时创建。使用官方 XRI 输入和处理链，不调用反馈脚本方法。</summary>
    public sealed class Week02ValidationInteractor : XRBaseInputInteractor
    {
        public IXRInteractable target;
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();
            if (target != null) targets.Add(target);
        }
    }

    /// <summary>真实物理、交互状态与实际相机渲染；退出 Play Mode 即还原场景。</summary>
    public sealed class Week02ValidationRunner : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string evidence; }
        [Serializable] public sealed class Report
        {
            public string generatedUtc;
            public string unityVersion;
            public string graphicsDevice;
            public string mode = "Real Unity Play Mode; automatic XRI API/input interaction";
            public string humanSimulator = "NOT_TESTED: no human keyboard/mouse or headset demonstration";
            public string frameDirectory;
            public int videoFramesPerSecond = 8;
            public int capturedFrameCount;
            public float capturedSimulationSeconds;
            public bool passed;
            public List<Check> checks = new List<Check>();
            public List<string> screenshots = new List<string>();
            public List<string> unityErrors = new List<string>();
        }

        sealed class EventCounts
        {
            public int hoverEnter, hoverExit, selectEnter, selectExit, activate, deactivate;
            readonly XRGrabInteractable grab;
            public EventCounts(XRGrabInteractable source)
            {
                grab = source;
                grab.hoverEntered.AddListener(HoverEnter); grab.hoverExited.AddListener(HoverExit);
                grab.selectEntered.AddListener(SelectEnter); grab.selectExited.AddListener(SelectExit);
                grab.activated.AddListener(Activate); grab.deactivated.AddListener(Deactivate);
            }
            void HoverEnter(HoverEnterEventArgs _) { hoverEnter++; }
            void HoverExit(HoverExitEventArgs _) { hoverExit++; }
            void SelectEnter(SelectEnterEventArgs _) { selectEnter++; }
            void SelectExit(SelectExitEventArgs _) { selectExit++; }
            void Activate(ActivateEventArgs _) { activate++; }
            void Deactivate(DeactivateEventArgs _) { deactivate++; }
            public bool AllOccurred => hoverEnter > 0 && hoverExit > 0 && selectEnter > 0 && selectExit > 0 && activate > 0 && deactivate > 0;
            public override string ToString() => $"hoverEntered={hoverEnter}, hoverExited={hoverExit}, selectEntered={selectEnter}, selectExited={selectExit}, activated={activate}, deactivated={deactivate}";
            public void Dispose()
            {
                if (grab == null) return;
                grab.hoverEntered.RemoveListener(HoverEnter); grab.hoverExited.RemoveListener(HoverExit);
                grab.selectEntered.RemoveListener(SelectEnter); grab.selectExited.RemoveListener(SelectExit);
                grab.activated.RemoveListener(Activate); grab.deactivated.RemoveListener(Deactivate);
            }
        }

        Report report;
        string output;
        Action<bool> completed;
        XRInteractionManager manager;
        Week02ValidationInteractor hand;
        Camera observer;
        bool recordFrames;
        bool captureTimeConfigured;
        int originalCaptureFramerate;
        float firstFrameTime;
        readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
        readonly List<EventCounts> subscriptions = new List<EventCounts>();
        readonly List<GameObject> temporaryObjects = new List<GameObject>();

        public void Begin(string directory, Action<bool> onCompleted)
        {
            output = directory;
            completed = onCompleted;
            Directory.CreateDirectory(output);
            // 清除旧的“中止”标记，图片只列出本次实际生成的文件。
            string oldAborted = Path.Combine(output, "Week02_Runtime_ABORTED.txt");
            if (File.Exists(oldAborted)) File.Delete(oldAborted);
            report = new Report {
                generatedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                graphicsDevice = SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName
            };
            // 固定模拟时间步用于离线录制；不伪造位置或物理结果，完成后恢复设置。
            originalCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = report.videoFramesPerSecond;
            captureTimeConfigured = true;
            Application.logMessageReceived += CaptureError;
            StartCoroutine(RunGuarded());
        }

        IEnumerator RunGuarded()
        {
            // 展开嵌套迭代器，保证子步骤异常也写入报告并退出 Play Mode。
            var stack = new Stack<IEnumerator>();
            stack.Push(RunChecks());
            while (stack.Count > 0)
            {
                object current = null;
                bool more = false;
                Exception failure = null;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception ex) { failure = ex; }
                if (failure != null)
                {
                    Record("Validation exception", false, failure.ToString());
                    break;
                }
                if (!more) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Cleanup();
            Application.logMessageReceived -= CaptureError;
            Record("No Unity Error/Exception during validation", report.unityErrors.Count == 0,
                string.Join("\n", report.unityErrors));
            report.passed = report.checks.Count > 0 && report.checks.TrueForAll(item => item.passed);
            try { WriteReport(); }
            catch (Exception ex) { report.passed = false; Debug.LogException(ex); }
            completed?.Invoke(report.passed);
        }

        IEnumerator RunChecks()
        {
            manager = FindFirstObjectByType<XRInteractionManager>();
            if (manager == null) throw new InvalidOperationException("缺少 XRInteractionManager。");

            // 避免真实 Rig 与本次自动测试争夺物体，仅在 Play Mode 暂停其输入。
            foreach (var behaviour in FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
                DisableDuringTest(behaviour);
            foreach (var behaviour in FindObjectsByType<XRInteractionSimulator>(FindObjectsSortMode.None))
                DisableDuringTest(behaviour);

            string[] names = { "PracticeCube", "GrabCube_02", "ExtensionObject" };
            var grabs = new List<XRGrabInteractable>();
            var initialY = new List<float>();
            foreach (string objectName in names)
            {
                var go = GameObject.Find(objectName);
                if (go == null) throw new InvalidOperationException("缺少物体 " + objectName);
                var grab = go.GetComponent<XRGrabInteractable>();
                if (grab == null) throw new InvalidOperationException(objectName + " 缺少 XRGrabInteractable。");
                grabs.Add(grab); initialY.Add(go.transform.position.y);
                var body = go.GetComponent<Rigidbody>();
                var collider = go.GetComponent<Collider>();
                Record(objectName + " dynamic Rigidbody and solid Collider", body != null && collider != null &&
                       body.useGravity && !body.isKinematic && !collider.isTrigger,
                    body == null ? "Rigidbody missing" : $"gravity={body.useGravity}, kinematic={body.isKinematic}, mass={body.mass:F2}");
            }
            yield return new WaitForSeconds(2.2f);
            for (int i = 0; i < grabs.Count; i++)
            {
                var grab = grabs[i];
                var body = grab.GetComponent<Rigidbody>();
                float fall = initialY[i] - grab.transform.position.y;
                Record(grab.name + " gravity moved object down", fall > 0.15f,
                    $"startY={initialY[i]:F3}, endY={grab.transform.position.y:F3}, drop={fall:F3} m");
                CheckGround(grab, "initial floor landing");
            }

            CreateObserver();
            Capture("01_initial_landed");
            CreateInteractor();
            yield return null;
            foreach (var grab in grabs) yield return CheckInteraction(grab, grabs);
            Capture("09_all_released");
        }

        IEnumerator CheckInteraction(XRGrabInteractable grab, List<XRGrabInteractable> allGrabs)
        {
            bool hasFeedback = grab.TryGetComponent<GrabVisualFeedback>(out var feedback);
            if (grab.name != "ExtensionObject") Record(grab.name + " feedback exists", hasFeedback, "GrabVisualFeedback component");
            var counts = new EventCounts(grab);
            subscriptions.Add(counts);
            var peers = new Dictionary<XRGrabInteractable, Vector3>();
            foreach (var other in allGrabs) if (other != grab) peers.Add(other, other.transform.position);
            hand.transform.SetPositionAndRotation(grab.transform.position, Quaternion.identity);
            hand.target = grab;
            hand.allowSelect = false;
            hand.allowHover = true;
            manager.HoverEnter((IXRHoverInteractor)hand, (IXRHoverInteractable)grab);
            yield return null;
            Record(grab.name + " manager HoverEnter", grab.isHovered && counts.hoverEnter > 0, counts.ToString());
            if (hasFeedback) CheckColor(grab, feedback.hoverColor, "hover: yellow");
            Capture(grab.name + "_02_hover");

            hand.selectInput.QueueManualState(true, 1f);
            yield return null;
            yield return null;
            hand.allowSelect = true;
            manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)grab);
            yield return new WaitForSeconds(0.25f);
            Record(grab.name + " manager SelectEnter", grab.isSelected && counts.selectEnter > 0 && hand.hasSelection, counts.ToString());
            if (hasFeedback) CheckColor(grab, feedback.selectColor, "selection takes priority over hover: green");

            Vector3 started = grab.transform.position;
            Vector3 handStart = hand.transform.position;
            Vector3 handEnd = handStart + new Vector3(0f, 1.1f, 0.55f);
            float elapsed = 0;
            while (elapsed < 0.65f)
            {
                elapsed += Time.deltaTime;
                hand.transform.position = Vector3.Lerp(handStart, handEnd, Mathf.Clamp01(elapsed / 0.65f));
                yield return null;
            }
            yield return new WaitForSeconds(0.35f);
            float moved = Vector3.Distance(started, grab.transform.position);
            float attachError = Vector3.Distance(hand.GetAttachTransform(grab).position, grab.GetAttachTransform(hand).position);
            Record(grab.name + " actual XRI follow", grab.isSelected && moved > 0.8f && attachError < 0.18f,
                $"objectMoved={moved:F3} m, attachError={attachError:F3} m; only interactor transform moved");
            Capture(grab.name + "_03_selected_follow");

            // Activate/Deactivate 无 Manager 公共入口；由官方 InputInteractor 处理手动输入。
            hand.activateInput.QueueManualState(true, 1f);
            yield return null; yield return null; yield return null;
            Record(grab.name + " official input Activate", counts.activate > 0, counts.ToString());
            if (hasFeedback) CheckColor(grab, feedback.activateColor, "activation takes priority: magenta");
            Capture(grab.name + "_04_activated");

            hand.allowHover = false;
            if (grab.isHovered) manager.HoverExit((IXRHoverInteractor)hand, (IXRHoverInteractable)grab);
            yield return null;
            Record(grab.name + " selected and activated after hover exit", grab.isSelected && !grab.isHovered, counts.ToString());
            if (hasFeedback) CheckColor(grab, feedback.activateColor, "hover exit must preserve activation: magenta");

            hand.activateInput.QueueManualState(false, 0f);
            yield return null; yield return null; yield return null;
            Record(grab.name + " official input Deactivate", counts.deactivate > 0, counts.ToString());
            if (hasFeedback) CheckColor(grab, feedback.selectColor, "deactivated but selected: green");

            hand.allowHover = true;
            manager.HoverEnter((IXRHoverInteractor)hand, (IXRHoverInteractable)grab);
            yield return null;
            if (hasFeedback) CheckColor(grab, feedback.selectColor, "hover enter must preserve selection: green");
            float releaseY = grab.transform.position.y;
            hand.allowSelect = false;
            hand.selectInput.QueueManualState(false, 0f);
            manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)grab);
            yield return null;
            Record(grab.name + " manager SelectExit", !grab.isSelected && counts.selectExit > 0 && !hand.hasSelection, counts.ToString());
            if (hasFeedback) CheckColor(grab, feedback.hoverColor, "released while hovered: yellow");

            hand.allowHover = false;
            manager.HoverExit((IXRHoverInteractor)hand, (IXRHoverInteractable)grab);
            hand.target = null;
            yield return null;
            if (hasFeedback) CheckColor(grab, feedback.normalColor, "hover exited after release: cyan");
            Record(grab.name + " all six real XRI events", counts.AllOccurred, counts.ToString());
            yield return new WaitForSeconds(2.2f);
            float fall = releaseY - grab.transform.position.y;
            Record(grab.name + " released gravity", fall > 0.5f && grab.GetComponent<Rigidbody>().useGravity,
                $"releaseY={releaseY:F3}, landedY={grab.transform.position.y:F3}, drop={fall:F3} m");
            CheckGround(grab, "released floor landing");
            foreach (var peer in peers)
            {
                float distance = Vector3.Distance(peer.Value, peer.Key.transform.position);
                Record(grab.name + " independent from " + peer.Key.name, distance < 0.12f,
                    $"other object moved={distance:F3} m");
            }
            Capture(grab.name + "_05_released_landed");
        }

        void CreateInteractor()
        {
            var go = new GameObject("__Validation_XRI_InputInteractor");
            go.SetActive(false);
            temporaryObjects.Add(go);
            hand = go.AddComponent<Week02ValidationInteractor>();
            hand.interactionManager = manager;
            hand.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
            hand.selectInput = new XRInputButtonReader("Validation Select", inputSourceMode: XRInputButtonReader.InputSourceMode.ManualValue);
            hand.activateInput = new XRInputButtonReader("Validation Activate", inputSourceMode: XRInputButtonReader.InputSourceMode.ManualValue);
            hand.allowHover = false; hand.allowSelect = false; hand.allowActivate = true;
            go.SetActive(true);
        }

        void CreateObserver()
        {
            var go = new GameObject("__Validation_RenderCamera");
            temporaryObjects.Add(go);
            observer = go.AddComponent<Camera>();
            observer.enabled = false;
            observer.stereoTargetEye = StereoTargetEyeMask.None;
            observer.clearFlags = CameraClearFlags.SolidColor;
            observer.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
            observer.fieldOfView = 50f;
            observer.aspect = 1280f / 720f;
            observer.nearClipPlane = 0.05f;
            observer.farClipPlane = 40f;
            observer.transform.position = new Vector3(4f, 3.2f, -3.5f);
            observer.transform.LookAt(new Vector3(0f, 0.8f, 2.2f));
            observer.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            report.frameDirectory = Path.GetFullPath(Path.Combine(output, "..", "Frames", "Week02",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(report.frameDirectory);
            firstFrameTime = Time.time;
            recordFrames = true;
        }

        void Capture(string filename)
        {
            try
            {
                string image = filename + ".png";
                RenderImage(Path.Combine(output, image));
                report.screenshots.Add(image);
                Record("Camera render " + filename, true, "Actual URP Camera -> RenderTexture -> PNG, 1280 x 720");
            }
            catch (Exception ex) { Record("Camera render " + filename, false, ex.Message); }
        }

        void LateUpdate()
        {
            if (!recordFrames || observer == null) return;
            try
            {
                string filename = "frame-" + report.capturedFrameCount.ToString("D5") + ".png";
                RenderImage(Path.Combine(report.frameDirectory, filename));
                report.capturedFrameCount++;
                report.capturedSimulationSeconds = Time.time - firstFrameTime;
            }
            catch (Exception ex)
            {
                recordFrames = false;
                Record("Continuous camera frame capture", false, ex.Message);
            }
        }

        void RenderImage(string destination)
        {
            RenderTexture target = null;
            Texture2D pixels = null;
            var previous = RenderTexture.active;
            try
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                    throw new InvalidOperationException("Null graphics device：本次不能生成真实截图；请去掉 -nographics 后运行。");
                target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                target.Create();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(observer, request))
                    throw new InvalidOperationException("当前渲染管线不支持 URP SingleCameraRequest。");
                RenderPipeline.SubmitRenderRequest(observer, request);
                RenderTexture.active = target;
                pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(destination, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) Destroy(pixels);
                if (target != null) { target.Release(); Destroy(target); }
            }
        }

        void CheckGround(XRGrabInteractable grab, string label)
        {
            var body = grab.GetComponent<Rigidbody>();
            float bottom = grab.GetComponent<Collider>().bounds.min.y;
            float speed = body.linearVelocity.magnitude;
            Record(grab.name + " " + label, Mathf.Abs(bottom) < 0.06f && speed < 0.15f,
                $"collider.bottomY={bottom:F4} m, linearSpeed={speed:F4} m/s, sleeping={body.IsSleeping()}");
        }

        void CheckColor(XRGrabInteractable grab, Color expected, string label)
        {
            var properties = new MaterialPropertyBlock();
            grab.GetComponent<Renderer>().GetPropertyBlock(properties);
            Color actual = properties.GetColor(Shader.PropertyToID("_BaseColor"));
            float difference = Vector4.Distance(actual, expected);
            Record(grab.name + " " + label, difference < 0.015f,
                $"Renderer _BaseColor actual={actual}, expected={expected}; XRI hovered={grab.isHovered}, selected={grab.isSelected}");
        }

        void DisableDuringTest(Behaviour behaviour)
        {
            if (!behaviour.enabled) return;
            disabledBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }

        void Cleanup()
        {
            recordFrames = false;
            if (captureTimeConfigured) Time.captureFramerate = originalCaptureFramerate;
            Record("Continuous camera frames", report.capturedFrameCount > 0,
                $"frames={report.capturedFrameCount}, fps={report.videoFramesPerSecond}, simulated seconds={report.capturedSimulationSeconds:F3}; directory={report.frameDirectory}");
            foreach (var subscription in subscriptions) subscription.Dispose();
            foreach (var temporary in temporaryObjects) if (temporary != null) Destroy(temporary);
            foreach (var behaviour in disabledBehaviours) if (behaviour != null) behaviour.enabled = true;
            // 测试从不保存场景；停止 Play Mode 会还原位置、刚体和运行时对象。
        }

        void CaptureError(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (report.unityErrors.Count < 20) report.unityErrors.Add(type + ": " + message);
        }

        void Record(string name, bool passed, string evidence)
        {
            report.checks.Add(new Check { name = name, passed = passed, evidence = evidence });
        }

        void WriteReport()
        {
            File.WriteAllText(Path.Combine(output, "Week02_Runtime.json"), JsonUtility.ToJson(report, true));
            var text = new StringBuilder();
            text.AppendLine("# Week 02 真实 Play Mode 自动验证");
            text.AppendLine();
            text.AppendLine("结果：**" + (report.passed ? "RUNTIME_API_PASS" : "RUNTIME_API_FAIL") + "**");
            text.AppendLine("时间 (UTC)：" + report.generatedUtc + "；Unity：" + report.unityVersion);
            text.AppendLine("渲染设备：" + report.graphicsDevice);
            text.AppendLine();
            text.AppendLine("验证范围：实际 Unity Play Mode、PhysX 重力与落地、官方 XRInteractionManager Hover/Select/Release、官方 XRBaseInputInteractor ManualValue 输入激活/取消激活、实际 Renderer 属性和 URP Camera 渲染。没有直接调用 GrabVisualFeedback 方法或 Invoke 其事件。");
            text.AppendLine();
            text.AppendLine("**未验证：真人键鼠操作 XR Interaction Simulator、头显/手柄硬件、学生个人演示与课堂录屏。这份报告不能替代上述演示或作业提交。**");
            text.AppendLine();
            text.AppendLine("Rig Interactor 和 Simulator 仅在自动测试期间暂停，以避免争抢；结束时恢复，并退出 Play Mode。测试没有保存运行时场景修改。");
            text.AppendLine();
            text.AppendLine($"录像来源：实际观察相机连续帧，Time.captureFramerate={report.videoFramesPerSecond} 固定模拟时间步；共 {report.capturedFrameCount} 帧，约 {report.capturedFrameCount / (float)report.videoFramesPerSecond:F2} 秒。录制墙钟耗时可能更长。此录像应标为“自动 API 交互测试”，不能标为学生亲自操作录像。");
            text.AppendLine("帧目录：`" + report.frameDirectory + "`");
            text.AppendLine();
            foreach (var check in report.checks)
                text.AppendLine("- [" + (check.passed ? "x" : " ") + "] **" + check.name + "** — " + check.evidence.Replace("\n", " ").Replace("\r", " "));
            text.AppendLine();
            text.AppendLine("## 本次实际相机截图");
            text.AppendLine();
            foreach (string image in report.screenshots) text.AppendLine("- [" + image + "](" + image + ")");
            File.WriteAllText(Path.Combine(output, "Week02_Runtime.md"), text.ToString());
        }
    }
}
#endif
