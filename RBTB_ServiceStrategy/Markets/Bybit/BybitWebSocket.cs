using BybitMapper.UTA.MarketStreamsV5;
using BybitMapper.UTA.MarketStreamsV5.Events;
using BybitMapper.UTA.MarketStreamsV5.Events.Subscriptions;
using BybitMapper.UTA.UserStreamsV5;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace RBTB_ServiceStrategy.Markets.Bybit;

public class BybitWebSocket
{
    private WebSocket _socket;
    internal MarketStreamsHandlerCompositionV5 MarketStreams { get; }
    internal UserStreamsHandlerCompositionV5 UserStreams { get; }


    public string Symbol { get; set; } = "BTCUSDT";
    public BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType EndpointType { get; set; } = BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Trade;
    public delegate void DepthEvent(OrderbookEvent bookEvent);
    public event DepthEvent? DepthEv;
    public delegate void TickEvent(TickerEvent tickEvent);
    public event TickEvent? TickEv;
    public delegate void TradesEvent(TradeEvent tradesEvent);
    public event TradesEvent? TradeEv;
    public delegate void ExecEvent(BybitMapper.UTA.UserStreamsV5.Events.BaseEvent exec);
    public event ExecEvent? ExecEv;
    public delegate void UserEvent(BybitMapper.UTA.UserStreamsV5.Events.BaseEvent exec);
    public event UserEvent? UserEv;
    public delegate void KlineEvent(BybitMapper.UTA.MarketStreamsV5.Events.KlineEvent exec);
    public event KlineEvent? KlineEv;

    public delegate void ErrorHandler(object sender, ErrorEventArgs e);
    public event ErrorHandler? ErrorEv;
    public delegate void CloseHandler(object sender, CloseEventArgs e);
    public event CloseHandler? CloseEv;

    private static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public BybitWebSocket(string url)
    {
        _socket = new WebSocket(url);
        MarketStreams = new MarketStreamsHandlerCompositionV5();
        UserStreams = new UserStreamsHandlerCompositionV5();
    }

    private static T? Deserialize<T>(byte[] message)
    {
        using (MemoryStream stream = new MemoryStream(message, 0, message.Length))
        {
            return JsonSerializer.Deserialize<T>(stream, jsonSerializerOptions);
        }
    }
    private void SocketOnOnMessage(object sender, MessageEventArgs e)
    {
        DefaultSpotEvent? baseEvent = null;
        BybitMapper.UTA.UserStreamsV5.Events.BaseEvent? defaultEvent = null;

        try
        {
            baseEvent = MarketStreams.HandleDefaultSpotEvent(e.Data);
        }
        catch (Exception)
        {
        }

        try
        {
            defaultEvent = UserStreams.HandleDefaultEvent(e.Data);
        }
        catch (Exception)
        {
        }

        if (baseEvent != null)
        {
            if (baseEvent.WSEventType == BybitMapper.UTA.MarketStreamsV5.Data.Enums.EventType.Orderbook)
            {
                var data = Deserialize<OrderbookEvent>(e.RawData)!;
                DepthEv?.Invoke(data);
            }
            else if (baseEvent.WSEventType == BybitMapper.UTA.MarketStreamsV5.Data.Enums.EventType.Tickers)
            {
                var data = Deserialize<TickerEvent>(e.RawData)!;
                TickEv?.Invoke(data);
            }
            else if (baseEvent.WSEventType == BybitMapper.UTA.MarketStreamsV5.Data.Enums.EventType.Trade)
            {
                var data = Deserialize<BybitMapper.UTA.MarketStreamsV5.Events.TradeEvent>(e.RawData)!;
                TradeEv?.Invoke(data);
            }
            else if (baseEvent.WSEventType == BybitMapper.UTA.MarketStreamsV5.Data.Enums.EventType.Kline)
            {
                var data = Deserialize<BybitMapper.UTA.MarketStreamsV5.Events.KlineEvent>(e.RawData)!;
                KlineEv?.Invoke(data);
            }
        }

        if (defaultEvent != null)
        {
            if (defaultEvent.WSEventType == BybitMapper.UTA.UserStreamsV5.Data.Enums.EventType.Execution)
            {
                var useEvent = UserStreams.HandleDefaultEvent(e.Data);
                ExecEv?.Equals(useEvent);
            }
        }
    }

    public void PublicSubscribe(string symbol,
         BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType endpointType,
        BybitMapper.UTA.RestV5.Data.Enums.IntervalType intervalType = BybitMapper.UTA.RestV5.Data.Enums.IntervalType.Unrecognized)
    {
        var cmd = BybitMapper.UTA.MarketStreamsV5.Subscriptions.CombineStreamsSubsV5.Create(symbol, endpointType,
            BybitMapper.UTA.MarketStreamsV5.Data.Enums.SubType.Subscribe, intervalType);
        _socket.Send(cmd);
    }

    public void Start()
    {
        _socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        _socket.OnMessage += SocketOnOnMessage!;
        _socket.Connect();
        _socket.OnError += SocketOnOnError!;
        _socket.OnClose += SocketOnOnClose!;
    }

    public void SocketOnOnClose(object sender, CloseEventArgs e)
    {
        CloseEv?.Invoke(sender, e);
    }

    public void SocketOnOnError(object sender, ErrorEventArgs e)
    {
        ErrorEv?.Invoke(sender, e);
    }

    public void Stop()
    {
        _socket.Close();
    }
}
