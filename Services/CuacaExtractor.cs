using System.Globalization;
using Microsoft.Extensions.Options;

namespace PipelineCuaca.Services;

/// <summary>Tahap Extract: mengambil JSON cuaca dari API (dengan timeout dan retry).</summary>
public sealed class CuacaExtractor(
    HttpClient http,
    IOptions<PipelineOptions> options,
    ILogger<CuacaExtractor> log)
{
    public async Task<string> AmbilJsonAsync(KotaOption kota, CancellationToken ct)
    {
        var opt = options.Value;

        // Mode uji: baca dari file lokal, tanpa jaringan.
        if (!string.IsNullOrWhiteSpace(opt.DariFile))
        {
            log.LogInformation("Mode uji: membaca {File}", opt.DariFile);
            return await File.ReadAllTextAsync(opt.DariFile, ct);
        }

        var lat = kota.Lat.ToString(CultureInfo.InvariantCulture);
        var lon = kota.Lon.ToString(CultureInfo.InvariantCulture);
        var url = $"{opt.ApiUrl}?latitude={lat}&longitude={lon}" +
                  "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum" +
                  "&timezone=Asia%2FJakarta";

        for (var percobaan = 1; ; percobaan++)
        {
            try
            {
                using var resp = await http.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync(ct);
            }
            // Timeout HttpClient muncul sebagai TaskCanceledException, bukan HttpRequestException.
            catch (Exception e) when (percobaan < opt.MaxRetries && !ct.IsCancellationRequested
                                      && e is HttpRequestException or TaskCanceledException)
            {
                log.LogWarning("Percobaan {Ke} untuk {Kota} gagal: {Pesan}", percobaan, kota.Nama, e.Message);
                await Task.Delay(TimeSpan.FromSeconds(2 * percobaan), ct);
            }
        }
    }
}
