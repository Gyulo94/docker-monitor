using System;

namespace docker_monitor.Models
{
    public class ContainerModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Health { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public string Uptime { get; set; } = string.Empty;
        public string PortsDisplay { get; set; } = string.Empty;
    }
}
