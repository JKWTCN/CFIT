using CFIT.AppTools;
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
    public class WorkerFsuipc7<C> : TaskWorker<C>, IWorkerUpdates where C : ConfigBase
    {
        public virtual bool Fsuipc7Required { get; set; } = true;
        public virtual string Fsuipc7Version { get; set; }
        public virtual bool Fsuipc7AllowBeta { get; set; } = false;
        public virtual bool Fsuipc7CheckPumps { get; set; } = true;
        public virtual Simulator Fsuipc7Simulator { get; set; } = Simulator.MSFS2020;
        public virtual string Fsuipc7Url { get; set; }
        public virtual string Fsuipc7Installer { get; set; } = "Install_FSUIPC7";
        public virtual string Fsuipc7InstallerArchive { get { return $"{Fsuipc7Installer}.zip"; } }
        public virtual string Fsuipc7InstallerBinary { get { return $"{Fsuipc7Installer}.exe"; } }
        public virtual string Fsuipc7WasmName { get; set; } = "fsuipc-lvar-module";
        public virtual string Fsuipc7WasmVersion { get; set; }
        public virtual Dictionary<Simulator, string[]> MsfsPackagePaths { get; set; }

        public virtual bool ShowUpdateInSummary { get; set; } = true;
        public virtual bool ShowUpdateCompleted { get; set; } = true;
        public virtual bool DisplayInSummary { get { return Model.DisplayInSummary; } set { Model.DisplayInSummary = value; } }
        public virtual bool DisplayPinned { get { return Model.DisplayPinned; } set { Model.DisplayPinned = value; } }
        public virtual bool DisplayCompleted { get { return Model.DisplayCompleted; } set { Model.DisplayCompleted = value; } }

        public WorkerFsuipc7(C config, Simulator sim) : base(config, Localization.Translate("FSUIPC7 [{0}]", sim), Localization.Translate("Check State and Version of FSUIPC7 ..."))
        {
            Model.DisplayInSummary = false;
            Model.DisplayCompleted = true;

            Fsuipc7Simulator = sim;
            SetPropertyFromConfig<bool>("Fsuipc7Required");
            SetPropertyFromConfig<string>("Fsuipc7Version");
            SetPropertyFromConfig<bool>("Fsuipc7AllowBeta");
            SetPropertyFromConfig<bool>("Fsuipc7CheckPumps");
            SetPropertyFromConfig<string>("Fsuipc7Url");
            SetPropertyFromConfig<string>("Fsuipc7Installer");
            SetPropertyFromConfig<string>("Fsuipc7WasmName");
            SetPropertyFromConfig<string>("Fsuipc7WasmVersion");
        }

        protected override async Task<bool> DoRun()
        {
            bool result = false;

            if (string.IsNullOrWhiteSpace(Fsuipc7Url) || string.IsNullOrWhiteSpace(Fsuipc7Version) || string.IsNullOrWhiteSpace(Fsuipc7Installer))
            {
                Model.SetError(Localization.Translate("Url, Installer or Version not set - abort!"));
                return result;
            }

            if (MsfsPackagePaths == null || MsfsPackagePaths?.Count <= 0)
            {
                if (!Config.HasOption(ConfigBase.OptionPackagePaths, out Dictionary<Simulator, string[]> paths) || paths?.ContainsKey(Fsuipc7Simulator) == false || paths?.Count == 0)
                {
                    Model.SetError(Localization.Translate("No Package Paths for MSFS set - abort!"));
                    return result;
                }

                MsfsPackagePaths = paths;
            }

            if (FuncFsuipc7.CheckVersion(Fsuipc7Version, Fsuipc7AllowBeta))
            {
                CheckInstallation();
                result = true;
            }
            else
            {
                if (ShowUpdateInSummary)
                    Model.DisplayInSummary = true;
                if (ShowUpdateCompleted)
                    Model.DisplayCompleted = true;

                Model.AddMessage(new TaskMessage(Localization.Translate("FSUIPC7 below minimum Version {0}!", Fsuipc7Version), false, FontWeights.DemiBold), true, false);
                Model.State = TaskState.WAITING;

                if (!SetupPossible())
                {
                    Model.AddMessage(Localization.Translate("Installation not possible while MSFS or FSUIPC are running!"), false, false, false, FontWeights.DemiBold);
                    Model.AddMessage(Localization.Translate("Click Retry when MSFS/FSUIPC are closed (or cancel the Installation)."));
                    var interaction = new TaskInteraction(Model);
                    interaction.AddInteraction(Localization.Translate("Retry"), InteractionResponse.RETRY);

                    if (await interaction.WaitOnResponse(Token, InteractionResponse.RETRY) && SetupPossible())
                    {
                        Model.Links.Clear();
                        result = await InstallFsuipc7();
                    }
                    else
                    {
                        Model.Links.Clear();
                        Model.SetError(Localization.Translate("MSFS or FSUIPC still running!"));
                    }
                }
                else
                    result = await InstallFsuipc7();
            }

            return result || !result && !Fsuipc7Required;
        }

        public static bool SetupPossible()
        {
            return !FuncFsuipc7.IsRunning() && !FuncMsfs.IsRunning();
        }

        protected virtual void CheckInstallation()
        {
            if (!FuncMsfs.CheckPackageVersion(MsfsPackagePaths[Fsuipc7Simulator], Fsuipc7WasmName, Fsuipc7WasmVersion))
            {
                Model.AddMessage(new TaskMessage(Localization.Translate("FSUIPC7 is installed, but its WASM Module does not match the Minimum Version {0}!", Fsuipc7WasmVersion), true, FontWeights.DemiBold), true, false);
                Model.AddMessage(new TaskMessage(Localization.Translate("It is not required for the Plugin itself, but could lead to Problems with Profiles/Integrations which use FSUIPC7 Variables or Commands.\r\nConsider Reinstalling FSUIPC!"), true, FontWeights.Regular), true, false);
                Model.State = TaskState.WAITING;
                Model.AddLink(Localization.Translate("FSUIPC"), "https://fsuipc.com/");
                Model.DisplayInSummary = true;
            }
            else if (!FuncFsuipc7.CheckSettingsForPumps())
            {
                Model.AddMessage(new TaskMessage(Localization.Translate("FSUIPC7 is installed, but the FSUIPC7.ini is missing the NumberOfPumps=0 Entry in the [General] Section (which helps to avoid Stutters)!"), true, FontWeights.DemiBold), true, false);
                Model.State = TaskState.WAITING;
                Model.DisplayInSummary = true;
            }
            else
            {
                Model.SetSuccess(Localization.Translate("FSUIPC7 at or above minimum Version {0}!", Fsuipc7Version));
            }
        }

        protected virtual async Task<bool> InstallFsuipc7()
        {
            Model.SetState(Localization.Translate("Downloading FSUIPC Installer ..."), TaskState.WAITING);
            string archivePath = await FuncIO.DownloadFile(Token, Fsuipc7Url, Fsuipc7InstallerArchive);
            if (string.IsNullOrWhiteSpace(archivePath))
            {
                Model.SetError(Localization.Translate("Could not download FSUIPC Installer!"));
                return false;
            }
            string workDir = Path.GetDirectoryName(archivePath);

            Model.Message = Localization.Translate("Extracting Installer Archive ...");
            string installerDir = $@"{workDir}\{Fsuipc7Installer}";
            FuncIO.DeleteDirectory(installerDir, true);
            if (!FuncZip.ExtractZipFile(workDir, archivePath))
            {
                Model.SetError(Localization.Translate("Error while extracting FSUIPC Installer!"));
                return false;
            }

            Model.Message = Localization.Translate("Running FSUIPC Installer - manual Interaction required ...");
            string binPath = $@"{installerDir}\{Fsuipc7InstallerBinary}";
            if (!File.Exists(binPath))
            {
                Model.SetError(Localization.Translate("Could not locate the Installer Binary!"));
                return false;
            }

            Sys.RunCommand($"\"{binPath}\"", out _);
            await Task.Delay(1000, Token);

            try
            {
                File.Delete(archivePath);
                FuncIO.DeleteDirectory(installerDir, true);
            }
            catch { }

            if (FuncFsuipc7.CheckVersion(Fsuipc7Version, Fsuipc7AllowBeta))
            {
                Model.SetSuccess(Localization.Translate("FSUIPC Version {0} was installed/updated successfully!", Fsuipc7Version));
                return true;
            }
            else
            {
                Model.SetError(Localization.Translate("FSUIPC not at target Version after Setup!"));
                return false;
            }
        }
    }
}
