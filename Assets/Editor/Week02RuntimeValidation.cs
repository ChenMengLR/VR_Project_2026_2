#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourseSetup
{
    /// <summary>真实 Play Mode 验证入口；批处理调用时不要加 -quit 或 -nographics。</summary>
    [InitializeOnLoad]
    public static class Week02RuntimeValidation
    {
        const string Prefix = "CourseSetup.Week02RuntimeValidation.";
        const string ScenePath = "Assets/01_Scenes/Week02_XR_Basics.unity";
        const double TimeoutSeconds = 240;

        [Serializable] sealed class SceneState { public SceneSetup[] scenes; }

        static Week02RuntimeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Watchdog;
        }

        [MenuItem("Course/Week 02/Run real Play Mode validation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先退出 Play Mode，再启动课程验证。");
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("请先创建课程场景。", ScenePath);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty || (!Application.isBatchMode && string.IsNullOrEmpty(scene.path) && scene.rootCount > 0))
                    throw new InvalidOperationException("存在未保存场景；验证已取消，不会覆盖你的工作。");
            }

            SessionState.SetString(Prefix + "Scenes", JsonUtility.ToJson(new SceneState {
                scenes = EditorSceneManager.GetSceneManagerSetup()
            }));
            SessionState.SetString(Prefix + "Started", DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Prefix + "Pending", true);
            SessionState.SetBool(Prefix + "Finished", false);
            SessionState.SetBool(Prefix + "Passed", false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Prefix + "Pending", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var host = new GameObject("__Week02_RuntimeValidation_Temporary");
                host.AddComponent<Week02ValidationRunner>().Begin(OutputDirectory(), passed => {
                    SessionState.SetBool(Prefix + "Passed", passed);
                    SessionState.SetBool(Prefix + "Finished", true);
                    EditorApplication.isPlaying = false;
                });
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                bool passed = SessionState.GetBool(Prefix + "Passed", false);
                bool finished = SessionState.GetBool(Prefix + "Finished", false);
                SessionState.SetBool(Prefix + "Pending", false);
                if (!finished)
                    WriteAborted("Play Mode 在测试完成前结束；不能视作通过。");
                try
                {
                    var previous = JsonUtility.FromJson<SceneState>(SessionState.GetString(Prefix + "Scenes", ""));
                    if (previous != null && previous.scenes != null && previous.scenes.Length > 0)
                    {
                        bool allSaved = true;
                        foreach (var item in previous.scenes) allSaved &= !string.IsNullOrEmpty(item.path);
                        if (allSaved) EditorSceneManager.RestoreSceneManagerSetup(previous.scenes);
                        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    }
                }
                catch (Exception ex)
                {
                    passed = false;
                    Debug.LogException(ex);
                    WriteAborted("场景恢复失败：" + ex.Message);
                }
                Debug.Log("Week02 runtime validation: " + (finished && passed ? "PASS" : "FAIL") +
                          ". Results: " + OutputDirectory());
                if (Application.isBatchMode) EditorApplication.Exit(finished && passed ? 0 : 1);
            }
        }

        static void Watchdog()
        {
            if (!SessionState.GetBool(Prefix + "Pending", false) ||
                SessionState.GetBool(Prefix + "Finished", false)) return;
            if (!long.TryParse(SessionState.GetString(Prefix + "Started", "0"), out long ticks)) return;
            if ((DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds < TimeoutSeconds) return;
            SessionState.SetBool(Prefix + "Finished", true);
            SessionState.SetBool(Prefix + "Passed", false);
            WriteAborted("验证超过 240 秒，已中止。请检查 Editor 日志；未保存 Play Mode 修改。");
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else
            {
                SessionState.SetBool(Prefix + "Pending", false);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static string OutputDirectory() => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CourseValidation", "Runtime"));

        static void WriteAborted(string reason)
        {
            Directory.CreateDirectory(OutputDirectory());
            File.WriteAllText(Path.Combine(OutputDirectory(), "Week02_Runtime_ABORTED.txt"),
                DateTime.UtcNow.ToString("O") + "\nRUNTIME_NOT_COMPLETE\n" + reason +
                "\n人工键鼠 / Simulator 演示：尚未验证。\n");
        }
    }
}
#endif
