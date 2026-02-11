using Avalonia;
using System;
using System.IO;
using Round.SDK.Logger;

namespace PaperConnect.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var consoleRedirector = new ConsoleRedirector(Path.Combine(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
                "PaperConnect", "PaperConnect.Desktop", "PaperConnect.Logs"),
            $"[BedrockBoot.Logger] {DateTime.Now.ToString("yyyy.MM.dd HHmmss.fff")}.log"));
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}