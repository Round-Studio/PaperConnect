using PaperConnect.Core.Room;

var code = RoomCodeGenerator.GenerateRoomCode();
Console.WriteLine(code);

var info = RoomCodeGenerator.ParseRoomCode(code);
Console.WriteLine(info.NetworkName);
Console.WriteLine(info.NetworkKey);