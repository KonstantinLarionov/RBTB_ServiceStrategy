using NUnit.Framework;
using RBTB_ServiceStrategy.Markets.Bybit;

namespace BybitTests;
public class BybitRestClientTests
{
    private string _symbol = "ADAUSDT";
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
    public async Task RestMarketGetServerTimeTest()
    {
        var response =  await _bybitRestClient.RequestGetServerTimeAsync();

        Assert.That(response, Is.Not.Null);
        Assert.That(response.Timestamp, Is.Not.Null);
    }

    [Test]
    public async Task RestGetOrderbookTest()
    {
        var response = await _bybitRestClient.RequestGetOrderbookAsync(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Spot, _symbol);
        
        Assert.That(response, Is.Not.Null);
    }

    [Test]
    public async Task RestGetKlineTest()
    {
        var response = await _bybitRestClient.RequestGetKlineAsync(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Spot, _symbol, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FifteenMinute, DateTime.Now.AddHours(-45), DateTime.Now.AddHours(-24));
        
        Assert.That(response, Is.Not.Null);
    }
}