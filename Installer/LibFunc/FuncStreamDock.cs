using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.IO;

namespace CFIT.Installer.LibFunc
{
    // StreamDock (MiraBox/HotSpot) counterpart of the former FuncStreamDeck.
    // The StreamDock desktop software ("VSD Craft") does not expose version or
    // install-location via the registry the way Elgato StreamDeck does, so this
    // implementation locates the executable by probing common install paths and
    // falls back to the streamdock:// protocol handler.
    public class FuncStreamDock
    {
        public virtual string BinaryPath { get; protected set; }
        public virtual bool IsValid { get { return !string.IsNullOrWhiteSpace(BinaryPath); } }

        public static string PluginBinary { get; set; }
        public static string DeckPluginPath { get { return $@"{Sys.FolderAppDataRoaming()}\HotSpot\StreamDock\plugins"; } }

        // Process name of the StreamDock desktop software (VSD Craft).
        public static string DeckBinaryName { get { return "VSD Craft"; } }
        public static string DeckProcessName { get { return "StreamDock"; } }

        // Common StreamDock executable install locations.
        protected static readonly string[] DeckInstallPaths = new string[]
        {
            @"C:\Program Files\HotSpot\StreamDock\StreamDock.exe",
            @"C:\Program Files (x86)\HotSpot\StreamDock\StreamDock.exe",
        };

        public FuncStreamDock()
        {
            BinaryPath = GetStreamDockBinaryPath();
        }

        protected virtual string GetStreamDockBinaryPath()
        {
            foreach (var path in DeckInstallPaths)
            {
                if (File.Exists(path))
                {
                    Logger.Debug($"Using StreamDock BinaryPath: {path}");
                    return path;
                }
            }

            // Fall back to the per-user LocalAppData install path.
            string localPath = Path.Combine(Sys.FolderAppDataLocal(), "HotSpot", "StreamDock", "StreamDock.exe");
            if (File.Exists(localPath))
            {
                Logger.Debug($"Using StreamDock BinaryPath: {localPath}");
                return localPath;
            }

            Logger.Warning("Could not find StreamDock executable in common locations - falling back to streamdock:// protocol.");
            return null;
        }

        public static bool IsStreamDockRunning()
        {
            return Sys.GetProcessRunning(DeckBinaryName) || Sys.GetProcessRunning(DeckProcessName);
        }

        public static bool IsDeckAndPluginRunning()
        {
            if (!string.IsNullOrWhiteSpace(PluginBinary))
                return IsStreamDockRunning() && Sys.GetProcessRunning(PluginBinary);
            else
                return IsStreamDockRunning();
        }

        public static bool IsDeckOrPluginRunning()
        {
            if (!string.IsNullOrWhiteSpace(PluginBinary))
                return IsStreamDockRunning() || Sys.GetProcessRunning(PluginBinary);
            else
                return IsStreamDockRunning();
        }

        public virtual void StartSoftware()
        {
            if (!string.IsNullOrWhiteSpace(BinaryPath) && File.Exists(BinaryPath))
            {
                Sys.StartProcess(BinaryPath, Path.GetDirectoryName(BinaryPath), null, true);
            }
            else
            {
                // Fallback: invoke the streamdock:// protocol handler.
                Sys.RunCommand("start streamdock://", out _);
            }
        }

        public virtual void StopSoftware()
        {
            // Try a graceful close first, then fall back to killing the process.
            Sys.KillProcess(DeckBinaryName);
            Sys.KillProcess(DeckProcessName);
        }

        public virtual void KillSoftware()
        {
            Sys.KillProcess(DeckBinaryName);
            Sys.KillProcess(DeckProcessName);
        }
    }
}
