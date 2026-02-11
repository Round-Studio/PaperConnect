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

    private void CreatBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(CreatPort.Text) || string.IsNullOrEmpty(CreatName.Text))
        {
            return;
        }
        
        var hostName = CreatName.Text;
        var port = int.Parse(CreatPort.Text);

        MainView.NavigationFrame.NavigateTo(new MainRoomServer(port, hostName));
    }

    private void AddBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(AddCode.Text) || string.IsNullOrEmpty(AddPlayer.Text))
        {
            return;
        }
        
        var code = AddCode.Text;
        var player = AddPlayer.Text;

        MainView.NavigationFrame.NavigateTo(new MainRoomClient(code, player));
    }
}