using Microsoft.AspNetCore.Mvc;
using RBTB_ServiceStrategy.Markets.Bybit;

namespace RBTB_ServiceStrategy.Controllers;
public class CheckController : ControllerBase
{
    private string _symbol = "ADAUSDT";
    private const string _url = "https://api.bybit.com";
    private BybitRestClient _bybitRestClient;
    private string _api = "6CjIuMxhY8DAhWu03C";
    private string _secret = "DoGlywXMEp8pvt9s5L2vYwIuoXBUnnSulHby";

    [HttpGet("orderbook")]
    public async Task<IActionResult> CheckOrderbook()
    {
        _bybitRestClient = new BybitRestClient(_url, _api, _secret);
        var a = await _bybitRestClient.RequestGetOrderbookAsync(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Spot, _symbol);
        return new JsonResult(a);
    }

    [HttpGet("kline")]
    public async Task<IActionResult> CheckKline()
    {
        _bybitRestClient = new BybitRestClient(_url, _api, _secret);
        var a = await _bybitRestClient.RequestGetKlineAsync(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Spot, _symbol, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.FifteenMinute, DateTime.Now.AddHours(-45), DateTime.Now.AddHours(-24));
        return new JsonResult(a);
    }

    [HttpGet("time")]
    public async Task<IActionResult> CheckServerTime()
    {
        _bybitRestClient = new BybitRestClient(_url, _api, _secret);
        var a = await _bybitRestClient.RequestGetServerTimeAsync();
        return new JsonResult(a);
    }
}
