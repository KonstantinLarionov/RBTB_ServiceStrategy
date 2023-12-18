using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RBTB_ServiceStrategy.Domain.Options;
using RBTB_ServiceStrategy.Markets.Bybit;

namespace RBTB_ServiceStrategy.Controllers;
public class CheckController : ControllerBase
{
    private readonly string _symbol;
    private readonly string _url;
    private readonly BybitRestClient _bybitRestClient;

    public CheckController(IOptions<LevelStrategyOption> bybitOptions)
    {
        _url = bybitOptions.Value.Url;
        _symbol = bybitOptions.Value.Symbol;

        _bybitRestClient = new BybitRestClient(_url);
    }

    [HttpGet("orderbook")]
    public async Task<IActionResult> CheckOrderbook()
    {
        var a = await _bybitRestClient.RequestGetOrderbookAsync(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Spot, _symbol);

        return new JsonResult(a);
    }

    [HttpGet("kline")]
    public async Task<IActionResult> CheckKline()
    {
        var a = await _bybitRestClient.RequestGetKlineAsync(BybitMapper.UTA.RestV5.Data.Enums.MarketCategory.Spot, _symbol, BybitMapper.UTA.RestV5.Data.Enums.IntervalType.TwelveHours, DateTime.Now.AddHours(-45), DateTime.Now.AddHours(-24));
        
        return new JsonResult(a);
    }

    [HttpGet("time")]
    public async Task<IActionResult> CheckServerTime()
    {
        var a = await _bybitRestClient.RequestGetServerTimeAsync();
        
        return new JsonResult(a);
    }
}