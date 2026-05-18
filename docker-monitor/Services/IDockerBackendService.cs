using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using docker_monitor.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace docker_monitor.Services
{
    public interface IDockerBackendService
    {
        Task<IEnumerable<ContainerModel>> GetContainersAsync();
        Task<bool> ControlContainerAsync(string id, string action);
        Task<string> GetLogsAsync(string id, int tail = 100);
        Task<string> GetContainerDetailsJsonAsync(string id);
        
        Task StartMonitoringAsync(string containerId);
        Task StopMonitoringAsync();
        Task StartGlobalStatsAsync();
        Task StopGlobalStatsAsync();
        Task StartGlobalLogsAsync();
        Task StopGlobalLogsAsync();
        Task LoadMoreLogsAsync(string containerId, int tail);
        Task<bool> DownloadLogsAsync(string containerId, string destinationPath, long? since = null, long? until = null);
        Task<bool> DownloadAllLogsAsync(string destinationPath, long? since = null, long? until = null);
        
        event Action<ContainerStats>? OnStatsReceived;
        event Action<string>? OnLogReceived;
        event Action<string, IEnumerable<string>>? OnMoreLogsReceived;
        event Action<IEnumerable<GlobalContainerStat>>? OnGlobalStatsReceived;
        event Action<GlobalLogUpdate>? OnGlobalLogReceived;
        event Action<IEnumerable<GlobalLogUpdate>>? OnGlobalLogsBatchReceived;
        event Action<string, IEnumerable<string>>? OnMoreGlobalLogsReceived;
        bool IsConnected { get; }
        event Action? OnMainSocketConnected;
        event Action? OnContainerSocketConnected;
    }

    public class ContainerStats
    {
        [System.Text.Json.Serialization.JsonPropertyName("containerId")]
        public string ContainerId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("cpu")]
        public double Cpu { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("memory")]
        public MemoryStats Memory { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    public class MemoryStats
    {
        [System.Text.Json.Serialization.JsonPropertyName("usageMB")]
        public string UsageMB { get; set; } = "0";

        [System.Text.Json.Serialization.JsonPropertyName("realUsageMB")]
        public string RealUsageMB { get; set; } = "0";

        [System.Text.Json.Serialization.JsonPropertyName("limitMB")]
        public string LimitMB { get; set; } = "0";

        [System.Text.Json.Serialization.JsonPropertyName("percent")]
        public string Percent { get; set; } = "0";

        [System.Text.Json.Serialization.JsonPropertyName("cacheMB")]
        public string CacheMB { get; set; } = "0";
    }

    public partial class GlobalContainerStat : ObservableObject
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        private double _cpu;
        [System.Text.Json.Serialization.JsonPropertyName("cpu")]
        public double Cpu 
        { 
            get => _cpu; 
            set => SetProperty(ref _cpu, value); 
        }

        private double _memory;
        [System.Text.Json.Serialization.JsonPropertyName("memory")]
        public double Memory 
        { 
            get => _memory; 
            set => SetProperty(ref _memory, value); 
        }

        private double _memoryLimit;
        [System.Text.Json.Serialization.JsonPropertyName("memoryLimit")]
        public double MemoryLimit 
        { 
            get => _memoryLimit; 
            set => SetProperty(ref _memoryLimit, value); 
        }

        private double _memoryPercent;
        [System.Text.Json.Serialization.JsonPropertyName("memoryPercent")]
        public double MemoryPercent 
        { 
            get => _memoryPercent; 
            set => SetProperty(ref _memoryPercent, value); 
        }

        private string _status = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status 
        { 
            get => _status; 
            set => SetProperty(ref _status, value); 
        }
    }

    public class GlobalLogUpdate
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("log")]
        public string Log { get; set; } = string.Empty;
    }

    public class LogUpdate
    {
        [System.Text.Json.Serialization.JsonPropertyName("containerId")]
        public string ContainerId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("log")]
        public string Log { get; set; } = string.Empty;
    }
}
