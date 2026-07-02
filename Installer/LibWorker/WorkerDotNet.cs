using CFIT.AppTools;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using static CFIT.Installer.LibFunc.FuncNet;

namespace CFIT.Installer.LibWorker
{
    public class WorkerDotNet<C> : TaskWorker<C>, IWorkerUpdates where C : ConfigBase
    {
        public virtual bool NetRuntimeDesktop { get; set; }
        public virtual string NetVersion { get; set; }
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; }
        public virtual string NetInstaller { get; set; }

        public virtual bool ShowUpdateInSummary { get; set; } = true;
        public virtual bool ShowUpdateCompleted { get; set; } = true;
        public virtual bool DisplayInSummary { get { return Model.DisplayInSummary; } set { Model.DisplayInSummary = value; } }
        public virtual bool DisplayPinned { get { return Model.DisplayPinned; } set { Model.DisplayPinned = value; } }
        public virtual bool DisplayCompleted { get { return Model.DisplayCompleted; } set { Model.DisplayCompleted = value; } }

        public WorkerDotNet(C config, string title = ".NET Runtime", string message = "Checking Runtime Version ...") : base(config, Localization.Translate(title), Localization.Translate(message))
        {
            Model.DisplayInSummary = false;
            Model.DisplayCompleted = true;

            SetPropertyFromConfig<bool>("NetRuntimeDesktop");
            SetPropertyFromConfig<string>("NetVersion");
            SetPropertyFromConfig<bool>("CheckMajorEqual");
            SetPropertyFromConfig<string>("NetUrl");
            SetPropertyFromConfig<string>("NetInstaller");
        }

        protected override async Task<bool> DoRun()
        {
            bool result = false;
            if (string.IsNullOrWhiteSpace(NetUrl) || string.IsNullOrWhiteSpace(NetVersion) || string.IsNullOrWhiteSpace(NetInstaller))
            {
                Model.SetError(Localization.Translate("Url, Installer or Version not set - abort!"));
                return result;
            }

            if (CheckDotNet(NetVersion, NetRuntimeDesktop, CheckMajorEqual))
            {
                Model.SetSuccess(Localization.Translate("The Runtime is at Version {0} or greater!", NetVersion));
                result = true;
            }
            else
            {
                if (ShowUpdateInSummary)
                    Model.DisplayInSummary = true;
                if (ShowUpdateCompleted)
                    Model.DisplayCompleted = true;

                Model.AddMessage(new TaskMessage(Localization.Translate("The Runtime is not installed or outdated!"), false, FontWeights.DemiBold), true, false);
                Model.SetState(Localization.Translate("Downloading Runtime ..."), TaskState.WAITING);

                string filepath = await DownloadNetRuntime(Token, NetUrl, NetInstaller);
                if (string.IsNullOrWhiteSpace(filepath))
                    Model.SetError(Localization.Translate("Could not download .NET Runtime!"));
                else if (!Token.IsCancellationRequested)
                {
                    Model.Message = Localization.Translate("Installing Runtime ({0}) ...", NetInstaller);
                    string cmd = $"\"{filepath}\" /install /quiet /norestart";
                    if (Sys.RunCommand(cmd, out _))
                    {
                        Model.AddMessage(Localization.Translate("Runtime Version {0} was installed/updated successfully.", NetVersion), true);
                        Model.SetSuccess(new TaskMessage(Localization.Translate("You need to restart the PC if a new Major Version was installed!"), true, FontWeights.DemiBold));
                        result = true;
                    }
                    else
                    {
                        Model.SetError(Localization.Translate("The Command '{0}' returned a non-zero Exit Code!", cmd));
                    }
                    File.Delete(filepath);
                }
            }

            return result;
        }
    }
}
