
using CFIT.AppTools;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerDesktopLinkCreate<C> : TaskWorker<C> where C : ConfigBase
    {
        public WorkerDesktopLinkCreate(C config, string title = "Desktop Link", string message = "Creating Link ...") : base(config, Localization.Translate(title), Localization.Translate(message))
        {
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
        }

        protected virtual bool CreateLink()
        {
            return Sys.CreateLink(Config.ProductName, Config.ProductExePath, Localization.Translate("Start {0}", Config.ProductName));
        }

        protected override Task<bool> DoRun()
        {
            bool result = CreateLink();
            if (result)
                Model.SetSuccess(Localization.Translate("Link placed on Desktop!"));

            return Task.FromResult(result);
        }
    }
}
