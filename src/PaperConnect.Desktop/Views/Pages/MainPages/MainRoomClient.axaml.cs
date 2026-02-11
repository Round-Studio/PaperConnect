using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using PaperConnect.Core.Enum;
using PaperConnect.Desktop.Module;

namespace PaperConnect.Desktop.Views.Pages.MainPages;

public partial class MainRoomClient : UserControl
{
    public MainRoomClient()
    {
        InitializeComponent();
    }
    private PaperConnectCore PaperConnectCore { get; set; }

    public MainRoomClient(string code, string player) : this()
    {
        PaperConnectCore = new PaperConnectCore()
        {
            EasyTierPath = GlobalModule.EasyTierCore,
            EasyTierCliPath = GlobalModule.EasyTierCli
        };
        PaperConnectCore.RoomCode = code;
        PaperConnectCore.ClientPlayer = player;

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

        DialogHost.Show(new DialogInfo()
        {
            Title = "正在连接",
            Content = "正在连接至房间，完成后将自动关闭该对话框"
        });

        PaperConnectCore.LinkSuccess = () =>
            Dispatcher.UIThread.Invoke(DialogHost.Close);
        
        Task.Run(() => PaperConnectCore.Initialize(CoreType.Client));
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