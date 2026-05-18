using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using docker_monitor.Models;
using docker_monitor.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using SkiaSharp;
using System.Text;

namespace docker_monitor.ViewModels
{
    public partial class ContainerDetailsViewModel : ObservableObject
    {
        private readonly IDockerBackendService _dockerService;
        private readonly string _containerId;
        private readonly List<LogEntry> _allLogEntries = new();

        public ObservableCollection<LogEntry> DisplayLogs { get; } = new();

        [ObservableProperty]
        private string _containerName = "Unknown";

        [ObservableProperty]
        private string _socketStatus = "연결되지 않음";

        [ObservableProperty]
        private string _currentCpuText = "0.0%";

        [ObservableProperty]
        private string _currentMemoryText = "0MiB / 0MiB (0%)";

        [ObservableProperty]
        private bool _isLogsExpanded = false;

        [ObservableProperty]
        private string _logCountDisplay = "(0 lines)";

        public ObservableCollection<double> CpuValues { get; } = new();
        public ObservableCollection<double> MemoryValues { get; } = new();

        public ISeries[] CpuSeries { get; }
        public ISeries[] MemorySeries { get; }
        public IEnumerable<ISeries> CpuGaugeSeries { get; }
        public IEnumerable<ISeries> MemoryGaugeSeries { get; }
        public Axis[] XAxes { get; }
        public Axis[] CpuYAxes { get; }
        public Axis[] MemoryYAxes { get; }

        private readonly ObservableValue _cpuGaugeValue = new(0);
        private readonly ObservableValue _memoryGaugeValue = new(0);

        public ContainerDetailsViewModel(IDockerBackendService dockerService, string containerId)
        {
            _dockerService = dockerService;
            _containerId = containerId;

            CpuSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = CpuValues,
                    Name = "CPU",
                    Fill = new SolidColorPaint(SKColors.Indigo.WithAlpha(30)) { ZIndex = 5 },
                    Stroke = new SolidColorPaint(SKColors.Indigo) { StrokeThickness = 3, ZIndex = 6 },
                    GeometrySize = 0,
                    LineSmoothness = 1
                }
            };

            MemorySeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = MemoryValues,
                    Name = "메모리",
                    Fill = new SolidColorPaint(SKColors.DeepSkyBlue.WithAlpha(30)) { ZIndex = 5 },
                    Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3, ZIndex = 6 },
                    GeometrySize = 0,
                    LineSmoothness = 1
                }
            };

            CpuGaugeSeries = new GaugeBuilder()
                .WithLabelsSize(20)
                .WithLabelsPosition(PolarLabelsPosition.ChartCenter)
                .WithInnerRadius(50)
                .WithBackgroundInnerRadius(50)
                .WithBackground(new SolidColorPaint(SKColors.Gray.WithAlpha(30)))
                .AddValue(_cpuGaugeValue, "CPU", SKColors.Indigo, SKColors.Indigo)
                .BuildSeries();

            MemoryGaugeSeries = new GaugeBuilder()
                .WithLabelsSize(20)
                .WithLabelsPosition(PolarLabelsPosition.ChartCenter)
                .WithInnerRadius(50)
                .WithBackgroundInnerRadius(50)
                .WithBackground(new SolidColorPaint(SKColors.Gray.WithAlpha(30)))
                .AddValue(_memoryGaugeValue, "RAM", SKColors.DeepSkyBlue, SKColors.DeepSkyBlue)
                .BuildSeries();

            XAxes = new Axis[]
            {
                new Axis
                {
                    IsVisible = false,
                    SeparatorsPaint = null
                }
            };

            var cpuSeparators = new SolidColorPaint(new SKColor(255, 255, 255, 15)) { StrokeThickness = 1, ZIndex = -1 };
            cpuSeparators.RemoveTransition(null);

            CpuYAxes = new Axis[]
            {
                new Axis
                {
                    MinLimit = 0,
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    SeparatorsPaint = cpuSeparators,
                    EasingFunction = null
                }
            };

            var memorySeparators = new SolidColorPaint(new SKColor(255, 255, 255, 15)) { StrokeThickness = 1, ZIndex = -1 };
            memorySeparators.RemoveTransition(null);

            MemoryYAxes = new Axis[]
            {
                new Axis
                {
                    MinLimit = 0,
                    LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                    SeparatorsPaint = memorySeparators,
                    EasingFunction = null
                }
            };

            StartCommand = new AsyncRelayCommand(() => ControlAsync("start"));
            StopCommand = new AsyncRelayCommand(() => ControlAsync("stop"));
            RestartCommand = new AsyncRelayCommand(() => ControlAsync("restart"));
            ToggleLogsCommand = new RelayCommand(() => IsLogsExpanded = !IsLogsExpanded);
            LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);
            DownloadLogsCommand = new AsyncRelayCommand<string>(DownloadLogsAsync);
            DownloadWithRangeCommand = new AsyncRelayCommand<object>(DownloadWithRangeAsync);

            _dockerService.OnStatsReceived += HandleStatsReceived;
            _dockerService.OnLogReceived += HandleLogReceived;
            _dockerService.OnMoreLogsReceived += HandleMoreLogsReceived;

            _dockerService.OnContainerSocketConnected += () =>
            {
                _ = Task.Run(async () =>
                {
                    await _dockerService.StartMonitoringAsync(_containerId);
                });
            };

            _ = InitializeAsync();
        }

        [ObservableProperty]
        private DateTime _customStartDate = DateTime.Now.AddDays(-1);

        private int _currentTail = 100;
        [ObservableProperty]
        private bool _isLoadingMore = false;

        private async Task LoadMoreAsync()
        {
            if (IsLoadingMore) return;
            IsLoadingMore = true;
            string oldStatus = SocketStatus;
            SocketStatus = "과거 로그 불러오는 중...";
            try
            {

                _currentTail += 100;
                await _dockerService.LoadMoreLogsAsync(_containerId, _currentTail);
                await Task.Delay(500); 
            }
            finally
            {
                IsLoadingMore = false;
                SocketStatus = oldStatus;
            }
        }

        private async Task DownloadWithRangeAsync(object? range)
        {
            if (range == null) return;
            dynamic r = range;
            DateTime start = r.Start;
            DateTime end = r.End;

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{ContainerName}_{start:yyyyMMdd}_{end:yyyyMMdd}.log",
                Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = $"로그 파일 저장 (범위 선택)"
            };

            if (sfd.ShowDialog() == true)
            {
                long since = GetUnixTimestamp(start);
                long until = GetUnixTimestamp(end);

                string oldStatus = SocketStatus;
                SocketStatus = "로그 다운로드 중...";
                try
                {
                    bool success = await _dockerService.DownloadLogsAsync(_containerId, sfd.FileName, since, until);
                    if (success)
                    {
                        SocketStatus = "다운로드 완료!";
                        await Task.Delay(2000);
                    }
                    else
                    {
                        SocketStatus = "다운로드 실패";
                    }
                }
                finally
                {
                    SocketStatus = oldStatus;
                }
            }
        }

        private async Task DownloadLogsAsync(string? period)
        {
            long? since = null;
            string suffix = "full";

            if (period == "1d") { since = GetUnixTimestamp(DateTime.Now.AddDays(-1)); suffix = "1d"; }
            else if (period == "7d") { since = GetUnixTimestamp(DateTime.Now.AddDays(-7)); suffix = "7d"; }
            else if (period == "30d") { since = GetUnixTimestamp(DateTime.Now.AddDays(-30)); suffix = "30d"; }
            else if (period == "custom") { since = GetUnixTimestamp(CustomStartDate); suffix = "custom"; }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{ContainerName}_{suffix}_{DateTime.Now:yyyyMMdd}.log",
                Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = $"로그 파일 저장 ({suffix})"
            };

            if (sfd.ShowDialog() == true)
            {
                string oldStatus = SocketStatus;
                SocketStatus = "로그 다운로드 중...";
                try
                {
                    bool success = await _dockerService.DownloadLogsAsync(_containerId, sfd.FileName, since);
                    if (success)
                    {
                        SocketStatus = "다운로드 완료!";
                        await Task.Delay(2000);
                    }
                    else
                    {
                        SocketStatus = "다운로드 실패";
                    }
                }
                finally
                {
                    SocketStatus = oldStatus;
                }
            }
        }

        private long GetUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }


        private void UpdateStatus()
        {
            SocketStatus = _dockerService.IsConnected ? "연결됨" : "연결되지 않음";
        }

        public IAsyncRelayCommand StartCommand { get; }
        public IAsyncRelayCommand StopCommand { get; }
        public IAsyncRelayCommand RestartCommand { get; }
        public IRelayCommand ToggleLogsCommand { get; }
        public IAsyncRelayCommand LoadMoreCommand { get; }
        public IAsyncRelayCommand DownloadLogsCommand { get; }
        public IAsyncRelayCommand<object> DownloadWithRangeCommand { get; }

        private void HandleStatsReceived(ContainerStats stats)
        {
            if (stats.ContainerId != _containerId) return;

            App.Current.Dispatcher.Invoke(() =>
            {
                CpuValues.Add(stats.Cpu);
                if (CpuValues.Count > 30) CpuValues.RemoveAt(0);

                double memUsage = double.TryParse(stats.Memory.RealUsageMB, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var m) ? m : 0;
                MemoryValues.Add(memUsage);
                if (MemoryValues.Count > 30) MemoryValues.RemoveAt(0);

                double memPercent = double.TryParse(stats.Memory.Percent, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0;

                _cpuGaugeValue.Value = Math.Round(stats.Cpu, 1);
                _memoryGaugeValue.Value = Math.Round(memPercent, 1);

                CurrentCpuText = $"{stats.Cpu:F1}%";
                CurrentMemoryText = $"{stats.Memory.RealUsageMB}MiB / {stats.Memory.LimitMB}MiB ({stats.Memory.Percent}%)";
            });
        }

        private void HandleLogReceived(string logChunk)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var lines = logChunk.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var entry = ParseLogLine(line);
                    _allLogEntries.Add(entry);
                    DisplayLogs.Add(entry);
                }

                while (_allLogEntries.Count > 1000)
                {
                    _allLogEntries.RemoveAt(0);
                }
                while (DisplayLogs.Count > 1000)
                {
                    DisplayLogs.RemoveAt(0);
                }

                LogCountDisplay = $"({DisplayLogs.Count}줄)";
            });
        }

        private void HandleMoreLogsReceived(string containerId, IEnumerable<string> logArray)
        {
            Console.WriteLine($"[디버그] HandleMoreLogsReceived 도달! 가져온 라인 수={logArray.Count()}");
            
            App.Current.Dispatcher.Invoke(() =>
            {
                var lines = logArray.ToArray();
                if (lines.Length == 0) return;

                for (int i = 0; i < lines.Length; i++) lines[i] = lines[i].Trim();

                string? oldestKnownLine = _allLogEntries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Message))?.Message?.Trim();
                
                int newLinesCount = 0;
                if (oldestKnownLine == null || oldestKnownLine.StartsWith("모니터링 연결 중..."))
                {
                    if (_allLogEntries.Any(e => e.Message.StartsWith("모니터링 연결 중...")))
                    {
                        _allLogEntries.RemoveAt(0);
                        if (DisplayLogs.Count > 0 && DisplayLogs[0].Message.StartsWith("모니터링 연결 중..."))
                            DisplayLogs.RemoveAt(0);
                    }

                    foreach (var line in lines.Reverse())
                    {
                        var entry = ParseLogLine(line);
                        _allLogEntries.Insert(0, entry);
                        DisplayLogs.Insert(0, entry);
                        newLinesCount++;
                    }
                }
                else
                {
                    int indexInNewBatch = -1;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i] == oldestKnownLine) { indexInNewBatch = i; break; }
                    }

                    int end = indexInNewBatch != -1 ? indexInNewBatch : lines.Length;
                    for (int i = end - 1; i >= 0; i--)
                    {
                        var entry = ParseLogLine(lines[i]);
                        _allLogEntries.Insert(0, entry);
                        DisplayLogs.Insert(0, entry);
                        newLinesCount++;
                    }
                }

                if (newLinesCount > 0)
                {
                    SocketStatus = $"{newLinesCount}줄 추가됨";
                }
                else
                {
                    if (lines.Length > 0)
                    {
                        for (int i = Math.Min(lines.Length, 50) - 1; i >= 0; i--)
                        {
                            var entry = ParseLogLine(lines[i]);
                            _allLogEntries.Insert(0, entry);
                            DisplayLogs.Insert(0, entry);
                            newLinesCount++;
                        }
                        SocketStatus = "강제 로드됨 (중복 일치 실패)";
                    }
                    else
                    {
                        SocketStatus = "모두 중복됨";
                    }
                }

                LogCountDisplay = $"({_allLogEntries.Count}줄)";
            });
        }

        private LogEntry ParseLogLine(string line)
        {
            string level = "INFO";
            if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || line.Contains("err", StringComparison.OrdinalIgnoreCase))
                level = "ERROR";
            else if (line.Contains("WARN", StringComparison.OrdinalIgnoreCase))
                level = "WARN";

            return new LogEntry { Message = line, Level = level };
        }

        private void UpdateDisplayLogs()
        {
            Console.WriteLine($"[디버그] UpdateDisplayLogs: 현재 개수={DisplayLogs.Count}, 신규 개수={_allLogEntries.Count}");
            
            DisplayLogs.Clear();
            foreach (var entry in _allLogEntries)
            {
                DisplayLogs.Add(entry);
            }
            Console.WriteLine($"[디버그] UpdateDisplayLogs 완료. 총 개수: {DisplayLogs.Count}");
        }

        partial void OnIsLogsExpandedChanged(bool value)
        {
        }

        private async Task InitializeAsync()
        {
            var entry = new LogEntry { Message = $"모니터링 연결 중... ({_containerId})" };
            _allLogEntries.Add(entry);
            DisplayLogs.Add(entry);
            await _dockerService.StartMonitoringAsync(_containerId);
            UpdateStatus();
        }

        private async Task ControlAsync(string action)
        {
            await _dockerService.ControlContainerAsync(_containerId, action);
        }

        public void Stop()
        {
            _dockerService.OnStatsReceived -= HandleStatsReceived;
            _dockerService.OnLogReceived -= HandleLogReceived;
            _dockerService.OnMoreLogsReceived -= HandleMoreLogsReceived;
            _ = _dockerService.StopMonitoringAsync();
        }
    }

    public class LogEntry
    {
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = "INFO";
    }
}
