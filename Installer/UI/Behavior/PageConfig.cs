using CFIT.AppTools;
using CFIT.Installer.Product;
using CFIT.Installer.UI.Config;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CFIT.Installer.UI.Behavior
{
    public abstract class PageConfig : IPageBehavior
    {
        public virtual ConfigPanel Panel { get; protected set; }
        public virtual List<ConfigItem> Items { get; } = new List<ConfigItem>();

        public PageConfig() : base()
        {

        }

        public abstract void CreateConfigItems();

        public override void Activate(InstallerWindow window)
        {
            base.Activate(window);
            BaseConfig?.CheckInstallerOptions();
            CreateConfigItems();
            Panel.Activate(Items);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            Panel.Deactivate();
            Items.Clear();
        }

        protected override void SetHeader()
        {
            TextBlock header = new TextBlock();
            {
                header.Text = Localization.Translate("Setup Options");
                header.FontSize = 14;
                header.FontWeight = FontWeights.Bold;
                header.HorizontalAlignment = HorizontalAlignment.Center;
                header.VerticalAlignment = VerticalAlignment.Top;
            }
            Window?.PanelHeader?.Children?.Add(header);
        }

        protected override void SetContent()
        {
            Panel = new ConfigPanel();
            ContentRef = Panel;
        }

        protected override void SetFooter()
        {

        }

        protected override void SetButtons()
        {
            SetCancelButton();
            SetInstallButton();
            SetActions();
        }

        protected virtual void SetCancelButton()
        {
            Gui.SetButton(Window?.ButtonLeft, true, true, Localization.Translate("Cancel"), Brushes.Red, IconLoader.Instance.LoadIcon("x-square"), Localization.Translate("Cancel Update / Installation"));
        }

        protected virtual void SetInstallButton()
        {
            Gui.SetButton(Window?.ButtonRight, true, true, null, Brushes.Green, IconLoader.Instance.LoadIcon("box-arrow-in-right"));
        }

        protected override void SetActions()
        {
            Window.ActionLeft = (w) =>
            {
                w.LoadWelcome();
            };

            Window.ActionRight = (w) =>
            {
                w.SetPage(InstallerPages.SETUP);
            };
        }
    }
}
