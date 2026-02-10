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
            RoomCode = "P/F50H-FXQB-Y8NQ-YRN9"
        };
        ser.Initialize(CoreType.Client);
    }
}