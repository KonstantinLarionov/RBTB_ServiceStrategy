﻿using BybitMapper.UTA.MarketStreamsV5;
using BybitMapper.UTA.MarketStreamsV5.Events;
using BybitMapper.UTA.MarketStreamsV5.Events.Subscriptions;
using BybitMapper.UTA.UserStreamsV5;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using BybitMapper.UTA.MarketStreamsV5.Data.Enums;
using BybitMapper.UTA.RestV5.Data.Enums;
using System.Text;

namespace RBTB_ServiceStrategy.Markets.Bybit;
public class BybitWebSocket
{
    private WebSocket _socket;
    internal MarketStreamsHandlerCompositionV5 MarketStreams { get; }
    internal UserStreamsHandlerCompositionV5 UserStreams { get; }

    public string Symbol { get; set; } = "BTCUSDT";
    public PublicEndpointType EndpointType { get; set; } = PublicEndpointType.Trade;
    public delegate void DepthHandler(OrderbookEvent bookEvent);
    public event DepthHandler? DepthEvent;
    public delegate void TickHandler(TickerEvent tickEvent);
    public event TickHandler? TickEvent;
    public delegate void TradesHandler(TradeEvent tradesEvent);
    public event TradesHandler? TradeEvent;
    public delegate void ExecHandler(BaseEvent exec);
    public event ExecHandler? ExecEvent;
    public delegate void KlineHanler(KlineEvent exec);
    public event KlineHanler? KlineEvent;
    public delegate void ErrorHandler(object sender, Exception ex);
    public event ErrorHandler? ErrorEvent;
    public delegate void CloseHandler(object sender, CloseEventArgs e);
    public event CloseHandler? CloseEvent;
    public delegate void OpenHandler(object sender, EventArgs e);
    public event OpenHandler? OpenEvent;

    private int _reconnectCounter = 40;
    private int _WSreconnectCounter;

    private static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public BybitWebSocket(string url, int counterReconnect = 40)
    {
        _socket = new WebSocket(url);
        MarketStreams = new MarketStreamsHandlerCompositionV5();
        UserStreams = new UserStreamsHandlerCompositionV5();

        _reconnectCounter = counterReconnect;
    }

    private static T? Deserialize<T>(byte[] message)
    {
        using (MemoryStream stream = new MemoryStream(message, 0, message.Length))
        {
            return JsonSerializer.Deserialize<T>(stream, jsonSerializerOptions);
        }
    }

    private void SocketOnMessage(object sender, MessageEventArgs e)
    {
        DefaultSpotEvent? baseEvent = null;
        BybitMapper.UTA.UserStreamsV5.Events.BaseEvent? defaultEvent = null;

        try
        {
            baseEvent = MarketStreams.HandleDefaultSpotEvent(e.Data);
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke(sender, ex);
        }

        try
        {
            defaultEvent = UserStreams.HandleDefaultEvent(e.Data);
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke(sender, ex);
        }

        if (baseEvent != null)
        {
            if (baseEvent.WSEventType == EventType.Orderbook)
            {
                var data = Deserialize<OrderbookEvent>(e.RawData)!;
                DepthEvent?.Invoke(data);
            }
            else if (baseEvent.WSEventType == EventType.Tickers)
            {
                var data = Deserialize<TickerEvent>(e.RawData)!;
                TickEvent?.Invoke(data);
            }
            else if (baseEvent.WSEventType == EventType.Trade)
            {
                var data = Deserialize<TradeEvent>(e.RawData)!;
                TradeEvent?.Invoke(data);
            }
            else if (baseEvent.WSEventType == EventType.Kline)
            {
                var data = Deserialize<KlineEvent>(e.RawData)!;
                KlineEvent?.Invoke(data);
            }
        }

        if (defaultEvent != null)
        {
            if (defaultEvent.WSEventType == BybitMapper.UTA.UserStreamsV5.Data.Enums.EventType.Execution)
            {
                var useEvent = UserStreams.HandleDefaultEvent(e.Data);
                ExecEvent?.Equals(useEvent);
            }
        }
    }

    public void PublicSubscribe(string symbol, PublicEndpointType endpointType,
        IntervalType intervalType = IntervalType.Unrecognized)
    {
        var request = BybitMapper.UTA.MarketStreamsV5.Subscriptions.CombineStreamsSubsV5.Create(symbol, endpointType,
            SubType.Subscribe, intervalType);
        _socket.Send(request);
    }

    public void Ping()
    {
        _socket.Send(Encoding.UTF8.GetBytes("ping"));
    }

    public void Start()
    {
        _socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        _socket.OnMessage += SocketOnMessage!;
        _socket.OnError += SocketOnError!;
        _socket.OnClose += SocketOnClose!;
        _socket.OnOpen += SocketOnOpen!;

        _socket.Connect();
    }

    public void SocketOnClose(object sender, CloseEventArgs e)
    {
        if(!e.WasClean)
        {
            if (!_socket.IsAlive && _WSreconnectCounter > 0)
            {
                _WSreconnectCounter--;
                Console.WriteLine($"Попытка переподключения. Осталось попыток {_WSreconnectCounter}");

                Thread.Sleep(1500);
                _socket.Connect();
            }
        }

        CloseEvent?.Invoke(sender, e);
    }

    public void SocketOnError(object sender, ErrorEventArgs e)
    {
        ErrorEvent?.Invoke(sender, e.Exception);
    }

    public void SocketOnOpen(object sender, EventArgs e)
    {
        _WSreconnectCounter = _reconnectCounter;

        OpenEvent?.Invoke(sender, e);
    }

    public void Stop()
    {
        _socket.Close();
    }
}