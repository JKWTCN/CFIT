using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public enum SimAutoStart
    {
        NOCHANGE = 1,
        NOAUTO = 2,
        FSUIPC = 4,
        MSFS2020 = 8,
        MSFS2024 = 16,
    }

    public class WorkerAutoStart<C> : TaskWorker<C> where C : ConfigBase
    {
        public virtual SimAutoStart AutoStartTargets { get; set; } = SimAutoStart.NOCHANGE;
        public virtual string AutoStartSuccessMsg { get; set; }

        protected virtual int Fails { get; set; } = 0;

        public WorkerAutoStart(C config, string title = "Setup Auto-Start", string message = "") : base(config, Localization.Translate(title), Localization.Translate(message))
        {
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
            SetPropertyFromOption<SimAutoStart>(ConfigBase.OptionAutoStartTargets);
            AutoStartSuccessMsg = Localization.Translate("Auto-Start configured for {0}!", Config.ProductName);
        }

        protected virtual void RemoveAutoStart(SimAutoStart flag, Func<bool> func)
        {
            Logger.Debug($"Check Removal for '{flag}'");
            if (!AutoStartTargets.HasFlag(flag))
                if (func?.Invoke() == false)
                    Fails++;
        }

        protected virtual void AddUpdateAutoStart(SimAutoStart flag, Func<bool> func, string message = null)
        {
            if (!AutoStartTargets.HasFlag(flag))
                return;

            if (string.IsNullOrEmpty(message))
                message = Localization.Translate("Add/Update {0} Auto-Start Entry ...", flag);
            Model.Message = message;

            if (func?.Invoke() == false)
                Fails++;
        }

        protected override Task<bool> DoRun()
        {
            bool result = false;

            if (AutoStartTargets.HasFlag(SimAutoStart.NOCHANGE))
            {
                Model.SetSuccess(Localization.Translate("No Changes to Auto-Start!"));
                Model.DisplayInSummary = false;
                return Task.FromResult(true);
            }
            else if (AutoStartTargets.HasFlag(SimAutoStart.NOAUTO))
            {
                Model.Message = Localization.Translate("Remove Auto-Start Entries ...");
                RemoveAutoStart(SimAutoStart.FSUIPC, () => { return FuncFsuipc7.AutoStartRemove(Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2020, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2020, Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2024, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2024, Config.ProductExe); });
                result = Fails == 0;
                if (!result)
                    Model.SetError(Localization.Translate("Auto-Start Removal failed!"));
                else
                    Model.SetSuccess(Localization.Translate("Auto-Start removed for {0}!", Config.ProductName));
                return Task.FromResult(result);
            }
            else
            {
                Model.Message = Localization.Translate("Remove unused Auto-Start Entries ...");
                RemoveAutoStart(SimAutoStart.FSUIPC, () => { return FuncFsuipc7.AutoStartRemove(Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2020, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2020, Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2024, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2024, Config.ProductExe); });

                if (Fails == 0)
                {
                    AddUpdateAutoStart(SimAutoStart.FSUIPC, () => { return FuncFsuipc7.AutoStartAddUpdate(Config.ProductExePath, Config.ProductExe); });
                    if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2020))
                        AddUpdateAutoStart(SimAutoStart.MSFS2020, () => { return FuncMsfs.AutoStartAddUpdate(Simulator.MSFS2020, Config.ProductExePath, Config.ProductExe, Config.ProductName); });
                    if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2024))
                        AddUpdateAutoStart(SimAutoStart.MSFS2024, () => { return FuncMsfs.AutoStartAddUpdate(Simulator.MSFS2024, Config.ProductExePath, Config.ProductExe, Config.ProductName); });

                    result = Fails == 0;
                    if (!result)
                        Model.SetError(Localization.Translate("Auto-Start Configuration failed!"));
                }
                else
                    Model.SetError(Localization.Translate("Auto-Start Removal failed!"));

                if (result)
                {
                    Model.SetSuccess(AutoStartSuccessMsg);
                    return Task.FromResult(true);
                }
                else
                    return Task.FromResult(false);
            }

        }
    }
}
