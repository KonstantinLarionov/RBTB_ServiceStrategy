using BybitMapper.UTA.RestV5.Requests.Market;
using BybitMapper.Requests;
using BybitMapper.UTA.RestV5.Data.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerTimeResponse = BybitMapper.UTA.RestV5.Responses.Market.ServerTimeResponse;
using BybitMapper.UTA.RestV5.Responses.Market;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Kline;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Orderbook;
using CSCommon.Http;
using RBTB_ServiceStrategy.Markets.Bybit.BybitExtensions;

namespace RBTB_ServiceStrategy.Markets.Bybit;
public class BybitRestClient
{
    private RequestArranger _arranger;
    private CommonHttpClient _client;

    private static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        IncludeFields = true
    };

    public BybitRestClient(string url, string api, string secret)
    {
        _client = new(url);
        _arranger = new RequestArranger(api, secret);
    }

    #region [Base]

    internal delegate void LogDlg(string sender, string message);
    internal LogDlg? Log;
    internal bool LogResponseEnabled = false;
    internal bool LogExEnabled = false;

    void OnLogResponse(string response)
    {
        if (LogResponseEnabled)
        {
            Log?.Invoke("RestClient", string.Concat("Response: ", response));
        }
    }

    void OnLogEx(Exception ex, string? response = null)
    {
        if (LogExEnabled)
        {
            Log?.Invoke("RestClient", string.Concat("Exception: ", ex.Message, "; ", ex?.InnerException, " - ", response));
        }
    }

    #endregion

    #region [Public]

    public async Task<OrderbookResult?> RequestGetOrderbookAsync(MarketCategory category, string symbol)
    {
        var request = new GetOrderbookRequest(category, symbol);
        var response = await BybitHelpers.GetContentAsync<GetOrderbookResponse>(request, _arranger, _client);

        return response != null ? response.Result : null;
    }

    public async Task<KlineResult?> RequestGetKlineAsync(MarketCategory category, string symbol, IntervalType intervalType,
        DateTime? start = null, DateTime? end = null, int limit = 100)
    {
        var request = new GetKlineRequest(category, symbol, intervalType)
        {
            StartTime = start,
            EndTime = end
        };
        var response = await BybitHelpers.GetContentAsync<GetKlineResponse>(request, _arranger, _client);

        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(KlineResult)));
            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));
        }

        return response != null ? response.Result : null;
    }

    public async Task<ServerTimeData?> RequestGetServerTimeAsync()
    {
        var requesrt = new ServerTimeRequest();
        var response = await BybitHelpers.GetContentAsync<ServerTimeResponse>(requesrt, _arranger, _client);

        return response != null ? response.Result : null;
    }

    #endregion
}
