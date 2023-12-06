using BybitMapper.UTA.RestV5.Requests.Market;
using BybitMapper.Requests;
using RestSharp;
using System.Drawing;
using System.Net;
using BybitMapper.UTA.RestV5.Data.Enums;
using BybitMapper.UTA.RestV5.Requests.Position;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerTimeResponse = BybitMapper.UTA.RestV5.Responses.Market.ServerTimeResponse;
using BybitMapper.UTA.RestV5.Responses.Position;
using BybitMapper.UTA.RestV5.Requests.Market.Option;
using BybitMapper.UTA.RestV5.Responses.Market.Option;
using BybitMapper.UTA.RestV5.Responses.Market;

namespace RBTB_ServiceStrategy.Markets.Bybit
{
    public class BybitRestClient
    {

        private RequestArranger _arranger;
        private CSCommon.Http.CommonHttpClient _client;
        private string _api = "6CjIuMxhY8DAhWu03C";
        private string _secret = "DoGlywXMEp8pvt9s5L2vYwIuoXBUnnSulHby";

        private static JsonSerializerOptions jsonSerializerOptions = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        public BybitRestClient(RequestArranger ra)
        {
            _arranger = ra;
        }
        public BybitRestClient(RequestArranger ra, string api, string secret)
        {
            _arranger = ra;
            _api = api;
            _secret = secret;
        }

        public void Setup()
        {
            _arranger = new(_api, _secret);
        }

        #region [Helpers]
        private T GetContent<T>(RequestPayload request)
        {
            var arrange = _arranger.Arrange(request);
            var content = _client.GetContent<T>(GetHttpMethod(arrange.Method), arrange.Query, null, arrange.Headers);
            return content;
        }

        private static async Task<T?> DeserializeAsync<T>(ArraySegment<byte> message, int lenght)
        {
            using (MemoryStream stream = new MemoryStream(message.Array, 0, lenght))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions);
            }
        }
        private static T? Deserialize<T>(byte[] message)
        {
            using (MemoryStream stream = new MemoryStream(message, 0, message.Length))
            {
                return JsonSerializer.Deserialize<T>(stream, jsonSerializerOptions);
            }
        }
        private HttpMethod GetHttpMethod(RequestMethod method)
        {
            switch (method)
            {
                case RequestMethod.GET: return HttpMethod.Get;
                case RequestMethod.POST: return HttpMethod.Post;
                case RequestMethod.PUT: return HttpMethod.Put;
                case RequestMethod.DELETE: return HttpMethod.Delete;
                default: throw new NotImplementedException();
            }
        }

        private long GetTime()
        {
            var request = new ServerTimeRequest();
            var response = GetContent<ServerTimeResponse>(request);
            return response.Time.Value;
        }
        #endregion


        #region [Base]

        RestClient m_RestClient;

        internal void SetUrl(string restUrl)
        {
            m_RestClient = new RestClient(restUrl);
        }

        internal void SetProxy(string ip, string port, string username, string password)
        {
            try
            {
                var proxy = new WebProxy($"{ip}:{port}");
                if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
                {
                    proxy.Credentials = new NetworkCredential(username, password);
                }

                m_RestClient.Proxy = proxy;

            }
            catch (Exception ex)
            {
                OnLogEx(ex);
            }
        }

        internal void ResetProxy()
        {
            m_RestClient.Proxy = null;
        }

        internal delegate void LogDlg(string sender, string message);
        internal LogDlg Log;
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

        string SendRestRequest(IRequestContent message)
        {
            Method method;

            switch (message.Method)
            {
                case RequestMethod.GET:
                    method = Method.GET;
                    break;
                case RequestMethod.POST:
                    method = Method.POST;
                    break;
                case RequestMethod.PUT:
                    method = Method.PUT;
                    break;
                case RequestMethod.DELETE:
                    method = Method.DELETE;
                    break;
                default:
                    throw new NotImplementedException("Unknown request method");
            }

            var request = new RestRequest(message.Query, method);

            if (message.Headers != null)
            {
                foreach (var header in message.Headers)
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }

            var r = m_RestClient.Execute(request);

            return r.Content;
        }

        #endregion

        #region [Public]

        internal bool RequestPositionInfo(MarketCategory marketCategory, string symbol, out GetPositionInfoResponse response)
        {
            response = null;
            var request_data = new GetPositionInfoRequest(marketCategory, symbol);

            try
            {
                response = GetContent<GetPositionInfoResponse>(request_data);
                return true;
            }
            catch (Exception ex)
            {
                OnLogEx(ex, "");
            }

            return false;
        }

        internal bool RequestTickerInfo(string symbol, out GetOptionsTickersResponse response)
        {
            response = null;
            var request = new GetOptionsTickersRequest() { BaseCoin = symbol };

            try
            {
                response = GetContent<GetOptionsTickersResponse>(request);
                return true;
            }
            catch (Exception ex)
            {
                OnLogEx(ex, "");
            }

            return false;
        }

        internal bool RequestOrderbookInfo(MarketCategory category, string symbol, out GetOrderbookResponse response)
        {
            response = null;
            var request = new GetOrderbookRequest(category, symbol);

            try
            {
                response = GetContent<GetOrderbookResponse>(request);
                return true;
            }
            catch (Exception ex)
            {
                OnLogEx(ex, "");
            }

            return false;
        }

        internal bool RequestGetKlineInfo(MarketCategory category, string symbol, IntervalType intervalType, out GetKlineResponse response,
            DateTime? start = null, DateTime? end = null, int limit = 100)
        {
            response = null;
            var request = new GetKlineRequest(category, symbol, intervalType)
            {
                StartTime = start,
                EndTime = end,
                Limit = Math.Min(limit, 1000)
            };
            try
            {
                response = GetContent<GetKlineResponse>(request);
                return true;
            }
            catch (Exception ex)
            {
                OnLogEx(ex, "");
            }


            return false;
        }

        internal bool RequestGetServerTime(out ServerTimeResponse response)
        {
            response = null;
            var request = new ServerTimeRequest();

            try
            {
                response = GetContent<ServerTimeResponse>(request);
                return true;
            }
            catch (Exception ex)
            {
                OnLogEx(ex, "");
            }

            return false;
        }

        #endregion
    }
}
