using System.Net;
using Microsoft.Extensions.Options;
using RBTB_ServiceStrategy.Domain.Options;

namespace RBTB_ServiceStrategy.Notification.Telergam;

public class TelegramClient
{
    private readonly TelegramOption _options;
    private string Token => _options.Token;
    private string Chat => _options.ChatId;
    private HttpClient HttpClient { get; }

    public TelegramClient(IOptions<TelegramOption> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value ?? throw new ArgumentException(nameof(options));
        HttpClient = httpClientFactory.CreateClient();
        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    public void SendMessage(string mess) =>
        HttpClient
            .GetAsync($"https://api.telegram.org/bot{Token}/sendMessage?chat_id={Chat}&text={mess}")
            .GetAwaiter()
            .GetResult();
}