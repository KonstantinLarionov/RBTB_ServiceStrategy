using BybitMapper.UTA.RestV5.Requests.Market;
using BybitMapper.Requests;
using RestSharp;
using BybitMapper.UTA.RestV5.Data.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerTimeResponse = BybitMapper.UTA.RestV5.Responses.Market.ServerTimeResponse;
using BybitMapper.UTA.RestV5.Responses.Market;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Kline;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Orderbook;
using System.Text;

namespace RBTB_ServiceStrategy.Markets.Bybit
{
    public class BybitRestClient
    {

        private RequestArranger _arranger;
        private RestClient _client;


        private static JsonSerializerOptions jsonSerializerOptions = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            IncludeFields = true 
        };

        public BybitRestClient(string url, string api, string secret)
        {
            _client = new RestClient(url);
            _arranger = new RequestArranger(api, secret);
        }

        #region [Helpers]
        private T GetContent<T>(RequestPayload request)
        {
            var arrange = _arranger.Arrange(request);
            var restRequest = new RestRequest(arrange.Query, GetHttpMethod(arrange.Method));
            var response = _client.Get<T>(restRequest);

            return response.Data;
        }
        private static T? Deserialize<T>(string message)
        {

            return JsonSerializer.Deserialize<T>(message, jsonSerializerOptions);

        }
        private Method GetHttpMethod(RequestMethod method)
        {
            switch (method)
            {
                case RequestMethod.GET: return Method.GET;
                case RequestMethod.POST: return Method.POST;
                case RequestMethod.PUT: return Method.PUT;
                case RequestMethod.DELETE: return Method.DELETE;
                default: throw new NotImplementedException();
            }
        }

        #endregion


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

        public OrderbookResult RequestGetOrderbook(MarketCategory category, string symbol)
        {
            var request = new GetOrderbookRequest(category, symbol);
            var response = GetContent<GetOrderbookResponse>(request);

            return response.Result;
        }

        public KlineResult? RequestGetKline(MarketCategory category, string symbol, IntervalType intervalType,
            DateTime? start = null, DateTime? end = null, int limit = 100)
        {
            var request = new GetKlineRequest(category, symbol, intervalType)
            {
                StartTime = start,
                EndTime = end,
                Limit = Math.Min(limit, 1000)
            };

            var response = GetContent<GetKlineResponse>(request);

            if (response == null)
            {
                OnLogEx(new NullReferenceException(message: nameof(GetKlineResponse)));
                return null;
            }
            if (response.RetCode != 0)
            {
                OnLogEx(new Exception(message: response.RetMsg));
            }

            return response.Result;
        }

        public ServerTimeData RequestGetServerTime()
        {
            var requesrt = new ServerTimeRequest();
            var response = GetContent<ServerTimeResponse>(requesrt);
            return response.Result;
        }

        #endregion
    }
}
