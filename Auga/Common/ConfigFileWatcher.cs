using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Auga
{
    // From the shared Jotunn template. Polls watched files for a changed write time or length, rather than using
    // FileSystemWatcher.
    //
    // FileSystemWatcher looks like the obvious choice and is not: it raises several events for one save (most
    // editors truncate and then write), it delivers them on a thread pool thread so every handler needs a
    // SynchronizingObject to get back onto Unity's main thread, and it holds an OS handle on a directory for the life
    // of the process. A poll on a MonoBehaviour Update is main-thread by construction, costs one FileInfo per watched
    // file per interval, and cannot fire twice for one save because the stamp is updated before the callback runs.
    //
    // The GameObject is DontDestroyOnLoad, so polling continues in the main menu where ZNet.instance is null.
    // Anything a callback does that touches the network has to guard for that itself.
    internal static class ConfigFileWatcher
    {
        private const float FallbackPollSeconds = 30f;

        private class WatchEntry
        {
            internal DateTime LastWriteUTC;
            internal long FileLength;
            internal Action<string> Callback;
        }

        private static readonly Dictionary<string, WatchEntry> WatchedFiles =
            new Dictionary<string, WatchEntry>(StringComparer.OrdinalIgnoreCase);
        private static ConfigFileWatcherBehaviour watchProcess;

        internal static void Initialize()
        {
            if (watchProcess != null) { return; }
            GameObject go = new GameObject(Auga.PluginName + "_ConfigFileWatcher");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            watchProcess = go.AddComponent<ConfigFileWatcherBehaviour>();
        }

        // Safe to call before Initialize -- registration only touches a dictionary, and the first poll picks up
        // whatever is registered by then. Re-registering a path replaces its callback rather than throwing.
        internal static void Register(string fullPath, Action<string> onChanged)
        {
            if (string.IsNullOrEmpty(fullPath)) { return; }

            DateTime mtime = DateTime.MinValue;
            long size = 0;
            if (File.Exists(fullPath))
            {
                FileInfo info = new FileInfo(fullPath);
                mtime = info.LastWriteTimeUtc;
                size = info.Length;
            }

            WatchedFiles[fullPath] = new WatchEntry() { LastWriteUTC = mtime, FileLength = size, Callback = onChanged };
        }

        // Re-seed a watched file's stamp after we write it ourselves, so our own write is not seen as an external
        // change on the next poll. No-op for paths that are not (yet) watched.
        internal static void RefreshStamp(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) { return; }
            if (WatchedFiles.TryGetValue(fullPath, out WatchEntry entry) == false) { return; }

            try
            {
                FileInfo info = new FileInfo(fullPath);
                entry.LastWriteUTC = info.LastWriteTimeUtc;
                entry.FileLength = info.Length;
            }
            catch (Exception)
            {
                // File gone or inaccessible right after our write; reset so the next poll re-reads it.
                entry.LastWriteUTC = DateTime.MinValue;
                entry.FileLength = 0;
            }
        }

        internal class ConfigFileWatcherBehaviour : MonoBehaviour
        {
            private float nextPollTime;

            public void Update()
            {
                if (Time.unscaledTime < nextPollTime) { return; }
                nextPollTime = Time.unscaledTime + PollInterval();
                Poll();
            }

            private static float PollInterval()
            {
                return ValConfig.ConfigPollIntervalSeconds != null
                    ? ValConfig.ConfigPollIntervalSeconds.Value
                    : FallbackPollSeconds;
            }

            private static void Poll()
            {
                if (WatchedFiles.Count == 0) { return; }

                // Snapshot the keys: a callback is allowed to register a further file, which would invalidate a live
                // enumerator mid-poll.
                string[] paths = WatchedFiles.Keys.ToArray();

                foreach (string path in paths)
                {
                    if (WatchedFiles.TryGetValue(path, out WatchEntry entry) == false) { continue; }
                    if (File.Exists(path) == false) { continue; }

                    FileInfo info = new FileInfo(path);
                    DateTime mtime = info.LastWriteTimeUtc;
                    long size = info.Length;
                    if (mtime == entry.LastWriteUTC && size == entry.FileLength) { continue; }

                    // Stamp before invoking, so a callback that rewrites the file cannot make it look changed again
                    // on the next pass.
                    entry.LastWriteUTC = mtime;
                    entry.FileLength = size;

                    try
                    {
                        entry.Callback?.Invoke(path);
                    }
                    catch (Exception e)
                    {
                        Auga.LogWarning($"ConfigFileWatcher callback for {path} threw: {e.Message}");
                    }
                }
            }
        }
    }
}
