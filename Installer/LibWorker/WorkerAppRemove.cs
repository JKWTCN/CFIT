using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System.IO;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerAppRemove<C> : TaskWorker<C> where C : ConfigBase
    {
        public virtual string InstallerRemoveDir { get; set; }
        public virtual string InstallerRemoveMsg { get; set; }

        public WorkerAppRemove(C config) : base(config, Localization.Translate("Remove {0}", config?.ProductName), "")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
            InstallerRemoveMsg = Localization.Translate("Removed {0} successfully!", Config.ProductName);
            Logger.Debug($"Setting RemoveDir");
            SetRemoveDir();
        }

        protected virtual void SetRemoveDir()
        {
            InstallerRemoveDir = Config.ProductPath;
        }

        protected virtual void PreRemoval()
        {

        }

        protected virtual void RemoveAppFiles()
        {
            Model.Message = Localization.Translate("Removing App Files ...");
            FuncIO.DeleteDirectory(InstallerRemoveDir, true, false);
        }

        protected virtual void PostRemoval()
        {

        }

        protected virtual bool CheckSuccess()
        {
            return !Directory.Exists(InstallerRemoveDir);
        }

        protected override Task<bool> DoRun()
        {
            Logger.Debug("Running PreRemoval() ...");
            PreRemoval();

            Logger.Debug("Running RemoveAppFiles() ...");
            RemoveAppFiles();

            Logger.Debug("Running PostRemoval() ...");
            PostRemoval();

            if (CheckSuccess())
            {
                Model.SetSuccess(InstallerRemoveMsg);
                return Task.FromResult(true);
            }
            else
                return Task.FromResult(false);
        }
    }
}
