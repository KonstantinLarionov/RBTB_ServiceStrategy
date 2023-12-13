using NUnit.Framework;
using RBTB_ServiceStrategy.Markets.Bybit;

namespace BybitTests;

public class BybitWebSocketTests
{
    private string _symbol = "BTCUSDT";
    private const string _spotUrlWs = "wss://stream.bybit.com/v5/public/spot";

    [Test]
    public void SocketPublicSpotKlineTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs) { Symbol = _symbol };

        socket.KlineEvent += (BybitMapper.UTA.MarketStreamsV5.Events.KlineEvent kline) =>
        {
            Assert.That(kline, Is.Not.Null);
            Assert.That(kline.Data, Is.Not.Null);
        };
        socket.ErrorEvent += (sender, message) => Assert.Fail(message.Message);

        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Kline, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);

        Thread.Sleep(10000);
    }

    [Test]
    public void SocketPublicOrderbookTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs) { Symbol = _symbol };

        socket.DepthEvent += (BybitMapper.UTA.MarketStreamsV5.Events.OrderbookEvent kline) =>
        {
            Assert.That(kline, Is.Not.Null);
            Assert.That(kline.Data, Is.Not.Null);
        };
        socket.ErrorEvent += (sender, message) => Assert.Fail(message.Message);

        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Orderbook, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);

        Thread.Sleep(10000);
    }

    [Test]
    public void SocketPublicTradeTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs) { Symbol = _symbol };

        socket.TradeEvent += (BybitMapper.UTA.MarketStreamsV5.Events.TradeEvent kline) =>
        {
            Assert.That(kline, Is.Not.Null);
            Assert.That(kline.Data, Is.Not.Null);
        };
        socket.ErrorEvent += (sender, message) => Assert.Fail(message.Message);

        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Trade, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);

        Thread.Sleep(10000);
    }

    [Test]
    public void SocketPublicTickerTest()
    {
        var socket = new BybitWebSocket(_spotUrlWs) { Symbol = _symbol };

        socket.TickEvent += (BybitMapper.UTA.MarketStreamsV5.Events.TickerEvent kline) =>
        {
            Assert.That(kline, Is.Not.Null);
            Assert.That(kline.Data, Is.Not.Null);
        };
        socket.ErrorEvent += (sender, message) => Assert.Fail(message.Message);

        socket.Start();
        socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Tickers, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FiveMinute);

        Thread.Sleep(10000);
    }
}