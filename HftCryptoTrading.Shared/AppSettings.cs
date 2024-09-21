public class BinanceSetting
{
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
    public bool IsBackTest { get; set; }
}

public class HubSetting
{
    public string HubApiKey { get; set; }
    public string HubApiSecret { get; set; }
    public string HubApiUrl { get; set; }
    public string NameSpace { get; set; }
}

public class AppSettings
{
    public int LimitSymbolsMarket { get; set; }
    public BinanceSetting Binance { get; set; }
    public HubSetting Hub { get; set; }
}
