using System.Diagnostics;
using PaperConnect.Core;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Room;

public class Program
{
    public static async Task Main(string[] args)
    {
        Process.GetProcessesByName("easytier-core").ToList().ForEach(p => p.Kill(true));
        var ser = new PaperConnectCore()
        {
            EasyTierPath = "easytier-core.exe",
            EasyTierCliPath = "easytier-cli.exe"
        };
        ser.OnPlayerInfoUpdated = list => list.ForEach(p => Console.WriteLine($"玩家心跳：{p.PlayerName} {p.ClientId}"));
        
        Console.WriteLine("选择模式(输入数字):\n" +
                          "1.Server\n" +
                          "2.Client");

        var mod = Console.ReadLine();
        if (mod.Contains("1"))
        {
            Console.Write("端口:");
            var gamePort = Console.ReadLine();
            ser.GamePort = int.Parse(gamePort);
            ser.Initialize(CoreType.Server);
            
            Console.WriteLine($"房间码：{ser.RoomCode}");
        }
        else if(mod.Contains("2"))
        {
            Console.Write("联机码:");
            var roomCode = Console.ReadLine();
            Console.Write("玩家:");
            var player = Console.ReadLine();
            ser.RoomCode = roomCode;
            ser.ClientPlayer = player;
            ser.Initialize(CoreType.Client);
        }
    }
}