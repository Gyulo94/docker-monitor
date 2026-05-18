using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using docker_monitor.Models;
using SocketIOClient;
using Newtonsoft.Json.Linq;

namespace docker_monitor.Services
{
    public class DockerBackendService : IDockerBackendService
    {
        private readonly HttpClient _httpClient;
        private readonly SocketIO _mainSocket;
        private readonly SocketIO _containerSocket;
        private static readonly string BaseUrl = GetBaseUrl();
        private static readonly string ApiUrl = GetApiUrl();
        private static readonly string SocketPath = GetSocketPath();
        private static readonly string SocketContainerUri = GetSocketContainerUri();
        private static readonly string ApiKey = GetEnvApiKey();

        private static string GetEnvUrl()
        {

            var envUrl = Environment.GetEnvironmentVariable("DOCKER_MONITOR_BACKEND_URL");
            if (!string.IsNullOrEmpty(envUrl))
            {     
                return envUrl.Trim();
            }   
            try
            {
                var searchDirs = new System.Collections.Generic.List<string>
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.Directory.GetCurrentDirectory()
                };

                string currentBase = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 4; i++)
                {
                    var parent = System.IO.Directory.GetParent(currentBase);
                    if (parent != null)
                    {
                        searchDirs.Add(parent.FullName);
                        currentBase = parent.FullName;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir)) continue;

                    string envFilePath = System.IO.Path.Combine(dir, ".env");
                    if (System.IO.File.Exists(envFilePath))
                    {
                        var lines = System.IO.File.ReadAllLines(envFilePath);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                continue;

                            int eqIdx = line.IndexOf('=');
                            if (eqIdx > 0)
                            {
                                string key = line.Substring(0, eqIdx).Trim().Trim('\uFEFF', '\uFFFE');
                                string val = line.Substring(eqIdx + 1).Trim().Trim('\"', '\'');
                                if (key == "DOCKER_MONITOR_BACKEND_URL" && !string.IsNullOrEmpty(val))
                                {
                                    return val;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            try
            {
                var searchDirs = new System.Collections.Generic.List<string>
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.Directory.GetCurrentDirectory()
                };

                string currentBase = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 4; i++)
                {
                    var parent = System.IO.Directory.GetParent(currentBase);
                    if (parent != null)
                    {
                        searchDirs.Add(parent.FullName);
                        currentBase = parent.FullName;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir)) continue;

                    string jsonFilePath = System.IO.Path.Combine(dir, "config.json");
                    if (System.IO.File.Exists(jsonFilePath))
                    {
                        string jsonText = System.IO.File.ReadAllText(jsonFilePath);
                        var token = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
                        var val = token["DOCKER_MONITOR_BACKEND_URL"]?.ToString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            var resolved = val.Trim();
                            return resolved;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return "http://localhost:3000";
        }

        private static string GetEnvApiKey()
        {
            var envKey = Environment.GetEnvironmentVariable("DOCKER_MONITOR_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                return envKey.Trim();
            }

            try
            {
                var searchDirs = new System.Collections.Generic.List<string>
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.Directory.GetCurrentDirectory()
                };

                string currentBase = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 4; i++)
                {
                    var parent = System.IO.Directory.GetParent(currentBase);
                    if (parent != null)
                    {
                        searchDirs.Add(parent.FullName);
                        currentBase = parent.FullName;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir)) continue;

                    string envFilePath = System.IO.Path.Combine(dir, ".env");
                    if (System.IO.File.Exists(envFilePath))
                    {
                        var lines = System.IO.File.ReadAllLines(envFilePath);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                continue;

                            int eqIdx = line.IndexOf('=');
                            if (eqIdx > 0)
                            {
                                string key = line.Substring(0, eqIdx).Trim().Trim('\uFEFF', '\uFFFE');
                                string val = line.Substring(eqIdx + 1).Trim().Trim('\"', '\'');
                                if (key == "DOCKER_MONITOR_API_KEY" && !string.IsNullOrEmpty(val))
                                {
                                    return val;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            try
            {
                var searchDirs = new System.Collections.Generic.List<string>
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.Directory.GetCurrentDirectory()
                };

                string currentBase = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 4; i++)
                {
                    var parent = System.IO.Directory.GetParent(currentBase);
                    if (parent != null)
                    {
                        searchDirs.Add(parent.FullName);
                        currentBase = parent.FullName;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir)) continue;

                    string jsonFilePath = System.IO.Path.Combine(dir, "config.json");
                    if (System.IO.File.Exists(jsonFilePath))
                    {
                        string jsonText = System.IO.File.ReadAllText(jsonFilePath);
                        var token = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
                        var val = token["DOCKER_MONITOR_API_KEY"]?.ToString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            var resolved = val.Trim();
                            return resolved;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return "your_api_key";
        }

        private static string GetBaseUrl()
        {
            string url = GetEnvUrl();
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Authority}";
            }
            catch
            {
                return url.TrimEnd('/');
            }
        }

        private static string GetApiUrl()
        {
            return GetEnvUrl().TrimEnd('/') + "/api";
        }

        private static string GetSocketPath()
        {
            string url = GetEnvUrl();
            try
            {
                var uri = new Uri(url);
                string path = uri.AbsolutePath.TrimEnd('/');
                return string.IsNullOrEmpty(path) ? "/socket.io/" : path + "/socket.io/";
            }
            catch
            {
                return "/socket.io/";
            }
        }

        private static string GetSocketContainerUri()
        {
            return GetBaseUrl() + "/container";
        }

        public event Action<ContainerStats>? OnStatsReceived;
        public event Action<string>? OnLogReceived;
        public event Action<string, IEnumerable<string>>? OnMoreLogsReceived;
        public event Action<IEnumerable<GlobalContainerStat>>? OnGlobalStatsReceived;
        public event Action<GlobalLogUpdate>? OnGlobalLogReceived;
        public event Action<IEnumerable<GlobalLogUpdate>>? OnGlobalLogsBatchReceived;
        public event Action<string, IEnumerable<string>>? OnMoreGlobalLogsReceived;

        public event Action? OnMainSocketConnected;
        public event Action? OnContainerSocketConnected;

        private bool _isMockMode = false;
        private List<ContainerModel> _mockContainers = new();
        private System.Threading.Timer? _mockTimer;
        private bool _mockGlobalStatsRunning = false;
        private bool _mockGlobalLogsRunning = false;
        private string? _monitoringContainerId = null;
        private readonly Random _rand = new Random();

        public bool IsConnected => _isMockMode || _mainSocket.Connected || _containerSocket.Connected;

        public DockerBackendService()
        {
            _httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            }

            var options = new SocketIOOptions
            {
                Path = SocketPath,
                EIO = SocketIOClient.Common.EngineIO.V4,
                Transport = SocketIOClient.Common.TransportProtocol.WebSocket
            };

            if (!string.IsNullOrEmpty(ApiKey))
            {
                options.Auth = new Dictionary<string, string>
                {
                    { "apiKey", ApiKey }
                };
            }

            _mainSocket = new SocketIO(new Uri(BaseUrl), options);
            _containerSocket = new SocketIO(new Uri(SocketContainerUri), options);

            SetupSocketEvents();
        }

        private void InitializeMockData()
        {
            _mockContainers = new List<ContainerModel>
            {
                new ContainerModel
                {
                    Id = "nginx-proxy-1",
                    Name = "gyubuntu-nginx-proxy",
                    Image = "nginx:alpine",
                    State = "running",
                    Status = "Up 4 days",
                    Health = "healthy",
                    CpuUsage = 1.2,
                    MemoryUsage = 24.5,
                    PortsDisplay = "80:80, 443:443",
                    Uptime = "4 days"
                },
                new ContainerModel
                {
                    Id = "postgres-db-1",
                    Name = "postgres-database",
                    Image = "postgres:15-alpine",
                    State = "running",
                    Status = "Up 12 hours",
                    Health = "healthy",
                    CpuUsage = 0.8,
                    MemoryUsage = 84.2,
                    PortsDisplay = "5432:5432",
                    Uptime = "12 hours"
                },
                new ContainerModel
                {
                    Id = "redis-cache-1",
                    Name = "redis-cache",
                    Image = "redis:7-alpine",
                    State = "running",
                    Status = "Up 4 days",
                    Health = "healthy",
                    CpuUsage = 0.2,
                    MemoryUsage = 12.1,
                    PortsDisplay = "6379:6379",
                    Uptime = "4 days"
                },
                new ContainerModel
                {
                    Id = "web-app-1",
                    Name = "planova-web-app",
                    Image = "node:18-alpine",
                    State = "running",
                    Status = "Up 2 hours",
                    Health = "healthy",
                    CpuUsage = 5.4,
                    MemoryUsage = 124.5,
                    PortsDisplay = "3000:3000",
                    Uptime = "2 hours"
                },
                new ContainerModel
                {
                    Id = "auth-service-1",
                    Name = "planova-auth-service",
                    Image = "node:18-alpine",
                    State = "running",
                    Status = "Up 2 hours",
                    Health = "healthy",
                    CpuUsage = 1.8,
                    MemoryUsage = 95.4,
                    PortsDisplay = "8080:8080",
                    Uptime = "2 hours"
                },
                new ContainerModel
                {
                    Id = "payment-worker-1",
                    Name = "payment-background-worker",
                    Image = "dotnet:8-sdk",
                    State = "exited",
                    Status = "Exited (0) 5 hours ago",
                    Health = "",
                    CpuUsage = 0.0,
                    MemoryUsage = 0.0,
                    PortsDisplay = "",
                    Uptime = ""
                }
            };
        }

        private void StartMockTimer()
        {
            if (_mockTimer != null) return;
            _mockTimer = new System.Threading.Timer(MockTimerCallback, null, 1000, 1000);
        }

        private void MockTimerCallback(object? state)
        {
            if (!_isMockMode) return;

            foreach (var container in _mockContainers)
            {
                if (container.State != "running")
                {
                    container.CpuUsage = 0;
                    container.MemoryUsage = 0;
                    continue;
                }
                double cpuDelta = (_rand.NextDouble() * 4.0) - 2.0;
                container.CpuUsage = Math.Max(0.1, Math.Min(99.9, container.CpuUsage + cpuDelta));

                double memDelta = (_rand.NextDouble() * 3.0) - 1.5;
                container.MemoryUsage = Math.Max(5.0, Math.Min(1000.0, container.MemoryUsage + memDelta));
            }

            if (_mockGlobalStatsRunning)
            {
                var stats = new List<GlobalContainerStat>();
                foreach (var c in _mockContainers)
                {
                    double limit = c.Id switch
                    {
                        "nginx-proxy-1" => 512.0,
                        "postgres-db-1" => 1024.0,
                        "redis-cache-1" => 256.0,
                        "web-app-1" => 2048.0,
                        "auth-service-1" => 1024.0,
                        _ => 512.0
                    };

                    stats.Add(new GlobalContainerStat
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Cpu = c.CpuUsage,
                        Memory = c.MemoryUsage,
                        MemoryLimit = limit,
                        MemoryPercent = c.State == "running" ? (c.MemoryUsage / limit) * 100.0 : 0.0,
                        Status = c.State == "running" ? "Running" : c.State == "paused" ? "Paused" : "Exited"
                    });
                }
                OnGlobalStatsReceived?.Invoke(stats);
            }

            // 3. 개별 모니터링 구독 중일 경우 스탯/로그 발송
            if (!string.IsNullOrEmpty(_monitoringContainerId))
            {
                var c = _mockContainers.Find(x => x.Id == _monitoringContainerId);
                if (c != null)
                {
                    double limit = c.Id switch
                    {
                        "nginx-proxy-1" => 512.0,
                        "postgres-db-1" => 1024.0,
                        "redis-cache-1" => 256.0,
                        "web-app-1" => 2048.0,
                        "auth-service-1" => 1024.0,
                        _ => 512.0
                    };

                    if (c.State == "running")
                    {
                        var cStats = new ContainerStats
                        {
                            ContainerId = c.Id,
                            Cpu = c.CpuUsage,
                            Memory = new MemoryStats
                            {
                                UsageMB = $"{c.MemoryUsage:F1}",
                                RealUsageMB = $"{c.MemoryUsage:F1}",
                                LimitMB = $"{limit:F0}",
                                Percent = $"{((c.MemoryUsage / limit) * 100.0):F1}",
                                CacheMB = "4.2"
                            },
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        OnStatsReceived?.Invoke(cStats);

                        if (_rand.Next(100) < 30)
                        {
                            string logMsg = GenerateMockLog(c.Name);
                            OnLogReceived?.Invoke(logMsg);
                        }
                    }
                    else
                    {
                        var cStats = new ContainerStats
                        {
                            ContainerId = c.Id,
                            Cpu = 0.0,
                            Memory = new MemoryStats
                            {
                                UsageMB = "0.0",
                                RealUsageMB = "0.0",
                                LimitMB = $"{limit:F0}",
                                Percent = "0.0",
                                CacheMB = "0.0"
                            },
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        OnStatsReceived?.Invoke(cStats);
                    }
                }
            }

            if (_mockGlobalLogsRunning && _rand.Next(100) < 40)
            {
                var activeMockContainers = _mockContainers.FindAll(x => x.State == "running");
                if (activeMockContainers.Count > 0)
                {
                    var target = activeMockContainers[_rand.Next(activeMockContainers.Count)];
                    string logMsg = GenerateMockLog(target.Name);
                    OnGlobalLogReceived?.Invoke(new GlobalLogUpdate
                    {
                        Name = target.Name,
                        Log = logMsg
                    });
                }
            }
        }

        private string GenerateMockLog(string containerName)
        {
            string timeStr = DateTime.Now.ToString("HH:mm:ss");
            if (containerName.Contains("nginx"))
            {
                string[] methods = { "GET", "POST", "PUT", "DELETE" };
                string[] paths = { "/index.html", "/api/containers", "/api/stats", "/api/logs", "/favicon.ico" };
                string[] statuses = { "200 OK", "201 Created", "304 Not Modified", "404 Not Found" };
                
                string method = methods[_rand.Next(methods.Length)];
                string path = paths[_rand.Next(paths.Length)];
                string status = statuses[_rand.Next(statuses.Length)];
                
                return $"127.0.0.1 - - [{DateTime.Now:dd/MMM/yyyy:HH:mm:ss zzz}] \"{method} {path} HTTP/1.1\" {status.Split(' ')[0]} {timeStr.Length * 12} \"-\" \"Mozilla/5.0\"";
            }
            else if (containerName.Contains("postgres"))
            {
                string[] queries = {
                    "SELECT * FROM \"containers\" WHERE \"id\" = 'web-app-1' LIMIT 1;",
                    "INSERT INTO \"logs\" (\"message\", \"timestamp\") VALUES ('Connection established', NOW());",
                    "UPDATE \"stats\" SET \"cpu\" = 1.2, \"memory\" = 124.5 WHERE \"id\" = 'auth-service-1';",
                    "COMMIT;",
                    "BEGIN TRANSACTION;"
                };
                return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} KST [postgres] LOG: statement: {queries[_rand.Next(queries.Length)]}";
            }
            else if (containerName.Contains("redis"))
            {
                string[] redisCmds = {
                    "DB loaded from disk: 0.042 seconds",
                    "Ready to accept connections tcp",
                    "DB saved on disk",
                    "GET user:session:active - OK",
                    "SET token:refresh:expired - 1"
                };
                return $"1:M {DateTime.Now:dd MMM yyyy HH:mm:ss.fff} * {redisCmds[_rand.Next(redisCmds.Length)]}";
            }
            else if (containerName.Contains("web-app") || containerName.Contains("auth-service"))
            {
                string[] nodeLogs = {
                    $"[INFO] NestJS application successfully started",
                    $"[INFO] Connection established with database pg-db",
                    $"[WARN] High memory usage alert triggered (>80%)",
                    $"[INFO] GET /api/containers 200 OK - 14.5ms",
                    $"[INFO] Socket handshake successful for client_id={_rand.Next(10000)}",
                    $"[INFO] User logged in: user_id={_rand.Next(1000, 9999)}"
                };
                return nodeLogs[_rand.Next(nodeLogs.Length)];
            }

            return $"[{timeStr}] Simulated backend log entry for {containerName}";
        }

        private void SetupSocketEvents()
        {
            _containerSocket.On("statsUpdate", context =>
            {
                try
                {
                    var stats = context.GetValue<ContainerStats>(0);
                    if (stats != null)
                    {
                        OnStatsReceived?.Invoke(stats);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _containerSocket.On("logUpdate", context =>
            {
                try
                {
                    var data = context.GetValue<LogUpdate>(0);
                    if (data?.Log != null)
                    {
                        OnLogReceived?.Invoke(data.Log);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _containerSocket.On("moreLogsReceived", context =>
            {
                try
                {
                    string id = context.GetValue<string>(0);
                    var logArray = context.GetValue<List<string>>(1);
                    
                    if (id != null && logArray != null)
                    {
                        OnMoreLogsReceived?.Invoke(id, logArray);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _mainSocket.On("globalStatsUpdate", context =>
            {
                try
                {
                    var stats = context.GetValue<IEnumerable<GlobalContainerStat>>(0);
                    if (stats != null)
                    {
                        OnGlobalStatsReceived?.Invoke(stats);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _mainSocket.On("globalLogUpdate", context =>
            {
                try
                {
                    var data = context.GetValue<GlobalLogUpdate>(0);
                    if (data != null)
                    {
                        OnGlobalLogReceived?.Invoke(data);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _mainSocket.On("globalLogsBatchUpdate", context =>
            {
                try
                {
                    var batch = context.GetValue<List<GlobalLogUpdate>>(0);
                    if (batch != null)
                    {
                        OnGlobalLogsBatchReceived?.Invoke(batch);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _mainSocket.On("moreGlobalLogsReceived", context =>
            {
                try
                {
                    string? name = context.GetValue<string>(0);
                    var logArray = context.GetValue<List<string>>(1);
                    if (!string.IsNullOrEmpty(name) && logArray != null)
                    {
                        OnMoreGlobalLogsReceived?.Invoke(name, logArray);
                    }
                }
                catch
                {
                }
                return Task.CompletedTask;
            });

            _mainSocket.OnConnected += (sender, e) =>
            {
                Console.WriteLine("메인 소켓 연결됨");
                OnMainSocketConnected?.Invoke();
            };
            _mainSocket.OnDisconnected += (sender, e) =>
            {
                Console.WriteLine($"메인 소켓 연결 해제됨: {e}");
            };
            _mainSocket.OnError += (sender, e) =>
            {
                Console.WriteLine($"메인 소켓 오류: {e}");
            };

            _containerSocket.OnConnected += (sender, e) =>
            {
                Console.WriteLine("컨테이너 소켓 연결됨");
                OnContainerSocketConnected?.Invoke();
            };
            _containerSocket.OnDisconnected += (sender, e) =>
            {
                Console.WriteLine($"컨테이너 소켓 연결 해제됨: {e}");
            };
            _containerSocket.OnError += (sender, e) =>
            {
                Console.WriteLine($"컨테이너 소켓 오류: {e}");
            };
        }

        public async Task<IEnumerable<ContainerModel>> GetContainersAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{ApiUrl}/containers");
                _isMockMode = false;
                return JsonConvert.DeserializeObject<IEnumerable<ContainerModel>>(response) ?? new List<ContainerModel>();
            }
            catch
            {
                if (!_isMockMode)
                {
                    _isMockMode = true;
                    InitializeMockData();
                    StartMockTimer();
                    
                    // 소켓 연결을 가정한 연결 완료 알림 백그라운드 호출
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        OnMainSocketConnected?.Invoke();
                        OnContainerSocketConnected?.Invoke();
                    });
                }
                return _mockContainers;
            }
        }

        public async Task<bool> ControlContainerAsync(string id, string action)
        {
            if (_isMockMode)
            {
                var c = _mockContainers.Find(x => x.Id == id);
                if (c != null)
                {
                    if (action == "stop")
                    {
                        c.State = "exited";
                        c.Status = "Exited (0) Just now";
                        c.CpuUsage = 0.0;
                        c.MemoryUsage = 0.0;
                    }
                    else if (action == "start")
                    {
                        c.State = "running";
                        c.Status = "Up Just now";
                        c.CpuUsage = 1.0;
                        c.MemoryUsage = 50.0;
                    }
                    else if (action == "restart")
                    {
                        c.State = "running";
                        c.Status = "Up Just now (Restarted)";
                        c.CpuUsage = 1.5;
                        c.MemoryUsage = 60.0;
                    }
                    return true;
                }
                return false;
            }

            try
            {
                var response = await _httpClient.PostAsync($"{ApiUrl}/containers/{id}/{action}", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<string> GetLogsAsync(string id, int tail = 100)
        {
            if (_isMockMode)
            {
                var c = _mockContainers.Find(x => x.Id == id);
                if (c != null)
                {
                    var lines = new List<string>();
                    for (int i = 0; i < Math.Min(tail, 20); i++)
                    {
                        lines.Add(GenerateMockLog(c.Name));
                    }
                    return string.Join(Environment.NewLine, lines);
                }
                return "No mock logs available.";
            }

            try
            {
                var response = await _httpClient.GetAsync($"{ApiUrl}/containers/{id}/logs?tail={tail}");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
            }
            catch { }
            return "No logs available.";
        }

        public async Task<string> GetContainerDetailsJsonAsync(string id)
        {
            if (_isMockMode)
            {
                var c = _mockContainers.Find(x => x.Id == id);
                if (c != null)
                {
                    var details = new
                    {
                        id = c.Id,
                        name = c.Name,
                        image = c.Image,
                        state = c.State,
                        status = c.Status,
                        created = DateTime.Now.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        platform = "linux",
                        driver = "overlay2",
                        restartPolicy = "unless-stopped"
                    };
                    return JsonConvert.SerializeObject(details);
                }
                return "{}";
            }

            try
            {
                var response = await _httpClient.GetAsync($"{ApiUrl}/containers/{id}");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
            }
            catch { }
            return "{}";
        }

        private async Task SafeEmitAsync(SocketIO socket, string @event, params object[] args)
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    await socket.EmitAsync(@event, args);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[소켓 오류] 이벤트 {@event} 전송 실패: {ex.Message}");
            }
        }

        public async Task StartMonitoringAsync(string containerId)
        {
            if (_isMockMode)
            {
                _monitoringContainerId = containerId;
                return;
            }

            if (!_containerSocket.Connected) await _containerSocket.ConnectAsync();
            await SafeEmitAsync(_containerSocket, "startMonitor", containerId);
        }

        public async Task StopMonitoringAsync()
        {
            if (_isMockMode)
            {
                _monitoringContainerId = null;
                return;
            }

            await SafeEmitAsync(_containerSocket, "stopMonitor");
        }

        public async Task StartGlobalStatsAsync()
        {
            if (_isMockMode)
            {
                _mockGlobalStatsRunning = true;
                return;
            }

            if (!_mainSocket.Connected) await _mainSocket.ConnectAsync();
            await SafeEmitAsync(_mainSocket, "startGlobalStats");
        }

        public async Task StopGlobalStatsAsync()
        {
            if (_isMockMode)
            {
                _mockGlobalStatsRunning = false;
                return;
            }

            await SafeEmitAsync(_mainSocket, "stopGlobalStats");
        }

        public async Task StartGlobalLogsAsync()
        {
            if (_isMockMode)
            {
                _mockGlobalLogsRunning = true;
                return;
            }

            if (!_mainSocket.Connected) await _mainSocket.ConnectAsync();
            await SafeEmitAsync(_mainSocket, "startGlobalLogs");
        }

        public async Task StopGlobalLogsAsync()
        {
            if (_isMockMode)
            {
                _mockGlobalLogsRunning = false;
                return;
            }

            await SafeEmitAsync(_mainSocket, "stopGlobalLogs");
        }

        public async Task LoadMoreLogsAsync(string containerId, int tail)
        {
            if (_isMockMode) return;
            await SafeEmitAsync(_containerSocket, "loadMoreLogs", new { containerId, tail });
        }

        public async Task<bool> DownloadLogsAsync(string containerId, string destinationPath, long? since = null, long? until = null)
        {
            if (_isMockMode)
            {
                try
                {
                    var c = _mockContainers.Find(x => x.Id == containerId);
                    string name = c?.Name ?? "container";
                    var logs = new List<string>();
                    for (int i = 0; i < 200; i++)
                    {
                        logs.Add(GenerateMockLog(name));
                    }
                    await System.IO.File.WriteAllLinesAsync(destinationPath, logs);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                string url = $"{ApiUrl}/containers/{containerId}/logs/download";
                var query = new List<string>();
                if (since.HasValue) query.Add($"since={since.Value}");
                if (until.HasValue) query.Add($"until={until.Value}");
                
                if (query.Count > 0) url += "?" + string.Join("&", query);

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true);
                
                await stream.CopyToAsync(fileStream);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] 로그 다운로드 실패: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DownloadAllLogsAsync(string destinationPath, long? since = null, long? until = null)
        {
            if (_isMockMode)
            {
                try
                {
                    var logs = new List<string>();
                    for (int i = 0; i < 500; i++)
                    {
                        var target = _mockContainers[_rand.Next(_mockContainers.Count)];
                        logs.Add($"[{target.Name}] {GenerateMockLog(target.Name)}");
                    }
                    await System.IO.File.WriteAllLinesAsync(destinationPath, logs);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                string url = $"{ApiUrl}/containers/all/logs/download";
                var query = new List<string>();
                if (since.HasValue) query.Add($"since={since.Value}");
                if (until.HasValue) query.Add($"until={until.Value}");

                if (query.Count > 0) url += "?" + string.Join("&", query);

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true);

                await stream.CopyToAsync(fileStream);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] 전역 로그 다운로드 실패: {ex.Message}");
                return false;
            }
        }
    }
}
