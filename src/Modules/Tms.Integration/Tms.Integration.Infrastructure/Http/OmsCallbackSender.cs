using Microsoft.Extensions.Logging;
using Tms.Integration.Application.Features.OmsIntegration.PushStatus;

namespace Tms.Integration.Infrastructure.Http;

/// <summary>
/// HTTP implementation ของ IOmsCallbackSender.
/// สร้าง HttpClient จาก Factory โดยใช้ชื่อ Provider Code เป็น Named Client.
/// </summary>
public sealed class OmsCallbackSender(
    IHttpClientFactory httpClientFactory,
    ILogger<OmsCallbackSender> logger) : IOmsCallbackSender
{
    public async Task<bool> SendAsync(
        string omsProviderCode, string payload, CancellationToken ct = default)
    {
        try
        {
            var clientName = $"OMS_{omsProviderCode}";
            var httpClient = httpClientFactory.CreateClient(clientName);

            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/oms/callback/status", content, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("OMS callback sent to provider {Provider}.", omsProviderCode);
                return true;
            }

            logger.LogWarning(
                "OMS callback to {Provider} failed: HTTP {Status}",
                omsProviderCode, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OMS callback to {Provider} threw exception.", omsProviderCode);
            return false;
        }
    }
}
