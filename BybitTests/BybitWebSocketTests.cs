using NUnit.Framework.Legacy;
using RBTB_ServiceStrategy.Markets.Bybit;

namespace BybitTests;

public class BybitWebSocketTests
{
    private string _symbol = "BTCUSDT";
    private const string _spotUrlWs = "wss://stream.bybit.com/v5/public/spot";

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void SocketPublicSpotKlineTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs);
        socket.Symbol = _symbol;
        socket.KlineEv += (BybitMapper.UTA.MarketStreamsV5.Events.KlineEvent kline) =>
        {
            ClassicAssert.NotNull(kline);
            ClassicAssert.NotNull(kline.Data);
        };
        socket.ErrorEv += (sender, message) =>
        {
            Assert.Fail(message.Message);
        };
        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Kline, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);
        Thread.Sleep(10000);
    }

    [Test]
    public void SocketPublicOrderbookTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs);
        socket.Symbol = _symbol;
        socket.DepthEv += (BybitMapper.UTA.MarketStreamsV5.Events.OrderbookEvent kline) =>
        {
            ClassicAssert.NotNull(kline);
            ClassicAssert.NotNull(kline.Data);
        };
        socket.ErrorEv += (sender, message) =>
        {
            Assert.Fail(message.Message);
        };
        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Orderbook, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);
        Thread.Sleep(10000);
    }

    [Test]
    public void SocketPublicTradeTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs);
        socket.Symbol = _symbol;
        socket.TradeEv += (BybitMapper.UTA.MarketStreamsV5.Events.TradeEvent kline) =>
        {
            ClassicAssert.NotNull(kline);
            ClassicAssert.NotNull(kline.Data);
        };
        socket.ErrorEv += (sender, message) =>
        {
            Assert.Fail(message.Message);
        };
        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Trade, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);
        Thread.Sleep(10000);
    }

    [Test]
    public void SocketPublicTickerTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs);
        socket.Symbol = _symbol;
        socket.TickEv += (BybitMapper.UTA.MarketStreamsV5.Events.TickerEvent kline) =>
        {
            ClassicAssert.NotNull(kline);
            ClassicAssert.NotNull(kline.Data);
        };
        socket.ErrorEv += (sender, message) =>
        {
            Assert.Fail(message.Message);
        };
        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Tickers, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);
        Thread.Sleep(10000);
    }
}