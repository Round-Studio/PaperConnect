using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PaperConnect.Core;
using PaperConnect.Core.Entry;
using PaperConnect.Core.Entry.Easytier;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Room;

public class PaperConnectCore
{
    public required string EasyTierPath { get; set; }
    public required string EasyTierCliPath { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public int GamePort { get; set; } = 19132;
    public static string PublicServer = "";
    
    private Process _easyTierProcess;
    private bool _isClient = false;

    public void Initialize(CoreType coreType)
    {
        if (string.IsNullOrEmpty(EasyTierPath)) 
            throw new NullReferenceException("EasyTierPath");

        // 验证 EasyTier 文件是否存在
        if (!File.Exists(EasyTierPath))
            throw new FileNotFoundException($"EasyTier not found at: {EasyTierPath}");

        if (coreType == CoreType.Server)
        {
            RoomCode = RoomCodeGenerator.GenerateRoomCode();
            Console.WriteLine($"RoomCode: {RoomCode}");
            
            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            var server = new PaperConnectServer("Dime", GamePort);

            // 启动服务器
            _ = Task.Run(() => server.StartAsync());

            // 启动 EasyTier 服务端
            var args = $"-i 10.144.144.1 --hostname paper-connect-server-{server.ServerPort} " +
                      $"--network-name {roomCodeInfo.NetworkName} --network-secret {roomCodeInfo.NetworkKey} " +
                      $"--multi-thread --no-tun -p {PublicServer} -l tcp://0.0.0.0:{server.ServerPort}";

            StartEasyTier(args);
        }
        else if (coreType == CoreType.Client)
        {
            _isClient = true;
            if (string.IsNullOrEmpty(RoomCode)) 
                throw new NullReferenceException("RoomCode");
            
            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            
            // 启动 EasyTier 客户端
            var args = $"--network-name {roomCodeInfo.NetworkName} " +
                      $"--network-secret {roomCodeInfo.NetworkKey} " +
                      $"--multi-thread --no-tun -p tcp://frp.tianpao.top:22876";

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
            if (_easyTierProcess != null && !_easyTierProcess.HasExited)
            {
                _easyTierProcess.Kill();
                _easyTierProcess.WaitForExit(5000);
                Console.WriteLine("EasyTier stopped");
            }
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

            _easyTierProcess = new Process { StartInfo = startInfo };
            
            // 添加输出事件处理
            _easyTierProcess.OutputDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    sb.AppendLine(e.Data);
            };
            
            _easyTierProcess.ErrorDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"[EasyTierCli ERROR] {e.Data}");
            };

            if (_easyTierProcess.Start())
            {
                _easyTierProcess.BeginOutputReadLine();
                _easyTierProcess.BeginErrorReadLine();
                
                Console.WriteLine($"EasyTierCli started with PID: {_easyTierProcess.Id}");
            }
            else
            {
                Console.WriteLine("Failed to start EasyTierCli process");
            }

            _easyTierProcess.WaitForExit();
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
        var serverPort = int.Parse(hostName.Replace($"{RoomCodeGenerator.ROOM_NAME}-server-", ""));
        Console.WriteLine($"Host Port: {serverPort}");

        var args = $"-i 10.144.144.2 " +
                   $"--network-name {RoomCodeGenerator.ParseRoomCode(RoomCode).NetworkName} " +
                   $"--network-secret {RoomCodeGenerator.ParseRoomCode(RoomCode).NetworkKey} " +
                   $"--multi-thread --no-tun " +
                   $"--tcp-whitelist {serverPort} " +
                   $"-p {PublicServer} " +
                   $"--port-forward tcp://0.0.0.0:{serverPort}/10.144.144.1:{serverPort}";
        Stop();
        Task.Run(() => StartEasyTier(args));
        var client = new PaperConnectClient($"0.0.0.0", serverPort, "YJQ");
        client.OnPlayerInfoUpdated = list => Console.WriteLine(list);

        while (true)
        {
            try
            {
                var result = client.PingAsync().Result;
                if (result != null)
                {
                    break;
                }
            }
            catch
            {
            }

            Thread.Sleep(1000);
        }

        client.StartHeartbeat();
        while (true) ;
    }
}