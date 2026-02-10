using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using PaperConnect.Core.Entry;
using PaperConnect.Core.Enum;
using PaperConnect.Desktop.Module;

namespace PaperConnect.Desktop.Views.Pages.MainPages;

public partial class MainRoomServer : UserControl
{
    public MainRoomServer()
    {
        InitializeComponent();
    }

    public MainRoomServer(int gamePort, string hostName) : this()
    {
        var ser = new PaperConnectCore()
        {
            EasyTierPath = GlobalModule.EasyTierCore,
            EasyTierCliPath = GlobalModule.EasyTierCli
        };
        ser.GamePort = gamePort;
        ser.ClientPlayer = hostName;

        ser.OnPlayerInfoUpdated = (list =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                PlayerCount.Text = $"联机人数：{list.Count}";
                RoomCode.Text = $"联机码：{ser.RoomCode}";
                PlayerList.Children.Clear();

                foreach (var player in list)
                {
                    PlayerList.Children.Add(new SettingCard()
                    {
                        Header = player.PlayerName,
                        Description = player.ClientId
                    });
                }
            });
        });

        Task.Run(() => ser.Initialize(CoreType.Server));
    }
}