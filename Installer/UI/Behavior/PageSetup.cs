using CFIT.AppTools;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI.Tasks;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CFIT.Installer.UI.Behavior
{
    public class PageSetup : IPageBehavior
    {
        protected virtual TaskViewPanel TaskPanel { get; set; }
        protected virtual DispatcherTimer RefreshTimer { get; set; }
        protected virtual bool WorkerCompleted { get; set; } = false;

        public PageSetup() : base()
        {
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)

            };
            RefreshTimer.Tick += RefreshState;
        }

        public override void Activate(InstallerWindow window)
        {
            base.Activate(window);
            BaseWorker.Run();
            RefreshTimer?.Start();
            TaskPanel.Activate();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            TaskPanel.Deactivate();
            RefreshTimer?.Stop();
        }

        protected override void SetHeader()
        {
            string mode = Localization.Translate("Installing");
            if (BaseConfig?.Mode == SetupMode.UPDATE)
                mode = Localization.Translate("Updating");
            if (BaseConfig?.Mode == SetupMode.REMOVE)
                mode = Localization.Translate("Removing");

            var header = new TextBlock()
            {
                Text = Localization.Translate("{0} {1} ...", mode, BaseConfig?.ProductName),
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };

            Window?.PanelHeader?.Children?.Add(header);
        }

        protected override void SetContent()
        {
            TaskPanel = new TaskViewPanel(BaseBehavior.MaxTasksShown);
            ContentRef = TaskPanel;
        }

        protected override void SetFooter()
        {
            var header = new TextBlock()
            {
                Text = Localization.Translate("Note: Some Steps can be intercepted by Windows SmartScreen or User Account Control (UAC)!"),
                FontSize = 12,
                FontWeight = FontWeights.DemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 448,
            };
            Window?.PanelFooter?.Children?.Add(header);

            header = new TextBlock()
            {
                Text = Localization.Translate("Allow them to run for a successful Setup."),
                FontSize = 12,
                FontWeight = FontWeights.DemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 448,
            };
            Window?.PanelFooter?.Children?.Add(header);
        }

        protected override void SetButtons()
        {
            Gui.SetButton(Window?.ButtonLeft, false, false, "");
            Gui.SetButton(Window?.ButtonRight, true, true, Localization.Translate("Cancel"), Brushes.Red, IconLoader.Instance.LoadIcon("box-arrow-in-right"), Localization.Translate("Cancel Update / Installation"));
        }

        protected override void SetActions()
        {
            Window.ActionLeft = null;
            Window.ActionRight = (w) =>
            {
                BaseWorker?.TokenSource?.Cancel();
                TaskStore.CurrentTask.SetError(Localization.Translate("Installation canceled by User."));
            };
        }

        protected virtual async void RefreshState(object sender, EventArgs e)
        {
            if (!WorkerCompleted)
            {
                if (Window?.ButtonRight?.IsEnabled == false && BaseWorker?.Token.IsCancellationRequested == false)
                    Window.ButtonRight.IsEnabled = true;
                else if (Window?.ButtonRight?.IsEnabled == true && BaseWorker?.Token.IsCancellationRequested == true)
                    Window.ButtonRight.IsEnabled = false;
            }

            if (BaseWorker?.IsCompleted == true && !WorkerCompleted)
            {
                WorkerCompleted = true;
                await WorkerHasCompleted();
            }
        }

        protected virtual async Task WorkerHasCompleted()
        {
            if (BaseBehavior?.ShowSummaryPage == true)
            {
                Window.ButtonRight.IsEnabled = false;
                await Task.Delay(BaseBehavior.DelaySummary);
                Window.ButtonRight.IsEnabled = true;
                Window?.SetPage(InstallerPages.SUMMARY);
            }
            else
            {
                string icon = "icons/check-square";
                if (BaseWorker?.IsSuccess == false || BaseWorker?.Token.IsCancellationRequested == true)
                    icon = "icons/x-square";
                Gui.SetButton(Window?.ButtonRight, true, true, Localization.Translate("Close"), SystemColors.ControlTextBrush, IconLoader.Instance.LoadIcon(icon), Localization.Translate("Close Installer"));
                Window.ButtonRight.IsEnabled = true;

                Window.ActionRight = (w) =>
                {
                    Gui.SetButton(w.ButtonRight, true, false);
                    w.SetPage();
                    w.Close();
                };
            }
        }
    }
}
