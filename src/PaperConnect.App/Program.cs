using PaperConnect.Core;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Room;

public class Program
{
    public static async Task Main(string[] args)
    {
        var ser = new PaperConnectCore()
        {
            EasyTierPath = "D:/ET/easytier-core.exe"
        };
        ser.Initialize(CoreType.Server);
    }
}