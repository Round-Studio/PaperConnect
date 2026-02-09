using PaperConnect.Core;
using PaperConnect.Core.Room;

public class Program
{
    public static async Task Main(string[] args)
    {
        var code = RoomCodeGenerator.GenerateRoomCode();
        Console.WriteLine(code);

        var info = RoomCodeGenerator.ParseRoomCode(code);
        Console.WriteLine(info.NetworkName);
        Console.WriteLine(info.NetworkKey);

        if (args.Contains("-server"))
        {
            var server = new PaperConnectServer("Dime", 33768);
            server.OnPlayerInfoUpdated = list =>
            {
                list.ForEach(p => { Console.WriteLine($"玩家名称：{p.PlayerName}"); });
            };
            // 启动服务器
            var serverTask = server.StartAsync();
            
            Console.WriteLine("Server running. Press any key to stop...");
            Console.ReadKey();
            server.Stop(); // 停止服务器
        }
        else
        {
            // 从控制台读取端口号
            Console.Write("Enter server port: ");
            int port = int.Parse(Console.ReadLine());
            
            // 连接到房主服务器
            var client = new PaperConnectClient("192.168.110.21", port, "Steve");
            client.OnPlayerInfoUpdated = list =>
            {
                list.ForEach(p => { Console.WriteLine($"玩家名称：{p.PlayerName}"); });
            };

            try
            {
                // 测试连通性
                var ping = await client.PingAsync();
                if (ping != null)
                {
                    Console.WriteLine($"Latency: {ping.ReturnTime - ping.Time} ms");
                    Console.WriteLine($"Game Port: {ping.GamePort}");
                }

                // 开始自动心跳（关键：必须持续发送心跳）
                client.StartHeartbeat();
                
                Console.WriteLine("Client connected and sending heartbeats. Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                // 清理资源
                client.StopHeartbeat();
                client.Dispose();
            }
        }
    }
}