using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace docker_monitor.Views
{

    public partial class ContainerDetailsView : System.Windows.Controls.UserControl
    {
        private DateTime _lastLoadTime = DateTime.MinValue;
        private double _lastOffset = 0;
        private bool _isInitialLoad = true;
        private DateTime _prependTime = DateTime.MinValue;
        private double _targetOffset = 0;

        public ContainerDetailsView()
        {
            InitializeComponent();
            this.DataContextChanged += (s, e) =>
            {
                if (e.NewValue is ViewModels.ContainerDetailsViewModel vm)
                {
                    _isInitialLoad = true;
                    
                    // Listen to IsLoadingMore to arm the scroll prepending logic
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(ViewModels.ContainerDetailsViewModel.IsLoadingMore))
                        {
                            if (vm.IsLoadingMore)
                            {
                                _prependTime = DateTime.Now;
                                _targetOffset = LogsScrollViewer.VerticalOffset;
                            }
                        }
                    };
                }
            };
        }

        private void LogsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isInitialLoad && e.ExtentHeight > 0)
            {
                _isInitialLoad = false;
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    LogsScrollViewer.ScrollToEnd();
                    _lastOffset = LogsScrollViewer.VerticalOffset;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            // 과거 로그 프리펜딩(prepend) 진행 중인 경우 (로딩 시작 후 1.5초 이내): 스크롤 위치 유지 및 하단 스크롤 방지
            if ((DateTime.Now - _prependTime).TotalSeconds < 1.5)
            {
                if (e.ExtentHeightChange > 0)
                {
                    _targetOffset += e.ExtentHeightChange;
                    LogsScrollViewer.ScrollToVerticalOffset(_targetOffset);
                    _lastOffset = _targetOffset;
                }
                else
                {
                    _targetOffset = e.VerticalOffset;
                }
                return; // 1.5초 이내에는 아래의 자동 하단 스크롤(wasAtBottom) 및 추가 로드 호출을 차단하고 즉시 리턴
            }

            if (e.ExtentHeightChange > 0)
            {
                double prevExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
                bool wasAtBottom = _lastOffset + e.ViewportHeight >= prevExtentHeight - 15;
                if (wasAtBottom)
                {
                    LogsScrollViewer.ScrollToEnd();
                }
            }

            // 오직 사용자의 수동 스크롤 조작(e.ExtentHeightChange == 0)으로 최상단에 도달하고 충분히 스크롤 가능한 영역이 있을 때만 과거 로그 로딩을 요청합니다.
            if (e.VerticalOffset == 0 && _lastOffset > 5 && !_isInitialLoad && e.ExtentHeightChange == 0 && LogsScrollViewer.ScrollableHeight > 50)
            {
                System.Diagnostics.Debug.WriteLine("Hit top via ScrollChanged");
                RequestLoadMore();
            }
            _lastOffset = e.VerticalOffset;
        }

        private void LogsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (LogsScrollViewer.VerticalOffset == 0 && e.Delta > 0)
            {
                System.Diagnostics.Debug.WriteLine("Hit top via MouseWheel");
                RequestLoadMore();
            }
        }

        private void RequestLoadMore()
        {
            if ((DateTime.Now - _lastLoadTime).TotalSeconds < 1.0) return;

            System.Diagnostics.Debug.WriteLine("RequestLoadMore triggered");

            var vm = this.DataContext as ViewModels.ContainerDetailsViewModel;
            if (vm != null)
            {
                if (vm.LoadMoreCommand.CanExecute(null))
                {
                    _lastLoadTime = DateTime.Now;
                    vm.LoadMoreCommand.Execute(null);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("RequestLoadMore: DataContext is null or wrong type");
            }
        }
        private void OnDownloadClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = this.DataContext as ViewModels.ContainerDetailsViewModel;
            if (vm == null) return;

            var dialog = new Dialogs.LogExportDialog();
            dialog.Owner = System.Windows.Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                vm.DownloadWithRangeCommand.Execute(new { Start = dialog.StartDate, End = dialog.EndDate });
            }
        }
    }
}
