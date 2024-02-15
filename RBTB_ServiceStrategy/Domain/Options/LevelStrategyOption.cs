namespace RBTB_ServiceStrategy.Domain.Options;

public class LevelStrategyOption
{
    public string WsUrl { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Api { get; set; } = null!;
    public string Secret { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public decimal ScopePrice { get; set; }
    public bool IsStart { get; set; }
    public int CounterReconnectWS { get; set; }
}