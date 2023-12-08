using BybitMapper.UTA.RestV5.Responses.Market;
using NUnit.Framework.Legacy;
using RBTB_ServiceStrategy.Markets.Bybit;

namespace BybitTests;

public class BybitRestClientTests
{
    private string _symbol = "BTCUSDT";
    private const string _url = "https://api.bybit.com";
    private BybitRestClient _bybitRestClient;
    private string _api = "6CjIuMxhY8DAhWu03C";
    private string _secret = "DoGlywXMEp8pvt9s5L2vYwIuoXBUnnSulHby";

    [SetUp]
    public void Setup()
    {
        _bybitRestClient = new BybitRestClient(_url, _api, _secret);
    }

    [Test]
    public void RestMarketGetServerTimeTest()
    {
        var response = _bybitRestClient.RequestGetServerTime();
        ClassicAssert.NotNull(response);
    }

    [Test]
    public void RestGetOrderbookTest()
    {
        var response = _bybitRestClient.RequestGetOrderbook(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Option, _symbol);
        ClassicAssert.NotNull(response);
    }

    [Test]
    public void RestGetKlineTest()
    {
        var response = _bybitRestClient.RequestGetKline(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Linear, _symbol, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FifteenMinute);
        ClassicAssert.NotNull(response);
    }
}