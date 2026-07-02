using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI;
using Localization = CFIT.Installer.UI.Localization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public enum DeckProcessOperation
    {
        START = 1,
        STOP = 2,
        KILL = 3,
    }

    public class WorkerStreamDeckStartStop<TConfig> : TaskWorker<TConfig> where TConfig : ConfigBase
    {
        public virtual DeckProcessOperation Operation { get; protected set; }
        protected virtual FuncStreamDeck StreamDeck { get; set; }
        public virtual bool IgnorePluginRunning { get; set; } = false;
        public virtual bool RefocusWindow { get; set; } = false;
        public virtual string RefocusWindowTitle { get; set; } = InstallerWindow.WindowTitle;
        public virtual int RefocusDelayMs { get; set; } = 2500;
        public virtual int CheckTimeout { get; set; } = 15;
        public virtual int StartStopDelay { get; set; } = 3;

        public static string GetTitle(DeckProcessOperation operation)
        {
            if (operation == DeckProcessOperation.START)
                return Localization.Translate("Start StreamDeck");
            else
                return Localization.Translate("Stop StreamDeck");
        }

        public WorkerStreamDeckStartStop(TConfig config, DeckProcessOperation operation) : base(config, GetTitle(operation), "")
        {
            Operation = operation;
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = false;
        }

        protected override async Task<bool> DoRun()
        {
            StreamDeck = new FuncStreamDeck();
            Model.State = TaskState.ACTIVE;
            if (!StreamDeck.IsValid)
            {
                Model.SetError(Localization.Translate("Could not get StreamDeck Version/Path!"));
                return false;
            }

            if (Operation == DeckProcessOperation.START)
            {
                Model.Message = Localization.Translate("Starting StreamDeck Software ...");
                return await StartStreamDeckSW();
            }
            else
            {
                Model.Message = Localization.Translate("Stopping StreamDeck Software ...");
                return await StopStreamDeckSW();
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

        protected async Task<bool> StartStreamDeckSW()
        {
            await TaskWaiter.CountdownWaiter(Model, Localization.Translate("The StreamDeck Software will be started in {0}s!"), StartStopDelay, Token, TaskState.ACTIVE);

            Model.Message = Localization.Translate("Start StreamDeck ...");
            StreamDeck.StartSoftware();

            Func<bool> func = () => { return !FuncStreamDeck.IsDeckAndPluginRunning(); };
            if (IgnorePluginRunning)
                func = () => { return !FuncStreamDeck.IsStreamDeckRunning(); };

            bool result = false;
            if (!await TaskWaiter.TimeoutWaiter(Model, Localization.Translate("Wait for StreamDeck to start ({0}/{1})"), CheckTimeout, func, Token))
            {
                Model.SetError(Localization.Translate("StreamDeck Software could not be started! (Re)Start it manually."));
                result = false;
            }
            else
            {
                Model.SetSuccess(Localization.Translate("StreamDeck Software running."));
                result = true;
            }

            _ = RefocusInstallerWindow();
            return result;
        }

        protected async Task<bool> StopStreamDeckSW()
        {
            await TaskWaiter.CountdownWaiter(Model, Localization.Translate("The StreamDeck Software will be stopped in {0}s!"), StartStopDelay, Token, TaskState.ACTIVE);
            Model.Message = Localization.Translate("Stop StreamDeck and Plugin ...");
            if (Operation == DeckProcessOperation.STOP)
                StreamDeck.StopSoftware();
            else
                StreamDeck.KillSoftware();

            Func<bool> func = () => { return FuncStreamDeck.IsDeckOrPluginRunning(Operation == DeckProcessOperation.STOP); };
            if (IgnorePluginRunning)
                func = () => { return FuncStreamDeck.IsStreamDeckRunning(Operation == DeckProcessOperation.STOP); };

            if (!await TaskWaiter.TimeoutWaiter(Model, Localization.Translate("Wait for StreamDeck to close ({0}/{1})"), CheckTimeout, func, Token))
            {
                Model.Message = Localization.Translate("StreamDeck still open after Timeout - trying manual Cleanup ...");
                FuncIO.DeleteFile(FuncStreamDeck.ProgDataPath);

                if (File.Exists(FuncStreamDeck.ProgDataPath))
                {
                    Model.SetError(Localization.Translate("StreamDeck Software could not be stopped!"));
                    return false;
                }
            }

            Model.SetSuccess(Localization.Translate("StreamDeck Software closed."));
            return true;
        }
    }
}
