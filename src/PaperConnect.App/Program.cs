using PaperConnect.Core;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Room;

public class Program
{
    public static async Task Main(string[] args)
    {
        var ser = new PaperConnectCore()
        {
            EasyTierPath = "D:/ET/easytier-core.exe",
            EasyTierCliPath = "D:/ET/easytier-cli.exe",
            RoomCode = "P/KYVZ-HA9V-LDRY-96JE"
        };
        ser.Initialize(CoreType.Client);
    }
}