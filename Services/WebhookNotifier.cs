using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace PipelineCuaca.Services;

/// <summary>Mengirim pesan ke webhook Discord (kunci "content"). Untuk Slack, ganti dengan kunci "text".</summary>
public sealed class WebhookNotifier(
    HttpClient http,
    IOptions<PipelineOptions> options,
    ILogger<WebhookNotifier> log)
{
    public async Task KirimAsync(string pesan)
    {
        var url = options.Value.WebhookUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            using var resp = await http.PostAsJsonAsync(url, new { content = pesan });
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            // Kegagalan notifikasi tidak boleh menutupi error asli pipeline.
            log.LogWarning("Notifikasi gagal terkirim: {Pesan}", e.Message);
        }
    }
}
