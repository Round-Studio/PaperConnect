using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PaperConnect.Desktop.Views.Pages.DialogContent;

public partial class DialogCreatRoomContent : UserControl
{
    public int RoomNumber => int.TryParse(Port.Text, out _) ? int.Parse(Port.Text) : 19132;
    public string RoomHostName => HostName.Text;
    public DialogCreatRoomContent()
    {
        InitializeComponent();
    }
}