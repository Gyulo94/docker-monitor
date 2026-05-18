using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace docker_monitor
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private bool _isExplicitExit = false;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            SetupTrayIcon();
            
            if (this.DataContext is ViewModels.MainViewModel initialVm)
            {
                initialVm.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(ViewModels.MainViewModel.CurrentPageTag))
                    {
                        if (initialVm.CurrentPageTag == "logs")
                        {
                            _isInitialGlobalLoad = true;
                        }
                    }
                };
            }
            
            this.DataContextChanged += (s, e) =>
            {
                if (e.NewValue is ViewModels.MainViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(ViewModels.MainViewModel.CurrentPageTag))
                        {
                            if (vm.CurrentPageTag == "logs")
                            {
                                _isInitialGlobalLoad = true;
                            }
                        }
                    };
                }
            };
        }

        private double _lastGlobalOffset = 0;
        private bool _isInitialGlobalLoad = true;

        private void GlobalLogsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isInitialGlobalLoad && e.ExtentHeight > 0)
            {
                _isInitialGlobalLoad = false;
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    GlobalLogsScrollViewer.ScrollToEnd();
                    _lastGlobalOffset = GlobalLogsScrollViewer.VerticalOffset;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            if (e.ExtentHeightChange > 0)
            {
                double prevExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
                bool wasAtBottom = _lastGlobalOffset + e.ViewportHeight >= prevExtentHeight - 15;
                if (wasAtBottom)
                {
                    GlobalLogsScrollViewer.ScrollToEnd();
                }
            }

            _lastGlobalOffset = e.VerticalOffset;
        }

        private void GlobalLogsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "Docker Monitor";
            
            try
            {
                var iconUri = new Uri("pack://application:,,,/app_icon.png");
                var iconStream = System.Windows.Application.GetResourceStream(iconUri)?.Stream;
                if (iconStream != null)
                {
                    using (var bitmap = new System.Drawing.Bitmap(iconStream))
                    {
                        IntPtr hIcon = bitmap.GetHicon();
                        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
                    }
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            
            _notifyIcon.Click += (s, e) =>
            {
                if (e is System.Windows.Forms.MouseEventArgs me && me.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    TrayOpen_Click(null!, null!);
                }
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) => TrayOpen_Click(null!, null!));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("종료", null, (s, e) => TrayExit_Click(null!, null!));
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                if (_notifyIcon != null)
                {
                    {
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                    }
                }
                base.OnClosing(e);
            }
        }

        public void ExitApplication()
        {
            _isExplicitExit = true;
            this.Close();
        }

        private void RootNavigation_BackRequested(Wpf.Ui.Controls.NavigationView sender, System.Windows.RoutedEventArgs args)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                if (vm.SelectedContainerDetails != null)
                {
                    vm.SelectContainerCommand.Execute(null);
                }
                else if (vm.CurrentPageTag != "dashboard")
                {
                    vm.CurrentPageTag = "dashboard";
                }
            }
        }

        private void OnMenuItemClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.NavigationViewItem item)
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    var tag = item.TargetPageTag?.ToString() ?? "dashboard";
                    vm.CurrentPageTag = tag;
                    
                    if (tag == "logs")
                    {
                        _isInitialGlobalLoad = true;
                    }

                    if (tag == "dashboard")
                    {
                        vm.SelectContainerCommand.Execute(null);
                    }
                }
            }
        }

        private void OnDownloadClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = this.DataContext as ViewModels.MainViewModel;
            if (vm == null) return;

            var dialog = new Dialogs.LogExportDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                vm.DownloadAllWithRangeCommand.Execute(new { Start = dialog.StartDate, End = dialog.EndDate });
            }
        }
    }
}