using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;
using PaperConnect.Desktop.Views.Pages.MainPages;

namespace PaperConnect.Desktop.Views.Pages;

public partial class MainView : UserControl
{
    public static NavigationFrame NavigationFrame { get; private set; }
    public MainView()
    {
        InitializeComponent();
        
        MainFrame.NavigateTo(new MainHome());
        NavigationFrame = MainFrame;
    }
}