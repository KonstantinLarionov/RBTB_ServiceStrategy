namespace RBTB_ServiceStrategy.Domain.States;

public static class WsEvents
{
    public delegate void StrategyTradeEvent(decimal price, string symbol, decimal level);
    public static event StrategyTradeEvent StrategyTradeEv;

    public static void InvokeSTE(decimal price, string symbol, decimal level) => StrategyTradeEv?.Invoke(price, symbol, level);
}