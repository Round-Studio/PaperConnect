using System.Diagnostics;
using PaperConnect.Core;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Room;

public class PaperConnectCore
{
    public required string EasyTierPath { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public int GamePort { get; set; } = 19132;
    
    private Process _easyTierProcess;

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
            var args = $"--service -i 10.144.144.1 --hostname paper-connect-server-{server.ServerPort} " +
                      $"--network-name {roomCodeInfo.NetworkName} --network-secret {roomCodeInfo.NetworkKey} " +
                      $"--enable-kcp-proxy --multi-thread --no-tun";

            StartEasyTier(args);
        }
        else if (coreType == CoreType.Client)
        {
            if (string.IsNullOrEmpty(RoomCode)) 
                throw new NullReferenceException("RoomCode");
            
            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            
            // 启动 EasyTier 客户端
            var args = $"--service --network-name {roomCodeInfo.NetworkName} " +
                      $"--network-secret {roomCodeInfo.NetworkKey} " +
                      $"--enable-kcp-proxy --multi-thread --no-tun";

            StartEasyTier(args);
        }
    }

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
}