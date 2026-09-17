// Suggested temporary Editor-only Play Mode smoke. Not yet compiled or executed.
// Put outside an Editor folder when the Unity operation lock is released.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

public sealed class Week02KeyboardInputSmoke : MonoBehaviour
{
    [Serializable] public sealed class Observation
    {
        public string phase;
        public int frame;
        public bool keyboardG, keyboardT, simulatorGrip, simulatorTrigger;
        public bool controllerGrip, controllerTrigger, hovered, selected;
        public int selectEvents, activateEvents, deactivateEvents, releaseEvents;
    }

    [Serializable] public sealed class Report
    {
        public string scope = "Synthetic Keyboard -> official XRI simulator -> official controller -> real interaction events; not manual student demonstration";
        public string status = "running", error = "";
        public List<Observation> observations = new List<Observation>();
    }

    public static string LastResultJson { get; private set; } = "not started";
    enum Phase { Settle, Hover, Select, Activate, Deactivate, Release, Cleanup }
    Phase phase;
    Report report = new Report();
    string reportPath;
    Keyboard keyboard;
    XRSimulatedController controller;
    XRInteractionSimulator simulator;
    NearFarInteractor right;
    XRGrabInteractable target;
    XROrigin origin;
    Rigidbody body;
    Vector3 oldOriginPosition, oldTargetPosition, oldVelocity, oldAngularVelocity;
    Quaternion oldTargetRotation;
    InputSettings.BackgroundBehavior oldBackground;
    InputSettings.EditorInputBehaviorInPlayMode oldEditorFocus;
    bool settingsCaptured, geometryCaptured, subscribed, finished, success;
    int phaseFrame, selectedCount, activatedCount, deactivatedCount, releasedCount;
    float phaseStart;

    [MenuItem("Course/Week 02/Start keyboard input smoke in current Play Mode")]
    public static void MenuRun() { Debug.Log(StartSmoke()); }

    public static string StartSmoke(string outputJsonPath = null)
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "Week02_XR_Basics")
            throw new InvalidOperationException("Open Week02_XR_Basics in a clean Play Mode session first.");
        if (FindObjectsByType<Week02KeyboardInputSmoke>(FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("A keyboard smoke is already running.");
        if (FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(x =>
            x != null && x.isActiveAndEnabled && x.GetType().Name == "Week02ValidationRunner"))
            throw new InvalidOperationException("Stop the previous Manager API runner; it competes with the simulator.");
        if (Keyboard.current != null && (Keyboard.current.gKey.isPressed ||
            Keyboard.current.tKey.isPressed || Keyboard.current.shiftKey.isPressed))
            throw new InvalidOperationException("Release physical G/T/Shift before starting.");
        var host = new GameObject("__Week02_KeyboardSmoke_Temporary");
        host.hideFlags = HideFlags.DontSave;
        var runner = host.AddComponent<Week02KeyboardInputSmoke>();
        runner.reportPath = outputJsonPath;
        LastResultJson = "running";
        try { runner.Begin(); }
        catch (Exception ex) { runner.report.error = ex.Message; runner.FinalizeRun(false); }
        return LastResultJson;
    }

    void Begin()
    {
        simulator = FindObjectsByType<XRInteractionSimulator>(FindObjectsSortMode.None)
            .Single(x => x.isActiveAndEnabled);
        if (simulator.deviceLifecycleManager == null ||
            simulator.deviceLifecycleManager.deviceMode != SimulatedDeviceLifecycleManager.DeviceMode.Controller)
            throw new InvalidOperationException("The official simulator must be active in Controller mode.");
        right = FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None)
            .Single(x => x.isActiveAndEnabled && HierarchyPath(x.transform).IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0);
        target = GameObject.Find("PracticeCube")?.GetComponent<XRGrabInteractable>();
        origin = right.GetComponentInParent<XROrigin>();
        if (target == null || origin == null || right.curveOrigin == null)
            throw new InvalidOperationException("PracticeCube, right NearFarInteractor, curve origin, and XROrigin are required.");
        if (target.isSelected || right.hasSelection)
            throw new InvalidOperationException("Start with no object selected.");

        body = target.GetComponent<Rigidbody>();
        oldOriginPosition = origin.transform.position;
        oldTargetPosition = target.transform.position;
        oldTargetRotation = target.transform.rotation;
        if (body != null) { oldVelocity = body.linearVelocity; oldAngularVelocity = body.angularVelocity; }
        geometryCaptured = true;
        oldBackground = InputSystem.settings.backgroundBehavior;
        oldEditorFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
        settingsCaptured = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>("CourseSmokeKeyboard");
        target.selectEntered.AddListener(OnSelected);
        target.activated.AddListener(OnActivated);
        target.deactivated.AddListener(OnDeactivated);
        target.selectExited.AddListener(OnReleased);
        subscribed = true;
        Go(Phase.Settle);
    }

    void Update()
    {
        if (finished) return;
        try
        {
            if (phase == Phase.Cleanup)
            {
                if (Time.frameCount - phaseFrame >= 4) FinalizeRun(success);
                return;
            }
            if (Time.realtimeSinceStartup - phaseStart > 5f)
                throw new TimeoutException("No expected real interaction at phase " + phase);
            if (Time.frameCount - phaseFrame < 3) return;
            if (phase == Phase.Settle)
            {
                if (Time.realtimeSinceStartup - phaseStart < 0.8f) return;
                controller = InputSystem.devices.OfType<XRSimulatedController>()
                    .Single(x => x.usages.Contains(CommonUsages.RightHand));
                var collider = target.GetComponent<Collider>();
                if (collider == null) throw new InvalidOperationException("Target Collider is missing.");
                var ray = right.curveOrigin;
                origin.transform.position += collider.bounds.center - ray.forward * 0.8f - ray.position;
                Go(Phase.Hover);
            }
            else if (phase == Phase.Hover && Hovered())
            {
                Record(); Queue(Key.G); Go(Phase.Select);
            }
            else if (phase == Phase.Select && Selected() && selectedCount > 0 && Chain(true, false))
            {
                Record(); Queue(Key.G, Key.T); Go(Phase.Activate);
            }
            else if (phase == Phase.Activate && Selected() && activatedCount > 0 && Chain(true, true))
            {
                Record(); Queue(Key.G); Go(Phase.Deactivate);
            }
            else if (phase == Phase.Deactivate && Selected() && deactivatedCount > 0 && Chain(true, false))
            {
                Record(); Queue(); Go(Phase.Release);
            }
            else if (phase == Phase.Release && !Selected() && !target.isSelected && releasedCount > 0 && Chain(false, false))
            {
                Record(); success = true; Queue(); Go(Phase.Cleanup);
            }
        }
        catch (Exception ex)
        {
            report.error = ex.Message;
            try { Record(); } catch { }
            Queue(); Go(Phase.Cleanup);
        }
    }

    bool Hovered() => right != null && target != null && right.interactablesHovered.Contains(target);
    bool Selected() => right != null && target != null && right.interactablesSelected.Contains(target);
    bool Chain(bool grip, bool trigger) => keyboard.gKey.isPressed == grip && keyboard.tKey.isPressed == trigger
        && simulator.gripInput.ReadIsPerformed() == grip && simulator.triggerInput.ReadIsPerformed() == trigger
        && controller.gripButton.isPressed == grip && controller.triggerButton.isPressed == trigger;
    void Queue(params Key[] keys) { if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    void Go(Phase value) { phase = value; phaseStart = Time.realtimeSinceStartup; phaseFrame = Time.frameCount; }
    void OnSelected(SelectEnterEventArgs e) { if (ReferenceEquals(e.interactorObject, right)) selectedCount++; }
    void OnActivated(ActivateEventArgs e) { if (ReferenceEquals(e.interactorObject, right)) activatedCount++; }
    void OnDeactivated(DeactivateEventArgs e) { if (ReferenceEquals(e.interactorObject, right)) deactivatedCount++; }
    void OnReleased(SelectExitEventArgs e) { if (ReferenceEquals(e.interactorObject, right)) releasedCount++; }
    static string HierarchyPath(Transform t) => t == null ? "" : HierarchyPath(t.parent) + "/" + t.name;

    void Record()
    {
        report.observations.Add(new Observation {
            phase = phase.ToString(), frame = Time.frameCount,
            keyboardG = keyboard != null && keyboard.gKey.isPressed,
            keyboardT = keyboard != null && keyboard.tKey.isPressed,
            simulatorGrip = simulator != null && simulator.gripInput.ReadIsPerformed(),
            simulatorTrigger = simulator != null && simulator.triggerInput.ReadIsPerformed(),
            controllerGrip = controller != null && controller.gripButton.isPressed,
            controllerTrigger = controller != null && controller.triggerButton.isPressed,
            hovered = Hovered(), selected = Selected(), selectEvents = selectedCount,
            activateEvents = activatedCount, deactivateEvents = deactivatedCount, releaseEvents = releasedCount
        });
    }

    void FinalizeRun(bool passed)
    {
        if (finished) return;
        finished = true;
        if (keyboard != null && keyboard.added) { InputSystem.ResetDevice(keyboard); InputSystem.RemoveDevice(keyboard); }
        if (subscribed && target != null)
        {
            target.selectEntered.RemoveListener(OnSelected); target.activated.RemoveListener(OnActivated);
            target.deactivated.RemoveListener(OnDeactivated); target.selectExited.RemoveListener(OnReleased);
        }
        if (settingsCaptured)
        {
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorFocus;
        }
        if (geometryCaptured)
        {
            if (origin != null) origin.transform.position = oldOriginPosition;
            if (target != null && !target.isSelected)
            {
                target.transform.SetPositionAndRotation(oldTargetPosition, oldTargetRotation);
                if (body != null) { body.linearVelocity = oldVelocity; body.angularVelocity = oldAngularVelocity; }
            }
        }
        report.status = passed ? "passed" : "failed";
        LastResultJson = JsonUtility.ToJson(report, true);
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            try { File.WriteAllText(reportPath, LastResultJson); }
            catch (Exception ex) { Debug.LogWarning("Keyboard smoke report could not be saved: " + ex.Message); }
        }
        Debug.Log("W2_KEYBOARD_INPUT_SMOKE " + LastResultJson);
        if (gameObject != null) Destroy(gameObject);
        EditorApplication.delayCall += () => { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; };
    }

    void OnDisable()
    {
        if (!finished) { report.error = "Smoke interrupted before completing the real input chain."; FinalizeRun(false); }
    }
}
#endif
