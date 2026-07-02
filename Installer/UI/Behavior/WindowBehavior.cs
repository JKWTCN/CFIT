using System;

namespace CFIT.Installer.UI.Behavior
{
    public class WindowBehavior
    {
        public virtual int TargetHeight { get; set; } = 600;
        public virtual int MaxTasksShown { get; set; } = -1;
        public virtual bool ShowSummaryPage { get; set; } = true;
        public virtual int DelaySummary { get; set; } = 1500;
        public virtual bool ShowTasksInSummary { get; set; } = true;
        public virtual bool ShowInstallationWarnings { get; set; } = true;
        public virtual bool ShowInstallationHints { get; set; } = true;
        public virtual bool FocusInstallerOnSummary { get; set; } = true;
        public virtual string WelcomeLogoResource { get; set; } = "";
        public virtual int WelcomeLogoWidth { get; set; } = 320;
        public virtual bool ShowDetectedVersion { get; set; } = true;
        public virtual bool ShowInstallPathInSummary { get; set; } = false;
        public virtual bool CheckRunning { get; set; } = true;

        public WindowBehavior()
        {

        }

        public virtual Func<InstallerWindow, string> FuncGetTitle { get; set; } = (w) =>
        {
            return Localization.Translate("{0} Installer {1}", w.BaseConfig?.ProductName, w?.BaseBehavior?.FuncGetVersion?.Invoke(w));
        };

        public virtual Func<InstallerWindow, string> FuncGetVersion { get; set; } = (w) =>
        {
            return Localization.Translate("v{0} ({1})", w?.BaseConfig?.Version, w?.BaseConfig?.ProductVersion?.Timestamp);
        };

        public virtual Func<InstallerWindow, string> FuncTextInstallationHintsHeader { get; set; } = (w) =>
        {
            return Localization.Translate("This App will install or update {0} on your System.\r\nYour existing Configuration is preserved during an Update.", w?.BaseConfig?.ProductName);
        };

        public virtual Func<InstallerWindow, string> FuncTextInstallationHintsFooter { get; set; } = (w) =>
        {
            return Localization.Translate("You do not need to remove the existing Installation for an Update!");
        };

        public virtual Action<InstallerWindow> CallbackOnActivated { get; set; } = null;
    }
}
