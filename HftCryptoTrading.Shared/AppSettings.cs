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

public class RuntimeSetting
{
    public string IndicatorsPath { get; set; } = "wwwroot/indicators";
    public string StrategiesPath { get; set; } = "wwwroot/strategies";
}

public class Trading
{
    public TimeSpan Period { get; set; } = TimeSpan.FromHours(1);
    public int StartElpasedTime { get; set; } = 60 * 500;
}

public class AppSettings
{
    public int LimitSymbolsMarket { get; set; }
    public BinanceSetting Binance { get; set; }
    public HubSetting Hub { get; set; }
    public RuntimeSetting Runtime { get; set; }
    public Trading Trading { get; set; }
}