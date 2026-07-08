using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using PaperConnect.Core;
using PaperConnect.Core.Entry;
using PaperConnect.Core.Entry.Easytier;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Module.Helper;
using PaperConnect.Core.Room;
using PaperConnect.Core.Utils;
using Tomlyn;

public class PaperConnectCore
{
    public required string EasyTierPath { get; set; }
    public required string EasyTierCliPath { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public int GamePort { get; set; } = 19132;
    public string ClientPlayer { get; set; } = "Steve";
    public Action<List<AgreementEntry.PlayerEntry>> OnPlayerInfoUpdated { get; set; }
    public System.Action? LinkSuccess { get; set; }
    public CoreType CoreType { get; private set; }

    private Process _easyTierProcess;
    private List<string> PublicServers { get; set; }
    private bool _isClient = false;
    private PaperConnectClient _client { get; set; }
    private bool _isStart = false;
    private CancellationTokenSource _cts;

    public void Initialize(CoreType coreType, List<string> etPublicser)
    {
        CoreType = coreType;
        PublicServers = etPublicser;
        _cts = new CancellationTokenSource();
        
        if (File.Exists("config.toml"))
            File.Delete("config.toml");

        if (string.IsNullOrEmpty(EasyTierPath))
            throw new NullReferenceException("EasyTierPath");

        if (!File.Exists(EasyTierPath))
            throw new FileNotFoundException($"EasyTier not found at: {EasyTierPath}");

        var argsJson = EmbeddedResourceHelper.ReadEmbeddedResource(Assembly.GetExecutingAssembly(),
            "PaperConnect.Core.Manifest.EasyTierParameter.json");

        var argsEntry = JsonSerializer.Deserialize<List<string>>(argsJson);
        var serverEntry = etPublicser;

        if (coreType == CoreType.Server)
        {
            Root acl = PaperConnectAclBuilder.BuildPaperConnectAcl(true, "10.144.144.1", null);
            var fromModel = Toml.FromModel(acl);
            File.WriteAllText("config.toml", fromModel);
            RoomCode = RoomCodeGenerator.GenerateRoomCode();
            Console.WriteLine($@"RoomCode: {RoomCode}");

            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            var server = new PaperConnectServer(ClientPlayer, GamePort);
            server.OnPlayerInfoUpdated = OnPlayerInfoUpdated;

            _ = Task.Run(() => server.StartAsync(), _cts.Token);

            var args = $"-i 10.144.144.1 --hostname paper-connect-server-{server.ServerPort} " +
                       $"--network-name {roomCodeInfo.NetworkName} --network-secret {roomCodeInfo.NetworkKey} " +
                       string.Join(" ", argsEntry) +
                       " -p " +
                       string.Join(" -p ", serverEntry);

            StartEasyTier(args);
        }
        else if (coreType == CoreType.Client)
        {
            _isClient = true;
            var acl = PaperConnectAclBuilder.BuildPaperConnectAcl(false, "10.144.144.1", null);
            var fromModel = Toml.FromModel(acl);
            File.WriteAllText("config.toml", fromModel);
            
            if (string.IsNullOrEmpty(RoomCode))
                throw new NullReferenceException("RoomCode");

            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);

            var args = $"-d --network-name {roomCodeInfo.NetworkName} " +
                       $"--network-secret {roomCodeInfo.NetworkKey} " +
                       string.Join(" ", argsEntry) +
                       " -p " +
                       string.Join(" -p ", serverEntry);

            StartEasyTier(args);
        }
    }

    private void StartEasyTier(string arguments)
    {
        try
        {
            Console.WriteLine($@"Starting EasyTier with args: {arguments}");
            
            // 使用 PowerShell 启动并捕获输出
            var escapedArgs = arguments.Replace("\"", "\\\"");
            var psScript = $@"
                $process = Start-Process -FilePath '{EasyTierPath}' -ArgumentList '{escapedArgs}' -Verb RunAs -PassThru -WindowStyle Hidden
                $process.Id
            ";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _easyTierProcess = new Process { StartInfo = startInfo };

            // 处理输出
            var outputData = new StringBuilder();
            var errorData = new StringBuilder();

            _easyTierProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputData.AppendLine(e.Data);
                    Console.WriteLine($@"[EasyTier] {e.Data}");
                    
                    // 处理客户端连接逻辑
                    if (!_isStart && _isClient && e.Data.Contains("new peer added."))
                    {
                        _isStart = true;
                        var json = GetPeers();
                        json.ForEach(p =>
                        {
                            if (p.Hostname.Contains(RoomCodeGenerator.ROOM_NAME))
                            {
                                Console.WriteLine(p.Hostname);
                                StartClient(p.Hostname, PublicServers);
                            }
                        });
                    }
                }
            };

            _easyTierProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorData.AppendLine(e.Data);
                    Console.WriteLine($@"[EasyTier ERROR] {e.Data}");
                }
            };

            if (_easyTierProcess.Start())
            {
                _easyTierProcess.BeginOutputReadLine();
                _easyTierProcess.BeginErrorReadLine();

                Console.WriteLine($@"EasyTier started");
            }
            else
            {
                Console.WriteLine(@"Failed to start EasyTier process");
                return;
            }

            // 等待进程退出
            _easyTierProcess.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Console.WriteLine(@"用户取消了管理员权限请求");
            throw new Exception("需要管理员权限才能运行 EasyTier");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error starting EasyTier: {ex.Message}");
            throw;
        }
    }

    public void Stop(bool isAllstop = false)
    {
        try
        {
            _cts?.Cancel();
            
            if (_easyTierProcess != null && !_easyTierProcess.HasExited)
            {
                _easyTierProcess.Kill();
                _easyTierProcess.WaitForExit(5000);
                _easyTierProcess.Dispose();
                Console.WriteLine(@"EasyTier stopped");
            }

            var processes = Process.GetProcesses()
                .Where(p => p.ProcessName.Equals("easytier-core.exe", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var p in processes)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Error killing process {p.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error stopping EasyTier: {ex.Message}");
        }

        if (_client != null && isAllstop)
        {
            _client.Stop();
        }
    }

    private string StartEasyTierCli(string arguments)
    {
        var sb = new StringBuilder();
        try
        {
            Console.WriteLine($@"Starting EasyTierCli with args: {arguments}");
            
            var escapedArgs = arguments.Replace("\"", "\\\"");
            var psScript = $@"
                $process = Start-Process -FilePath '{EasyTierCliPath}' -ArgumentList '{escapedArgs}' -Verb RunAs -PassThru -WindowStyle Hidden -Wait
                $process.ExitCode
            ";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var etCli = new Process { StartInfo = startInfo };

            etCli.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    sb.AppendLine(e.Data);
            };

            etCli.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($@"[EasyTierCli ERROR] {e.Data}");
            };

            if (etCli.Start())
            {
                etCli.BeginOutputReadLine();
                etCli.BeginErrorReadLine();
                etCli.WaitForExit();
                Console.WriteLine($@"EasyTierCli completed");
            }
            else
            {
                Console.WriteLine(@"Failed to start EasyTierCli process");
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Console.WriteLine(@"用户取消了管理员权限请求");
            throw new Exception("需要管理员权限才能运行 EasyTierCli");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error starting EasyTierCli: {ex.Message}");
            throw;
        }

        return sb.ToString();
    }

    private List<PeerInfo> GetPeers()
    {
        var json = StartEasyTierCli("-o json peer");
        if (string.IsNullOrEmpty(json))
            return new List<PeerInfo>();
            
        try
        {
            return JsonSerializer.Deserialize<List<PeerInfo>>(json) ?? new List<PeerInfo>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($@"Failed to parse peer info: {ex.Message}");
            Console.WriteLine($@"JSON: {json}");
            return new List<PeerInfo>();
        }
    }

    private void StartClient(string hostName, List<string> sers)
    {
        var argsJson = EmbeddedResourceHelper.ReadEmbeddedResource(Assembly.GetExecutingAssembly(),
            "PaperConnect.Core.Manifest.EasyTierParameter.json");

        var argsEntry = JsonSerializer.Deserialize<List<string>>(argsJson);
        var serverEntry = sers;

        Stop();
        
        var serverPort = int.Parse(hostName.Replace($"{RoomCodeGenerator.ROOM_NAME}-server-", ""));
        Console.WriteLine($@"Host Port: {serverPort}");

        var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
        
        var args = $"-d --network-name {roomCodeInfo.NetworkName} " +
                   $"--network-secret {roomCodeInfo.NetworkKey} " +
                   string.Join(" ", argsEntry) +
                   " -p " +
                   string.Join(" -p ", serverEntry) +
                   $" --port-forward tcp://0.0.0.0:{serverPort}/10.144.144.1:{serverPort}";
                   
        Task.Run(() => StartEasyTier(args));
        _client = new PaperConnectClient($"127.0.0.1", serverPort, ClientPlayer);
        _client.OnPlayerInfoUpdated = OnPlayerInfoUpdated;

        // 等待连接建立
        AgreementEntry.PingResponse pingResponse = null;
        var retryCount = 0;
        const int maxRetries = 30;
        
        while (retryCount < maxRetries)
        {
            try
            {
                pingResponse = _client.PingAsync().Result;
                if (pingResponse != null)
                {
                    Console.WriteLine($@"GamePort: {pingResponse.GamePort}");
                    Console.WriteLine($@"GameProtocolType: {pingResponse.GameProtocolType}");
                    Console.WriteLine($@"GameType: {pingResponse.GameType}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Ping attempt {retryCount + 1} failed: {ex.Message}");
            }

            retryCount++;
            Thread.Sleep(1000);
        }

        if (pingResponse == null)
        {
            Console.WriteLine(@"Failed to connect to server");
            return;
        }

        Stop();

        args = $"-d --network-name {roomCodeInfo.NetworkName} " +
               $"--network-secret {roomCodeInfo.NetworkKey} " +
               string.Join(" ", argsEntry) +
               " -p " +
               string.Join(" -p ", serverEntry) +
               $" --port-forward tcp://0.0.0.0:{serverPort}/10.144.144.1:{serverPort}" +
               $" --port-forward udp://0.0.0.0:{GamePort}/10.144.144.1:{GamePort}";

        Task.Run(() => StartEasyTier(args));
        _client.OnPlayerInfoUpdated = OnPlayerInfoUpdated;

        retryCount = 0;
        while (retryCount < maxRetries)
        {
            try
            {
                var result = _client.PingAsync().Result;
                if (result != null)
                {
                    Console.WriteLine($@"GamePort: {result.GamePort}");
                    Console.WriteLine($@"GameProtocolType: {result.GameProtocolType}");
                    Console.WriteLine($@"GameType: {result.GameType}");
                    break;
                }
            }
            catch
            {
                // 忽略连接错误
            }

            retryCount++;
            Thread.Sleep(1000);
        }

        LinkSuccess?.Invoke();
        _client.StartHeartbeat();
    }
}