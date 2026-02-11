using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
    
    private PaperConnectCore PaperConnectCore { get; set; }

    public MainRoomServer(int gamePort, string hostName) : this()
    {
        PaperConnectCore = new PaperConnectCore()
        {
            EasyTierPath = GlobalModule.EasyTierCore,
            EasyTierCliPath = GlobalModule.EasyTierCli
        };
        PaperConnectCore.GamePort = gamePort;
        PaperConnectCore.ClientPlayer = hostName;

        PaperConnectCore.OnPlayerInfoUpdated = (list =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                PlayerCount.Text = $"联机人数：{list.Count}";
                RoomCode.Text = $"联机码：{PaperConnectCore.RoomCode}";
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

        Task.Run(() => PaperConnectCore.Initialize(CoreType.Server));
    }

    private void CopyCode_OnClick(object? sender, RoutedEventArgs e)
    {
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(PaperConnectCore.RoomCode);
    }

    private void CloseRoom_OnClick(object? sender, RoutedEventArgs e)
    {
        PaperConnectCore.Stop();
        
        MainView.NavigationFrame.NavigateTo(new MainHome());
    }
}