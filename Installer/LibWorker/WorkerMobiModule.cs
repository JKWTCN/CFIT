using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CFIT.Installer.LibWorker
{
    public class WorkerMobiModule<C> : TaskWorker<C>, IWorkerUpdates where C : ConfigBase
    {
        public virtual bool MobiRequired { get; set; } = true;
        public virtual string MobiVersion { get; set; }
        public virtual Simulator MobiSimulator { get; set; } = Simulator.MSFS2020;
        public virtual string MobiModuleName { get; set; } = "mobiflight-event-module";
        public virtual string MobiArchive { get { return $"{MobiModuleName}.{MobiVersion}.zip"; } }
        public virtual string MobiUrl { get { return $"https://github.com/MobiFlight/MobiFlight-WASM-Module/releases/download/{MobiVersion}/{MobiArchive}"; } }
        public virtual Dictionary<Simulator, string[]> MsfsPackagePaths { get; set; }

        public virtual bool ShowUpdateInSummary { get; set; } = true;
        public virtual bool ShowUpdateCompleted { get; set; } = true;
        public virtual bool DisplayInSummary { get { return Model.DisplayInSummary; } set { Model.DisplayInSummary = value; } }
        public virtual bool DisplayPinned { get { return Model.DisplayPinned; } set { Model.DisplayPinned = value; } }
        public virtual bool DisplayCompleted { get { return Model.DisplayCompleted; } set { Model.DisplayCompleted = value; } }
        public virtual bool RunOnlyWhenInstalled { get; set; } = true;

        public WorkerMobiModule(C config, Simulator sim) : base(config, Localization.Translate("MobiFlight Module [{0}]", sim), Localization.Translate("Check State and Version of MobiFlight Event Module ..."))
        {
            MobiSimulator = sim;
            Model.DisplayInSummary = false;
            Model.DisplayCompleted = true;

            SetPropertyFromConfig<bool>("MobiRequired");
            SetPropertyFromConfig<string>("MobiVersion");
            SetPropertyFromConfig<string>("MobiModuleName");
        }

        protected override bool RunCondition()
        {
            return !RunOnlyWhenInstalled || RunOnlyWhenInstalled && FuncMsfs.CheckInstalledMsfs(MobiSimulator);
        }

        protected override async Task<bool> DoRun()
        {
            bool result = false;

            if (string.IsNullOrWhiteSpace(MobiUrl) || string.IsNullOrWhiteSpace(MobiVersion) || string.IsNullOrWhiteSpace(MobiArchive))
            {
                Model.SetError(Localization.Translate("Url, Archive or Version not set - abort!"));
                return result;
            }

            if (MsfsPackagePaths == null || MsfsPackagePaths.Count <= 0)
            {
                if (!Config.HasOption(ConfigBase.OptionPackagePaths, out Dictionary<Simulator, string[]> paths) || paths?.ContainsKey(MobiSimulator) == false || paths?.Count == 0)
                {
                    Model.SetError(Localization.Translate("No Package Paths for MSFS set in Options - abort!"));
                    return result;
                }

                MsfsPackagePaths = paths;
            }

            if (FuncMsfs.CheckPackageVersion(MsfsPackagePaths[MobiSimulator], MobiModuleName, MobiVersion))
            {
                result = true;
                Model.SetSuccess(Localization.Translate("Module at or above minimum Version {0}!", MobiVersion));
            }
            else
            {
                if (ShowUpdateInSummary)
                    Model.DisplayInSummary = true;
                if (ShowUpdateCompleted)
                    Model.DisplayCompleted = true;

                Model.AddMessage(new TaskMessage(Localization.Translate("Module below minimum Version {0}!", MobiVersion), false, FontWeights.DemiBold), true, false);
                Model.State = TaskState.WAITING;

                if (!SetupPossible())
                {
                    Model.AddMessage(Localization.Translate("Installation not possible while MSFS is running!"), false, false, false, FontWeights.DemiBold);
                    Model.AddMessage(Localization.Translate("Click Retry when MSFS is closed (or cancel the Installation)."));
                    var interaction = new TaskInteraction(Model);
                    interaction.AddInteraction(Localization.Translate("Retry"), InteractionResponse.RETRY);

                    if (await interaction.WaitOnResponse(Token, InteractionResponse.RETRY) && SetupPossible())
                    {
                        Model.Links.Clear();
                        result = await InstallModule();
                    }
                    else
                    {
                        Model.Links.Clear();
                        Model.SetError(Localization.Translate("MSFS is still running!"));
                    }
                }
                else
                    result = await InstallModule();
            }

            return result || !result && !MobiRequired;
        }

        public static bool SetupPossible()
        {
            return !FuncMsfs.IsRunning();
        }

        protected virtual async Task<bool> InstallModule()
        {
            bool result = false;

            Model.Message = Localization.Translate("Downloading MobiFlight Module ...");
            string filepath = await FuncIO.DownloadFile(Token, MobiUrl, MobiArchive);
            if (string.IsNullOrWhiteSpace(filepath))
            {
                Model.SetError(Localization.Translate("Could not download MobiFlight Module!"));
                return result;
            }

            Model.Message = Localization.Translate("Extracting Module to Community Folder ...");
            if (!FuncMsfs.InstallPackageFromArchive(MsfsPackagePaths[MobiSimulator], filepath, MobiModuleName, true, false))
            {
                Model.SetError(Localization.Translate("Error while extracting MobiFlight Module!"));
                return result;
            }

            try
            {
                File.Delete(filepath);
            }
            catch { }

            result = true;
            Model.SetSuccess(Localization.Translate("MobiFlight Module Version {0} installed/updated successfully!", MobiVersion));

            return result;
        }
    }
}
