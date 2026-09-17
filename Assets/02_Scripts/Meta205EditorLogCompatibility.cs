#if UNITY_EDITOR_WIN
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourseSetup
{
    /// <summary>
    /// Meta Core 205 的 Windows Editor 日志兼容层。仅使用公开 SetLogCallback2，
    /// 不修改 SDK、不屏蔽日志、不改变系统编码；不进入 Android/Windows 发布构建。
    /// 不能修复 SDK InitOVRManager 内注册原回调后、此兼容层安装前的初始化窗口。
    /// </summary>
    [InitializeOnLoad]
    public static class Meta205EditorLogCompatibility
    {
        sealed class PendingLog
        {
            public OVRPlugin.LogLevel level;
            public byte[] bytes;
            public string diagnostic;
        }

        // 必须强引用 delegate，原生代码可能在后台线程调用它。
        static readonly OVRPlugin.LogCallback2DelegateType Callback = ReceiveNativeLog;
        static readonly ConcurrentQueue<PendingLog> Pending = new ConcurrentQueue<PendingLog>();
        static readonly object FileLock = new object();
        static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        static readonly UTF8Encoding DisplayUtf8 = new UTF8Encoding(false, false);
        const int MaxMessageBytes = 4 * 1024 * 1024;
        static StreamWriter rawWriter;
        static string rawPath;
        static int installedManagerId;
        static bool installed;
        static bool sessionStopping;
        static bool? supportedPackage;
        static int callbackErrors;
        static int nativeMessageCount;
        static int malformedUtf8Count;
        static int activeCallbacks;

        static Meta205EditorLogCompatibility()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayState;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            sessionStopping = false;
            InstallNow();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // OVRManager.Awake 可能在新场景中重新注册 SDK 的回调。
            if (!Application.isPlaying || sessionStopping) return;
            InstallNow();
        }

        /// <summary>可由 Editor 菜单或 Unity CLI eval 调用；仅在已初始化的 Core 205 场景安装。</summary>
        [MenuItem("Course/Diagnostics/Install Meta 205 Editor log compatibility")]
        public static void InstallNow()
        {
            if (!Application.isPlaying || sessionStopping || !SupportsInstalledVersion() ||
                !OVRManager.OVRManagerinitialized || OVRManager.instance == null) return;
            try
            {
                PrepareFile();
                OVRPlugin.SetLogCallback2(Callback);
                installedManagerId = OVRManager.instance.GetInstanceID();
                installed = true;
                Debug.Log("[Meta205 Log Compatibility] Installed Editor-only safe native-log decoding. Raw bytes: " + rawPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Meta205 Log Compatibility] Could not install; SDK callback is not guaranteed replaced. " + ex);
            }
        }

        static bool SupportsInstalledVersion()
        {
            if (!supportedPackage.HasValue)
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(OVRManager).Assembly);
                supportedPackage = package != null && package.name == "com.meta.xr.sdk.core" && package.version == "205.0.0";
            }
            return supportedPackage.Value;
        }

        static void Tick()
        {
            if (Application.isPlaying && !sessionStopping && SupportsInstalledVersion())
            {
                // OVRManager 可以在首次 Update 才完成初始化，AfterSceneLoad 并非总是足够。
                if (!OVRManager.OVRManagerinitialized || OVRManager.instance == null)
                {
                    installedManagerId = 0;
                }
                else if (!installed || installedManagerId != OVRManager.instance.GetInstanceID())
                {
                    InstallNow();
                }
            }
            DrainConsole();
        }

        static void PrepareFile()
        {
            lock (FileLock)
            {
                if (rawWriter != null) return;
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Meta205Native"));
                Directory.CreateDirectory(directory);
                rawPath = Path.Combine(directory, "native-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".jsonl");
                rawWriter = new StreamWriter(new FileStream(rawPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                rawWriter.AutoFlush = true;
                callbackErrors = 0;
                nativeMessageCount = 0;
                malformedUtf8Count = 0;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(OVRPlugin.LogCallback2DelegateType))]
        static void ReceiveNativeLog(OVRPlugin.LogLevel level, IntPtr pointer, int size)
        {
            // 不在原生回调线程做 ANSI 字符串转换或调用 Unity API。
            // 未知的非法指针属于原生内存损坏，不在本兼容层可保证修复的范围内。
            Interlocked.Increment(ref activeCallbacks);
            try
            {
                if (size < 0 || size > MaxMessageBytes || (size > 0 && pointer == IntPtr.Zero))
                {
                    Interlocked.Increment(ref callbackErrors);
                    Pending.Enqueue(new PendingLog { level = OVRPlugin.LogLevel.Error,
                        diagnostic = "Rejected invalid native log buffer: size=" + size + ", null=" + (pointer == IntPtr.Zero) +
                        ". Buffer was not read; raw bytes are unavailable for this malformed callback." });
                    return;
                }
                byte[] bytes = new byte[size];
                if (size > 0) Marshal.Copy(pointer, bytes, 0, size);
                Interlocked.Increment(ref nativeMessageCount);

                // 原始字节完整保留。编码错误不会抹掉日志，也不猜测系统区域编码。
                string record = "{\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"level\":" + (int)level +
                                ",\"size\":" + size + ",\"rawBase64\":\"" + Convert.ToBase64String(bytes) + "\"}";
                try
                {
                    lock (FileLock)
                    {
                        if (rawWriter != null) rawWriter.WriteLine(record);
                        else throw new IOException("Raw log writer was closed before callback finished.");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref callbackErrors);
                    // 如果磁盘写入失败，仍保留 Console 队列及原始 Base64，显式报告失败。
                    Pending.Enqueue(new PendingLog { level = OVRPlugin.LogLevel.Error,
                        diagnostic = "Raw-log write failed: " + ex.Message + " ; preserved record=" + record });
                }
                Pending.Enqueue(new PendingLog { level = level, bytes = bytes });
            }
            catch (Exception ex)
            {
                // 不能让可处理的托管异常越过 reverse-P/Invoke 边界。
                Interlocked.Increment(ref callbackErrors);
                try { Pending.Enqueue(new PendingLog { level = OVRPlugin.LogLevel.Error,
                    diagnostic = "Native log callback failed: " + ex.GetType().Name + ": " + ex.Message }); }
                catch { /* 内存耗尽等进程级故障下，不能再从原生回调抛出异常。 */ }
            }
            finally { Interlocked.Decrement(ref activeCallbacks); }
        }

        static void DrainConsole(bool all = false)
        {
            int remaining = all ? int.MaxValue : 256;
            while (remaining-- > 0 && Pending.TryDequeue(out var entry))
            {
                string message;
                if (entry.diagnostic != null)
                {
                    Debug.LogWarning("[Meta205 Log Compatibility] " + entry.diagnostic);
                    continue;
                }
                try { message = StrictUtf8.GetString(entry.bytes); }
                catch (DecoderFallbackException)
                {
                    Interlocked.Increment(ref malformedUtf8Count);
                    message = DisplayUtf8.GetString(entry.bytes) +
                              " [invalid UTF-8 replaced for display only; original bytes preserved in " + rawPath + "]";
                }
                message = message.TrimEnd('\0');
                // 保持 Core 205 原回调的 Console 路由；JSONL 同时保留原始数值等级。
                if (entry.level <= OVRPlugin.LogLevel.Info) Debug.Log("[OVRPlugin] " + message);
                else Debug.LogWarning("[OVRPlugin] " + message);
            }
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                sessionStopping = false;
                installedManagerId = 0;
                installed = false;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) Stop();
        }

        static void Stop()
        {
            sessionStopping = true;
            if (installed)
            {
                // 仅在退出/域重载时解除回调，避免原生代码调用已经卸载的托管 delegate。
                try { OVRPlugin.SetLogCallback2(null); }
                catch (Exception ex) { Debug.LogWarning("[Meta205 Log Compatibility] Unregister failed: " + ex.Message); }
                if (!SpinWait.SpinUntil(() => Volatile.Read(ref activeCallbacks) == 0, 1000))
                    Debug.LogWarning("[Meta205 Log Compatibility] Native callback did not drain within 1 second; inspect raw log completeness.");
            }
            DrainConsole(true);
            lock (FileLock)
            {
                if (rawWriter != null)
                {
                    try
                    {
                        rawWriter.WriteLine("{\"event\":\"session-end\",\"nativeMessages\":" + nativeMessageCount +
                            ",\"malformedUtf8Messages\":" + malformedUtf8Count + ",\"callbackErrors\":" + callbackErrors + "}");
                        rawWriter.Dispose();
                    }
                    catch (Exception ex) { Debug.LogWarning("[Meta205 Log Compatibility] Raw-log close failed: " + ex.Message); }
                    finally { rawWriter = null; }
                }
            }
            installed = false;
            installedManagerId = 0;
        }
    }
}
#endif
