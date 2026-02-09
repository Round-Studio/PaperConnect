using System.Diagnostics;
using PaperConnect.Core.Enum;
using PaperConnect.Core.Room;

namespace PaperConnect.Core;

public class PaperConnectCore
{
    public required string EasyTierPath { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public int GamePort { get; set; } = 19132;

    public void Initialize(CoreType coreType)
    {
        if (string.IsNullOrEmpty(EasyTierPath)) throw new NullReferenceException("EasyTierPath");

        if (coreType == CoreType.Server)
        {
            RoomCode = RoomCodeGenerator.GenerateRoomCode();
            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            var server = new PaperConnectServer("Dime", GamePort);

            var args =
                $"-i 10.144.144.1 --hostname paper-connect-server-{server.ServerPort} --network-name {roomCodeInfo.NetworkName} --network-secret {roomCodeInfo.NetworkKey} --no-tun";
            Process.Start(EasyTierPath, args);
            server.StartAsync().Wait();
        }
        else if (coreType == CoreType.Client)
        {
            if (string.IsNullOrEmpty(RoomCode)) throw new NullReferenceException("RoomCode");
            
            var roomCodeInfo = RoomCodeGenerator.ParseRoomCode(RoomCode);
            var args =
                $"--network-name {roomCodeInfo.NetworkName} --network-secret {roomCodeInfo.NetworkKey} --no-tun";
        }
    }
}