#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace CourseSetup
{
    /// <summary>
    /// Editor-only construction from the unmodified Meta ISDK 205 example.
    /// Requires Meta XR All-in-One 205.0.0 and Unity OpenXR 1.18.0.
    /// Does not add an XRI rig, change the machine OpenXR runtime, or claim play testing.
    /// </summary>
    public static class Week03Builder
    {
        public const string ScenePath = "Assets/01_Scenes/Week03_Meta_Locomotion.unity";
        private const string PackageId = "com.meta.xr.sdk.interaction.ovr";
        private const string ExpectedVersion = "205.0.0";
        private const string TaskRootName = "Week03_CourseTask";
        private const string ReportPath = "Assets/01_Scenes/Week03_Meta_Locomotion.structure.json";
        private static readonly List<string> Notes = new List<string>();

        [Serializable]
        private sealed class StructureReport
        {
            public string generatedUtc;
            public string unityVersion;
            public string sdkVersion;
            public string sourceScene;
            public string sourceSha256;
            public bool originalSourceUnchanged;
            public string courseScene;
            public string folderConvention;
            public int cameraRigCount;
            public int xriComponentCount;
            public int locomotionSettingsUICount;
            public int teleportInteractorCount;
            public int controllerGrabInteractorCount;
            public int missingScriptCount;
            public bool startHotspotConfigured;
            public bool targetHotspotConfigured;
            public bool missionGrabConfigured;
            public bool missionGravityEnabled;
            public bool missionDynamicBody;
            public string realHandTracking;
            public string runtimeVerification;
            public string[] notes;
            public string[] errors;
        }

        [MenuItem("Course/Week 03/Build Meta Locomotion Scene")]
        public static void Build()
        {
            GuardEditorState();
            Notes.Clear();
            var package = PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == PackageId);
            if (package == null || package.version != ExpectedVersion)
                throw new InvalidOperationException("Install " + PackageId + "@" + ExpectedVersion + " before building Week 03.");

            EnsureFolder("Assets/01_Scenes");
            EnsureTmpEssentials();
            string source = ImportOfficialExample(package.version);
            string sourceHash = HashFile(source);

            if (File.Exists(ProjectAbsolute(ScenePath)))
            {
                // Re-running is inspection only, so it cannot overwrite later student edits.
                var existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!existing.GetRootGameObjects().Any(g => g.name == TaskRootName))
                    throw new InvalidOperationException("Destination scene already exists but was not created by this builder. It was preserved.");
                Notes.Add("Existing course scene preserved; Build performed structure validation only.");
                WriteAndCheckReport(existing, source, sourceHash, package.version);
                return;
            }

            if (!AssetDatabase.CopyAsset(source, ScenePath))
                throw new IOException("Could not copy official example scene to " + ScenePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var rigs = Components<OVRCameraRig>(scene);
            if (rigs.Length != 1)
                throw new InvalidOperationException("The official sample must contain exactly one OVRCameraRig; no extra rig was added.");
            int originalUiCount = Components<LocomotionSettingsUIController>(scene).Length;
            if (originalUiCount == 0)
                throw new InvalidOperationException("Official locomotion settings UI was not found. Stop to inspect the imported sample.");

            string materialFolder = ExistingNumberedFolder("Materials", "Assets/03_Materials") + "/Week03";
            EnsureFolder(materialFolder);
            var green = CreateMaterial(materialFolder + "/StartZone.mat", new Color(0.10f, 0.72f, 0.28f));
            var blue = CreateMaterial(materialFolder + "/TargetZone.mat", new Color(0.08f, 0.38f, 0.92f));
            var orange = CreateMaterial(materialFolder + "/MissionObject.mat", new Color(1.0f, 0.48f, 0.08f));

            var taskRoot = new GameObject(TaskRootName);
            SceneManager.MoveGameObjectToScene(taskRoot, scene);
            var rig = rigs[0];
            Vector3 forward = Vector3.ProjectOnPlane(rig.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // Near the original spawn, with a clear three metre exercise route.
            Vector3 start = rig.transform.position + right * 1.25f + forward * 0.75f;
            Vector3 target = start + forward * 3.0f;
            Physics.SyncTransforms();
            start.y = FindGroundY(start, rig.transform.position.y) + 0.065f;
            target.y = FindGroundY(target, rig.transform.position.y) + 0.065f;
            var startZone = CreateZone("StartZone", start, Quaternion.LookRotation(forward), green, taskRoot.transform);
            var targetZone = CreateZone("TargetZone", target, Quaternion.LookRotation(-forward), blue, taskRoot.transform);

            var mission = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mission.name = "MissionObject";
            mission.transform.SetParent(taskRoot.transform, true);
            // Side offset keeps the landing centre free, while the ball begins over the target disc.
            mission.transform.position = target + right * 0.40f + Vector3.up * 1.05f;
            mission.transform.localScale = Vector3.one * 0.24f;
            mission.GetComponent<Renderer>().sharedMaterial = orange;
            var body = mission.AddComponent<Rigidbody>();
            body.mass = 0.25f;
            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Uses the official 205 public API; default supported devices and grab types are All.
            QuickActionsAPI.AddGrabInteraction(mission);

            // Do not run Project Setup Tool Apply All: its hand recommendation conflicts with this course.
            var config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
            OVRProjectConfig.CommitProjectConfig(config);
            Notes.Add("Original example rig, environment, slide/teleport selection and turn settings UI are retained.");
            Notes.Add("StartZone and TargetZone use official Teleport Hotspots with SnapPositionAndRotation.");
            Notes.Add("MissionObject starts above TargetZone with dynamic gravity; a runtime run is needed to verify its final resting position.");
            Notes.Add("Real hand tracking is ControllersOnly. Official controller-driven hand visuals and default All interaction wiring are retained.");
            Notes.Add("Course scene uses Assets/01_Scenes to match the numbered Week 02 convention; the official Samples scene remains at its imported path.");
            Notes.Add("Scene placement and shader appearance still require visual inspection in Meta XR Simulator.");
            if (Components<LocomotionSettingsUIController>(scene).Length != originalUiCount)
                throw new InvalidOperationException("Locomotion settings UI count changed unexpectedly.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save course scene.");
            AssetDatabase.SaveAssets();
            WriteAndCheckReport(scene, source, sourceHash, package.version);
            Selection.activeGameObject = targetZone;
            Debug.Log("Week 03 scene saved: " + ScenePath + ". Structure checked; Simulator execution is still pending.");
        }

        private static GameObject CreateZone(string name, Vector3 landingPosition, Quaternion rotation, Material material, Transform parent)
        {
            // The logical root stays at unit scale so official interaction reticles are not flattened.
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, true);
            zone.transform.SetPositionAndRotation(landingPosition, rotation);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "DiscVisual";
            visual.transform.SetParent(zone.transform, false);
            visual.transform.localPosition = new Vector3(0, -0.0325f, 0);
            visual.transform.localScale = new Vector3(1.25f, 0.03f, 1.25f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            // A scaled CapsuleCollider would not match a thin disc. Use the actual cylinder mesh.
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var collider = visual.AddComponent<MeshCollider>();
            collider.sharedMesh = visual.GetComponent<MeshFilter>().sharedMesh;
            collider.convex = true;
            QuickActionsAPI.AddTeleportInteraction(zone, TeleportSurfaceType.Hotspot,
                TeleportHotspotSnap.SnapPositionAndRotation);
            return zone;
        }

        private static float FindGroundY(Vector3 position, float fallback)
        {
            var hits = Physics.RaycastAll(new Vector3(position.x, fallback + 0.45f, position.z),
                Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            var ground = hits.Where(h => h.normal.y > 0.8f && h.collider.GetComponentInParent<OVRCameraRig>() == null)
                .OrderBy(h => h.distance).ToArray();
            if (ground.Length > 0) return ground[0].point.y;
            Notes.Add("No upward-facing floor collider found below " + position + "; used original rig floor height. Inspect placement.");
            return fallback;
        }

        private static string ImportOfficialExample(string version)
        {
            var matches = Sample.FindByPackage(PackageId, version).Where(s => s.displayName == "Example Scenes").ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one official Example Scenes sample.");
            var sample = matches[0];
            if (!sample.isImported && !sample.Import()) throw new IOException("Failed to import official Example Scenes.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // The package display name contains U+200B; never construct its display-name directory manually.
            string direct = Path.Combine(sample.importPath, "LocomotionExamples.unity").Replace('\\', '/');
            if (File.Exists(ProjectAbsolute(direct))) return ProjectRelative(direct);
            var paths = AssetDatabase.FindAssets("LocomotionExamples t:Scene", new[] { "Assets/Samples" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileName(p) == "LocomotionExamples.unity" && p.Contains("/" + version + "/"))
                .ToArray();
            if (paths.Length != 1) throw new InvalidOperationException("Could not identify the single imported 205 LocomotionExamples scene.");
            return paths[0];
        }

        private static void EnsureTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings", new[] { "Assets" }).Length > 0) return;
            var packages = PackageInfo.GetAllRegisteredPackages().Where(p => p.name == "com.unity.ugui" || p.name == "com.unity.textmeshpro");
            string archive = packages.SelectMany(p => Directory.GetFiles(p.resolvedPath,
                "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)).FirstOrDefault();
            if (archive == null)
            {
                Notes.Add("TMP Essentials archive was not found; inspect sample text and import TMP Essentials if the Editor requests it.");
                return;
            }
            AssetDatabase.ImportPackage(archive, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Notes.Add("Imported bundled TMP Essential Resources because no project TMP_Settings asset existed.");
        }

        [MenuItem("Course/Week 03/Configure OpenXR for Desktop and Android")]
        public static void ConfigureWeek03()
        {
            GuardEditorState();
            EnsureFolder("Assets/XR");
            XRGeneralSettingsPerBuildTarget store;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out store) || store == null)
            {
                store = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>).FirstOrDefault(s => s != null);
                if (store == null)
                {
                    store = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(store, AssetDatabase.GenerateUniqueAssetPath("Assets/XR/XRGeneralSettingsPerBuildTarget.asset"));
                }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, store, true);
            }

            foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                if (!store.HasSettingsForBuildTarget(group)) store.CreateDefaultSettingsForBuildTarget(group);
                if (!store.HasManagerSettingsForBuildTarget(group)) store.CreateDefaultManagerSettingsForBuildTarget(group);
                var general = store.SettingsForBuildTarget(group);
                general.InitManagerOnStart = true;
                var manager = store.ManagerSettingsForBuildTarget(group);
                manager.automaticLoading = true;
                manager.automaticRunning = true;
                if (!XRPackageMetadataStore.AssignLoader(manager, typeof(OpenXRLoader).FullName, group))
                    throw new InvalidOperationException("Unable to assign OpenXR loader to " + group);
                // Public helper creates/refreshes valid features for this build target.
                FeatureHelpers.RefreshFeatures(group);
                var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (xr == null) throw new InvalidOperationException("OpenXR settings unavailable for " + group + "; verify Android Build Support is installed.");
                var touch = xr.GetFeature<OculusTouchControllerProfile>();
                if (touch == null) throw new InvalidOperationException("Oculus Touch feature not present for " + group);
                touch.enabled = true;
                EditorUtility.SetDirty(touch);
                // Meta Quest Support is Android-only by its official Feature attribute.
                if (group == BuildTargetGroup.Android)
                {
                    var quest = xr.GetFeature<MetaQuestFeature>();
                    if (quest == null) throw new InvalidOperationException("Meta Quest Support feature is missing for Android.");
                    quest.enabled = true;
                    EditorUtility.SetDirty(quest);
                }
                foreach (var feature in xr.GetFeatures<OpenXRFeature>())
                {
                    // Disable hand-specific OpenXR extensions while leaving controller profiles untouched.
                    string typeName = feature.GetType().Name;
                    if (typeName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        feature.enabled = false;
                        EditorUtility.SetDirty(feature);
                    }
                }
                EditorUtility.SetDirty(general);
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(xr);
            }
            EditorUtility.SetDirty(store);
            var config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
            OVRProjectConfig.CommitProjectConfig(config);
            AssetDatabase.SaveAssets();
            Debug.Log("OpenXR configured for Desktop and Android, Oculus Touch enabled for both, Android Meta Quest Support enabled, real hand tracking disabled. Machine runtime was not changed.");
        }

        private static void WriteAndCheckReport(Scene scene, string source, string sourceHash, string sdkVersion)
        {
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var behaviours = Components<MonoBehaviour>(scene).Where(c => c != null).ToArray();
            var task = scene.GetRootGameObjects().SingleOrDefault(g => g.name == TaskRootName);
            var start = task == null ? null : task.transform.Find("StartZone");
            var target = task == null ? null : task.transform.Find("TargetZone");
            var mission = task == null ? null : task.transform.Find("MissionObject");
            var body = mission == null ? null : mission.GetComponent<Rigidbody>();
            var errors = new List<string>();
            var report = new StructureReport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                sdkVersion = sdkVersion,
                sourceScene = source,
                sourceSha256 = sourceHash,
                originalSourceUnchanged = sourceHash == HashFile(source),
                courseScene = ScenePath,
                folderConvention = "Course scene: Assets/01_Scenes; official example remains in its original imported Samples directory.",
                cameraRigCount = Components<OVRCameraRig>(scene).Length,
                xriComponentCount = behaviours.Count(c => (c.GetType().FullName ?? "").StartsWith("UnityEngine.XR.Interaction.Toolkit", StringComparison.Ordinal)),
                locomotionSettingsUICount = Components<LocomotionSettingsUIController>(scene).Length,
                teleportInteractorCount = Components<TeleportInteractor>(scene).Length,
                controllerGrabInteractorCount = Components<GrabInteractor>(scene).Length,
                missingScriptCount = all.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
                startHotspotConfigured = IsSnapHotspot(start),
                targetHotspotConfigured = IsSnapHotspot(target),
                missionGrabConfigured = mission != null && mission.GetComponent<Grabbable>() != null
                    && mission.GetComponentsInChildren<GrabInteractable>(true).Length > 0
                    && mission.GetComponentsInChildren<HandGrabInteractable>(true).Length > 0,
                missionGravityEnabled = body != null && body.useGravity,
                missionDynamicBody = body != null && !body.isKinematic,
                realHandTracking = OVRProjectConfig.CachedProjectConfig.handTrackingSupport.ToString(),
                runtimeVerification = "NOT PERFORMED by this builder. Meta XR Simulator play, controls, rendering, physics, teleport and grab still require runtime verification.",
                notes = Notes.ToArray()
            };
            if (!report.originalSourceUnchanged) errors.Add("Original official source scene changed during construction.");
            if (report.cameraRigCount != 1) errors.Add("Expected one OVRCameraRig.");
            if (report.xriComponentCount > 0) errors.Add("Unexpected XRI components in the Meta sample scene.");
            if (report.locomotionSettingsUICount < 1) errors.Add("Official locomotion settings UI missing.");
            if (report.teleportInteractorCount < 1) errors.Add("TeleportInteractor missing.");
            if (report.controllerGrabInteractorCount < 1) errors.Add("Controller GrabInteractor missing.");
            if (report.missingScriptCount > 0) errors.Add("Scene contains missing scripts.");
            if (!report.startHotspotConfigured || !report.targetHotspotConfigured) errors.Add("Hotspot setup is incomplete.");
            if (!report.missionGrabConfigured || !report.missionGravityEnabled || !report.missionDynamicBody) errors.Add("MissionObject grab/physics setup is incomplete.");
            if (report.realHandTracking != "ControllersOnly") errors.Add("Real hand tracking must remain ControllersOnly for this course.");
            report.errors = errors.ToArray();
            File.WriteAllText(ProjectAbsolute(ReportPath), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
            if (errors.Count > 0) throw new InvalidOperationException("Week 03 structure checks failed: " + string.Join("; ", errors));
        }

        private static bool IsSnapHotspot(Transform zone)
        {
            if (zone == null || zone.GetComponentsInChildren<Collider>(true).Length == 0) return false;
            var teleport = zone.GetComponentInChildren<TeleportInteractable>(true);
            if (teleport == null || !teleport.AllowTeleport || !teleport.FaceTargetDirection || teleport.EyeLevel) return false;
            var data = new SerializedObject(teleport);
            var target = data.FindProperty("_targetPoint");
            var surface = data.FindProperty("_surface");
            return target != null && target.objectReferenceValue == zone
                && surface != null && surface.objectReferenceValue != null;
        }

        private static T[] Components<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        private static void GuardEditorState()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before running the course setup.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the open scene before running setup; unsaved edits were preserved.");
        }

        private static Material CreateMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable. Check the URP project setup.");
            var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static string ExistingNumberedFolder(string suffix, string fallback)
        {
            var match = AssetDatabase.GetSubFolders("Assets").OrderBy(x => x)
                .FirstOrDefault(p => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(p), @"^\d+_" + suffix + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            string path = match ?? fallback;
            EnsureFolder(path);
            return path;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string segment in path.Split('/').Skip(1))
            {
                string next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }

        private static string ProjectRelative(string path)
        {
            if (!Path.IsPathRooted(path)) return path.Replace('\\', '/');
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new IOException("Sample path is outside the project.");
            return full.Substring(root.Length).Replace('\\', '/');
        }

        private static string HashFile(string path)
        {
            using (var stream = File.OpenRead(ProjectAbsolute(path)))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static string ProjectAbsolute(string path)
            => Path.IsPathRooted(path) ? path : Path.Combine(Application.dataPath, "..", path);
    }
}
#endif
