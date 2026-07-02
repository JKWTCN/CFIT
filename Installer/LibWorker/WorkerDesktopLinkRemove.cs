using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerDesktopLinkRemove<C> : TaskWorker<C> where C : ConfigBase
    {
        public WorkerDesktopLinkRemove(C config, string title = "Desktop Link", string message = "Removing Link ...") : base(config, Localization.Translate(title), Localization.Translate(message))
        {
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
        }

        protected virtual string GetLinkFileName()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{Config.ProductName}.lnk");
        }

        protected virtual bool RemoveLink()
        {
            string link = GetLinkFileName();

            if (File.Exists(link))
                File.Delete(link);

            return !File.Exists(link);
        }

        protected override Task<bool> DoRun()
        {
            bool result = RemoveLink();
            if (result)
                Model.SetSuccess(Localization.Translate("Link removed from Desktop!"));

            return Task.FromResult(result);
        }
    }
}
