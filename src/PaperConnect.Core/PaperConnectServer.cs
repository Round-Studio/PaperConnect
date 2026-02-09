using PaperConnect.Core.Interface;

namespace PaperConnect.Core;

public class PaperConnectServer : IEasyTierClient
{
    public string EasyTierMain { get; set; } = string.Empty;
    public string EasyTierCli { get; set; } = string.Empty;
}