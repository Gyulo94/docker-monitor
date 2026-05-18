using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using docker_monitor.Models;
using docker_monitor.Services;
using System.Linq;
using System.Collections.Concurrent;
using System;

namespace docker_monitor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDockerBackendService _dockerService;

        [ObservableProperty]
        private ObservableCollection<ContainerModel> _containers = new();

        [ObservableProperty]
        private ContainerModel? _selectedContainer;

        [ObservableProperty]
        private ContainerDetailsViewModel? _selectedContainerDetails;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private string _currentPageTag = "dashboard";

        public ObservableCollection<GlobalContainerStat> GlobalStats { get; } = new();
        public ObservableCollection<LogEntry> GlobalLogs { get; } = new();

        private readonly ConcurrentQueue<LogEntry> _pendingLogsQueue = new();
        private readonly System.Windows.Threading.DispatcherTimer _logFlushTimer;

        public MainViewModel()
        {
            _dockerService = new DockerBackendService();
            RefreshContainersCommand = new AsyncRelayCommand(RefreshContainersAsync);
            StartContainerCommand = new AsyncRelayCommand<string>(id => ControlContainerAsync(id!, "start"));
            StopContainerCommand = new AsyncRelayCommand<string>(id => ControlContainerAsync(id!, "stop"));
            RestartContainerCommand = new AsyncRelayCommand<string>(id => ControlContainerAsync(id!, "restart"));
            SelectContainerCommand = new RelayCommand<ContainerModel>(SelectContainer);
            SelectContainerByIdCommand = new RelayCommand<string>(SelectContainerById);
            DownloadAllLogsCommand = new AsyncRelayCommand<string>(DownloadAllLogsAsync);
            DownloadAllWithRangeCommand = new AsyncRelayCommand<object>(DownloadAllWithRangeAsync);
            
            SetupService();

            _logFlushTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _logFlushTimer.Tick += LogFlushTimer_Tick;
            _logFlushTimer.Start();

            _ = RefreshContainersAsync();
        }

        private void SetupService()
        {
            _dockerService.OnGlobalStatsReceived += HandleGlobalStatsReceived;
            _dockerService.OnGlobalLogReceived += HandleGlobalLogReceived;
            _dockerService.OnGlobalLogsBatchReceived += HandleGlobalLogsBatchReceived;
            _dockerService.OnMainSocketConnected += () =>
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    if (CurrentPageTag == "stats")
                    {
                        await _dockerService.StartGlobalStatsAsync();
                    }
                    else if (CurrentPageTag == "logs")
                    {
                        await _dockerService.StartGlobalStatsAsync();
                        await _dockerService.StartGlobalLogsAsync();
                    }
                });
            };
        }

        [ObservableProperty]
        private string _globalSocketStatus = "연결됨";

        [ObservableProperty]
        private string _globalLogCountDisplay = "(0 lines)";

        private void HandleGlobalLogReceived(GlobalLogUpdate data)
        {
            if (data?.Log == null) return;
            var lines = data.Log.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var entry = new LogEntry 
                { 
                    Message = $"[{data.Name}] {line.Trim()}",
                    Level = ParseLevel(line)
                };
                _pendingLogsQueue.Enqueue(entry);
            }
        }

        private void HandleGlobalLogsBatchReceived(System.Collections.Generic.IEnumerable<GlobalLogUpdate> batch)
        {
            if (batch == null) return;
            foreach (var data in batch)
            {
                if (data?.Log == null) continue;
                var lines = data.Log.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var entry = new LogEntry 
                    { 
                        Message = $"[{data.Name}] {line.Trim()}",
                        Level = ParseLevel(line)
                    };
                    _pendingLogsQueue.Enqueue(entry);
                }
            }
        }

        private void LogFlushTimer_Tick(object? sender, EventArgs e)
        {
            if (_pendingLogsQueue.IsEmpty) return;

            var entriesToAdd = new List<LogEntry>();
            while (_pendingLogsQueue.TryDequeue(out var entry))
            {
                entriesToAdd.Add(entry);
            }

            if (entriesToAdd.Count > 0)
            {
                foreach (var entry in entriesToAdd)
                {
                    GlobalLogs.Add(entry);
                }

                while (GlobalLogs.Count > 30000)
                {
                    GlobalLogs.RemoveAt(0);
                }
                GlobalLogCountDisplay = $"({GlobalLogs.Count} lines)";
            }
        }

        private string ParseLevel(string log)
        {
            if (log.Contains("ERROR", StringComparison.OrdinalIgnoreCase)) return "ERROR";
            if (log.Contains("WARN", StringComparison.OrdinalIgnoreCase)) return "WARN";
            return "INFO";
        }

        private void HandleGlobalStatsReceived(System.Collections.Generic.IEnumerable<GlobalContainerStat> stats)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var s in stats)
                {
                    var existing = GlobalStats.FirstOrDefault(x => x.Id == s.Id);
                    if (existing != null)
                    {
                        existing.Cpu = s.Cpu;
                        existing.Memory = s.Memory;
                        existing.MemoryLimit = s.MemoryLimit;
                        existing.MemoryPercent = s.MemoryPercent;
                        existing.Status = s.Status;
                    }
                    else
                    {
                        GlobalStats.Add(s);
                    }
                }

                var ids = stats.Select(s => s.Id).ToList();
                var toRemove = GlobalStats.Where(x => !ids.Contains(x.Id)).ToList();
                foreach (var r in toRemove) GlobalStats.Remove(r);
            });
        }

        partial void OnCurrentPageTagChanged(string value)
        {
            GlobalSocketStatus = "연결됨";

            if (value == "stats")
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await _dockerService.StartGlobalStatsAsync();
                    await _dockerService.StopGlobalLogsAsync();
                });
            }
            else if (value == "logs")
            {
                App.Current.Dispatcher.Invoke(() => GlobalLogs.Clear());
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await _dockerService.StartGlobalStatsAsync();
                    await _dockerService.StartGlobalLogsAsync();
                });
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    await _dockerService.StopGlobalStatsAsync();
                    await _dockerService.StopGlobalLogsAsync();
                });
            }
        }

        public IAsyncRelayCommand RefreshContainersCommand { get; }
        public IAsyncRelayCommand<string> StartContainerCommand { get; }
        public IAsyncRelayCommand<string> StopContainerCommand { get; }
        public IAsyncRelayCommand<string> RestartContainerCommand { get; }
        public IRelayCommand<ContainerModel> SelectContainerCommand { get; }
        public IRelayCommand<string> SelectContainerByIdCommand { get; }

        public IAsyncRelayCommand DownloadAllLogsCommand { get; }
        public IAsyncRelayCommand<object> DownloadAllWithRangeCommand { get; }

        private void SelectContainerById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var container = Containers.FirstOrDefault(c => c.Id == id);
            if (container != null)
            {
                SelectContainer(container);
                CurrentPageTag = "dashboard";
            }
        }

        private void SelectContainer(ContainerModel? container)
        {
            if (container == null)
            {
                SelectedContainerDetails?.Stop();
                SelectedContainerDetails = null;
                return;
            }

            SelectedContainer = container;
            SelectedContainerDetails?.Stop();
            SelectedContainerDetails = new ContainerDetailsViewModel(_dockerService, container.Id)
            {
                ContainerName = container.Name
            };
        }

        private async Task RefreshContainersAsync()
        {
            IsRefreshing = true;
            try
            {
                var list = await _dockerService.GetContainersAsync();
                Containers.Clear();
                foreach (var container in list)
                {
                    Containers.Add(container);
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task ControlContainerAsync(string id, string action)
        {
            var success = await _dockerService.ControlContainerAsync(id, action);
            if (success)
            {
                await Task.Delay(500);
                await RefreshContainersAsync();
            }
        }



        [ObservableProperty]
        private DateTime _customGlobalStartDate = DateTime.Now.AddDays(-1);

        private async Task DownloadAllWithRangeAsync(object? range)
        {
            if (range == null) return;
            dynamic r = range;
            DateTime start = r.Start;
            DateTime end = r.End;

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"global_docker_logs_{start:yyyyMMdd}_{end:yyyyMMdd}.log",
                Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = $"전체 로그 내보내기 (범위 선택)"
            };

            if (sfd.ShowDialog() == true)
            {
                long since = GetUnixTimestamp(start);
                long until = GetUnixTimestamp(end);

                string oldStatus = GlobalSocketStatus;
                GlobalSocketStatus = "전체 로그 다운로드 중...";
                try
                {
                    bool success = await _dockerService.DownloadAllLogsAsync(sfd.FileName, since, until);
                    if (success)
                    {
                        GlobalSocketStatus = "다운로드 완료!";
                        await Task.Delay(2000);
                    }
                    else
                    {
                        GlobalSocketStatus = "다운로드 실패";
                    }
                }
                finally
                {
                    GlobalSocketStatus = oldStatus;
                }
            }
        }

        private async Task DownloadAllLogsAsync(string? period)
        {
            long? since = null;
            string suffix = "full";

            if (period == "1d") { since = GetUnixTimestamp(DateTime.Now.AddDays(-1)); suffix = "1d"; }
            else if (period == "7d") { since = GetUnixTimestamp(DateTime.Now.AddDays(-7)); suffix = "7d"; }
            else if (period == "30d") { since = GetUnixTimestamp(DateTime.Now.AddDays(-30)); suffix = "30d"; }
            else if (period == "custom") { since = GetUnixTimestamp(CustomGlobalStartDate); suffix = "custom"; }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"global_docker_logs_{suffix}_{System.DateTime.Now:yyyyMMdd}.log",
                Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = $"전체 로그 내보내기 ({suffix})"
            };

            if (sfd.ShowDialog() == true)
            {
                string oldStatus = GlobalSocketStatus;
                GlobalSocketStatus = "전체 로그 다운로드 중...";
                try
                {
                    bool success = await _dockerService.DownloadAllLogsAsync(sfd.FileName, since);
                    if (success)
                    {
                        GlobalSocketStatus = "다운로드 완료!";
                        await Task.Delay(2000);
                    }
                    else
                    {
                        GlobalSocketStatus = "다운로드 실패";
                    }
                }
                finally
                {
                    GlobalSocketStatus = oldStatus;
                }
            }
        }

        private long GetUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
