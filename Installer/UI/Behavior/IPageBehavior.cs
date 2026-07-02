using CFIT.AppLogger;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI.Tasks;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CFIT.Installer.UI.Behavior
{
    public abstract class IPageBehavior
    {
        public virtual InstallerWindow Window { get; protected set; }
        public virtual ProductDefinition BaseDefinition { get { return Window?.BaseDefinition; } }
        public virtual ConfigBase BaseConfig { get { return BaseDefinition?.BaseConfig; } }
        public virtual WorkerManagerBase BaseWorker { get { return BaseDefinition?.BaseWorker; } }
        public virtual WindowBehavior BaseBehavior { get { return BaseDefinition?.BaseBehavior; } }
        public virtual UIElement ContentRef { get; protected set; } = null;
        protected virtual DispatcherTimer RefreshAppTimer { get; set; }
        protected virtual TaskView TaskViewRef { get; set; } = null;
        protected virtual StackPanel PanelHeader => Window.PanelHeader;
        protected virtual StackPanel PanelFooter => Window.PanelFooter;
        protected virtual StackPanel PanelButtons => Window.PanelButtons;

        public IPageBehavior()
        {
            RefreshAppTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            RefreshAppTimer.Tick += RefreshAppState;
        }

        public virtual void Activate(InstallerWindow window)
        {
            Window = window;
            SetHeader();
            SetContent();
            SetFooter();
            SetButtons();
            SetActions();
            if (BaseBehavior?.CheckRunning == true && BaseWorker?.IsCompleted == false)
                RefreshAppTimer.Start();
        }

        public virtual void Deactivate()
        {
            if (ContentRef is StackPanel panel)
                panel.Children.Clear();
            RefreshAppTimer?.Stop();
            TaskViewRef = null;
        }

        protected void AddHeader(UIElement element)
        {
            Window?.PanelHeader?.Children?.Add(element);
        }

        protected void AddFooter(UIElement element)
        {
            Window?.PanelFooter?.Children?.Add(element);
        }

        public static TextBlock CreateTextBlock(string text, int fontSize = 12, FontWeight? fontWeight = null, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center)
        {
            if (fontWeight == null)
                fontWeight = FontWeights.Regular;

            return new TextBlock()
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = (FontWeight)fontWeight,
                HorizontalAlignment = horizontalAlignment,
            };
        }

        protected abstract void SetHeader();

        protected abstract void SetContent();

        protected abstract void SetFooter();

        protected abstract void SetButtons();

        protected abstract void SetActions();

        protected virtual void RefreshAppState(object sender, EventArgs e)
        {
            if (BaseDefinition?.IsRunning == true && BaseWorker?.IsRunning == true && BaseWorker?.Token.IsCancellationRequested == false)
                BaseWorker?.TokenSource?.Cancel();

            if (BaseDefinition?.IsRunning == true && TaskViewRef == null)
            {
                Window.ButtonLeft.IsEnabled = false;
                Window.ButtonRight.IsEnabled = false;
                RunningWarningCreate();
            }
            else if (BaseDefinition?.IsRunning == false && TaskViewRef != null)
            {
                Window.ButtonLeft.IsEnabled = true;
                Window.ButtonRight.IsEnabled = true;
                RunningWarningRemove();
            }
        }

        protected virtual void RunningWarningCreate()
        {
            if (BaseBehavior?.CheckRunning == true)
            {
                var model = new TaskModel(Localization.Translate("{0} running", BaseConfig?.ProductName));
                model.SetError(Localization.Translate("The Application needs to be closed before the Installer can run!"));
                model.IsCompleted = true;
                TaskViewRef = new TaskView(model);
                (ContentRef as StackPanel)?.Children?.Add(TaskViewRef);
                Logger.Debug($"{BaseConfig?.ProductName} is running!");
            }
        }

        protected virtual void RunningWarningRemove()
        {
            if (TaskViewRef == null)
                return;

            TaskViewRef.Disable(true);
            (ContentRef as StackPanel)?.Children?.Remove(TaskViewRef);
            TaskViewRef = null;
            Logger.Debug($"{BaseConfig?.ProductName} has closed.");
        }
    }
}
