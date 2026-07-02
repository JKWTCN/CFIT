using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI;
using System;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    // StreamDock (MiraBox/HotSpot) counterpart of the former WorkerStreamDeckStartStop.
    // Unlike StreamDeck, the StreamDock software does not write a shared-memory flag
    // file, so there is no ProgDataPath cleanup step here.
    public class WorkerStreamDockStartStop<TConfig> : TaskWorker<TConfig> where TConfig : ConfigBase
    {
        public virtual DeckProcessOperation Operation { get; protected set; }
        protected virtual FuncStreamDock StreamDock { get; set; }
        public virtual bool IgnorePluginRunning { get; set; } = false;
        public virtual bool RefocusWindow { get; set; } = false;
        public virtual string RefocusWindowTitle { get; set; } = InstallerWindow.WindowTitle;
        public virtual int RefocusDelayMs { get; set; } = 2500;
        public virtual int CheckTimeout { get; set; } = 15;
        public virtual int StartStopDelay { get; set; } = 3;

        public static string GetTitle(DeckProcessOperation operation)
        {
            if (operation == DeckProcessOperation.START)
                return $"Start StreamDock";
            else
                return $"Stop StreamDock";
        }

        public WorkerStreamDockStartStop(TConfig config, DeckProcessOperation operation) : base(config, GetTitle(operation), "")
        {
            Operation = operation;
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = false;
        }

        protected override async Task<bool> DoRun()
        {
            StreamDock = new FuncStreamDock();
            Model.State = TaskState.ACTIVE;

            if (Operation == DeckProcessOperation.START)
            {
                Model.Message = "Starting StreamDock Software ...";
                return await StartStreamDockSW();
            }
            else
            {
                Model.Message = "Stopping StreamDock Software ...";
                return await StopStreamDockSW();
            }
        }

        protected async Task RefocusInstallerWindow()
        {
            if (RefocusWindow)
            {
                await Task.Delay(RefocusDelayMs, Token);
                Logger.Debug($"Refocus to '{RefocusWindowTitle}'");
                Sys.SetForegroundWindow(RefocusWindowTitle);
            }
        }

        protected async Task<bool> StartStreamDockSW()
        {
            await TaskWaiter.CountdownWaiter(Model, "The StreamDock Software will be started in {0}s!", StartStopDelay, Token, TaskState.ACTIVE);

            Model.Message = "Start StreamDock ...";
            StreamDock.StartSoftware();

            Func<bool> func = () => { return !FuncStreamDock.IsDeckAndPluginRunning(); };
            if (IgnorePluginRunning)
                func = () => { return !FuncStreamDock.IsStreamDockRunning(); };

            bool result = false;
            if (!await TaskWaiter.TimeoutWaiter(Model, "Wait for StreamDock to start ({0}/{1})", CheckTimeout, func, Token))
            {
                Model.SetError("StreamDock Software could not be started! (Re)Start it manually.");
                result = false;
            }
            else
            {
                Model.SetSuccess("StreamDock Software running.");
                result = true;
            }

            _ = RefocusInstallerWindow();
            return result;
        }

        protected async Task<bool> StopStreamDockSW()
        {
            await TaskWaiter.CountdownWaiter(Model, "The StreamDock Software will be stopped in {0}s!", StartStopDelay, Token, TaskState.ACTIVE);
            Model.Message = "Stop StreamDock and Plugin ...";
            StreamDock.KillSoftware();

            Func<bool> func = () => { return FuncStreamDock.IsDeckOrPluginRunning(); };
            if (IgnorePluginRunning)
                func = () => { return !FuncStreamDock.IsStreamDockRunning(); };

            if (!await TaskWaiter.TimeoutWaiter(Model, "Wait for StreamDock to close ({0}/{1})", CheckTimeout, func, Token))
            {
                Model.SetError("StreamDock Software could not be stopped!");
                return false;
            }

            Model.SetSuccess("StreamDock Software closed.");
            return true;
        }
    }
}
