namespace RBTB_ServiceStrategy.Domain.Options;

public class LevelStrategyOption
{
    public string WsUrl { get; set; }
    public string Url { get; set; }
    public string Symbol { get; set; }
    public decimal ScopePrice { get; set; }
    public bool IsStart { get; set; }
}