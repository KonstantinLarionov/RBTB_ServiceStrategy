using BybitMapper.Requests;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSCommon.Http;

namespace RBTB_ServiceStrategy.Markets.Bybit.BybitExtensions;
public static class BybitHelpers
{
    public static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static async Task<T?> GetContentAsync<T>(RequestPayload request, RequestArranger arranger, CommonHttpClient client)
    {
        var arrange = arranger.Arrange(request);
        T? content;
        try
        {
            content = await client.GetContentAsync<T>(GetHttpMethod(arrange.Method), arrange.Query, null!, null!);
        }
        catch (Exception)
        {

            return default(T);
        }

        return content;
    }

    private static HttpMethod GetHttpMethod(RequestMethod method)
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
}
