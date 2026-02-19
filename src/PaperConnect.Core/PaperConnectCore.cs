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
    private bool _isClient = false;
   
	public void Initialize(CoreType coreType,List<string> etPublicser)
    {
        CoreType = coreType;
	    if (File.Exists("cofig.toml"))
	    {
		    File.Delete("config.toml");
	    }
		if (string.IsNullOrEmpty(EasyTierPath)) 
            throw new NullReferenceException("EasyTierPath");

        // 验证 EasyTier 文件是否存在
        if (!File.Exists(EasyTierPath))
            throw new FileNotFoundException($"EasyTier not found at: {EasyTierPath}");

        var argsJson = EmbeddedResourceHelper.ReadEmbeddedResource(Assembly.GetExecutingAssembly(),
            "PaperConnect.Core.Manifest.EasyTierParameter.json");
        var serverJson = etPublicser;
       
        var argsEntry = JsonSerializer.Deserialize<List<string>>(argsJson);
        var serverEntry = JsonSerializer.Deserialize<List<string>>(serverJson);


        if (coreType == CoreType.Server)
        {
	        Root acl = PaperConnectAclBuilder.BuildPaperConnectAcl(true, "10.144.144.1",null);
			var fromModel = Toml.FromModel(acl);
			File.WriteAllText("config.toml", fromModel);
			RoomCode = RoomCodeGenerator.GenerateRoomCode();
            Console.WriteLine($"RoomCode: {RoomCode}");
            
            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            var server = new PaperConnectServer(ClientPlayer, GamePort);
            server.OnPlayerInfoUpdated = OnPlayerInfoUpdated;

            // 启动服务器
            _ = Task.Run(() => server.StartAsync());

            // 启动 EasyTier 服务端
            var args = $"-i 10.144.144.1 --hostname paper-connect-server-{server.ServerPort} " +
                       $"--network-name {roomCodeInfo.NetworkName} --network-secret {roomCodeInfo.NetworkKey} " +
                    //   $"--tcp-whitelist {server.ServerPort} --udp-whitelist {GamePort} " +
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
            
            // 启动 EasyTier 客户端
            var args = $"-d --network-name {roomCodeInfo.NetworkName} " +
                       $"--network-secret {roomCodeInfo.NetworkKey} " +
                       string.Join(" ", argsEntry) +
                       " -p " +
                       string.Join(" -p ", serverEntry);

            StartEasyTier(args);
        }
    }
    bool isStart = false;

    private void StartEasyTier(string arguments)
    {
        try
        {
            Console.WriteLine($"Starting EasyTier with args: {arguments}");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = EasyTierPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _easyTierProcess = new Process { StartInfo = startInfo };
            
            // 添加输出事件处理
            _easyTierProcess.OutputDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"[EasyTier] {e.Data}");

                if (!isStart && 
                    _isClient)
                {
                    if (e.Data.Contains("new peer added."))
                    {
                        isStart = true;
                        var json = GetPeers();

                        json.ForEach(p =>
                        {
                            if (p.Hostname.Contains(RoomCodeGenerator.ROOM_NAME))
                            {
                                Console.WriteLine(p.Hostname);
                                StartClient(p.Hostname);
                            }
                        });
                    }
                }
            };
            
            _easyTierProcess.ErrorDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"[EasyTier ERROR] {e.Data}");
            };

            if (_easyTierProcess.Start())
            {
                _easyTierProcess.BeginOutputReadLine();
                _easyTierProcess.BeginErrorReadLine();
                
                Console.WriteLine($"EasyTier started with PID: {_easyTierProcess.Id}");
            }
            else
            {
                Console.WriteLine("Failed to start EasyTier process");
            }

            _easyTierProcess.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting EasyTier: {ex.Message}");
            throw;
        }
    }
    public void Stop()
    {
        try
        {
            _easyTierProcess.Kill();
            _easyTierProcess.WaitForExit(5000);
            Console.WriteLine("EasyTier stopped");
            
            var processes = Process.GetProcesses()
                .Where(p => p.ProcessName.Equals("easytier-core.exe",StringComparison.OrdinalIgnoreCase))
                .ToList();

            processes.ForEach(p => p.Kill(true));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping EasyTier: {ex.Message}");
        }
    }
    private string StartEasyTierCli(string arguments)
    {
        var sb = new StringBuilder();
        try
        {
            Console.WriteLine($"Starting EasyTierCli with args: {arguments}");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = EasyTierCliPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var etCli = new Process { StartInfo = startInfo };
            
            // 添加输出事件处理
            etCli.OutputDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    sb.AppendLine(e.Data);
            };
            
            etCli.ErrorDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"[EasyTierCli ERROR] {e.Data}");
            };

            if (etCli.Start())
            {
                etCli.BeginOutputReadLine();
                etCli.BeginErrorReadLine();
                
                Console.WriteLine($"EasyTierCli started with PID: {etCli.Id}");
            }
            else
            {
                Console.WriteLine("Failed to start EasyTierCli process");
            }

            etCli.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting EasyTierCli: {ex.Message}");
            throw;
        }

        return sb.ToString();
    }
    private List<PeerInfo> GetPeers()
    {
        var json = StartEasyTierCli("-o json peer");
        return JsonSerializer.Deserialize<List<PeerInfo>>(json);
    }

    private void StartClient(string hostName)
    {
        var argsJson = EmbeddedResourceHelper.ReadEmbeddedResource(Assembly.GetExecutingAssembly(),
            "PaperConnect.Core.Manifest.EasyTierParameter.json");
        var serverJson = EmbeddedResourceHelper.ReadEmbeddedResource(Assembly.GetExecutingAssembly(),
            "PaperConnect.Core.Manifest.PublicServerList.json");

        var argsEntry = JsonSerializer.Deserialize<List<string>>(argsJson);
        var serverEntry = JsonSerializer.Deserialize<List<string>>(serverJson);

        Stop();
        var serverPort = int.Parse(hostName.Replace($"{RoomCodeGenerator.ROOM_NAME}-server-", ""));
        Console.WriteLine($"Host Port: {serverPort}");

        var args = $"-d --network-name {RoomCodeGenerator.ParseRoomCode(RoomCode).NetworkName} " +
                   $"--network-secret {RoomCodeGenerator.ParseRoomCode(RoomCode).NetworkKey} " +
                   string.Join(" ", argsEntry) +
                   " -p " +
                   string.Join(" -p ", serverEntry) +
                   $" --port-forward tcp://0.0.0.0:{serverPort}/10.144.144.1:{serverPort}";
        Task.Run(() => StartEasyTier(args));
        var client = new PaperConnectClient($"127.0.0.1", serverPort, ClientPlayer);
        client.OnPlayerInfoUpdated = OnPlayerInfoUpdated;

        AgreementEntry.PingResponse pingResponse = null;
        while (true)
        {
            try
            {
                pingResponse = client.PingAsync().Result;
                if (pingResponse != null)
                {
                    Console.WriteLine(pingResponse.GamePort);
                    Console.WriteLine(pingResponse.GameProtocolType);
                    Console.WriteLine(pingResponse.GameType);
                    break;
                }
            }
            catch
            {
            }

            Thread.Sleep(1000);
        }

        Stop();

        args = $"-d --network-name {RoomCodeGenerator.ParseRoomCode(RoomCode).NetworkName} " +
               $"--network-secret {RoomCodeGenerator.ParseRoomCode(RoomCode).NetworkKey} " +
               string.Join(" ", argsEntry) +
               " -p " +
               string.Join(" -p ", serverEntry) +
               $" --port-forward tcp://0.0.0.0:{serverPort}/10.144.144.1:{serverPort}" +
               $" --port-forward udp://0.0.0.0:{GamePort}/10.144.144.1:{GamePort}";

        Task.Run(() => StartEasyTier(args));

        while (true)
        {
            try
            {
                var result = client.PingAsync().Result;
                if (result != null)
                {
                    Console.WriteLine(result.GamePort);
                    Console.WriteLine(result.GameProtocolType);
                    Console.WriteLine(result.GameType);
                    break;
                }
            }
            catch
            {
            }

            Thread.Sleep(1000);
        }
        LinkSuccess?.Invoke();
        client.StartHeartbeat();
        while (true) ;
    }
}