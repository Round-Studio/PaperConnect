using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using PaperConnect.Desktop.Views.Pages.DialogContent;

namespace PaperConnect.Desktop.Views.Pages.MainPages;

public partial class MainHome : UserControl
{
    public MainHome()
    {
        InitializeComponent();
    }

    private void CreatRoomBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogCreatRoomContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = "创建房间",
            Content = dialog,
            CloseButtonText = "创建",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                var hostName = dialog.RoomHostName;
                var port = dialog.RoomNumber;

                MainView.NavigationFrame.NavigateTo(new MainRoomServer(port, hostName));
            }
        });
    }
}