using Microsoft.Extensions.Options;
using PipelineCuaca.Models;

namespace PipelineCuaca.Services;

/// <summary>Merangkai tahap Extract, Transform, Validasi, dan Load.</summary>
public sealed class PipelineRunner(
    CuacaExtractor extractor,
    RawStore rawStore,
    ICuacaRepository repository,
    IOptions<PipelineOptions> options,
    ILogger<PipelineRunner> log)
{
    public async Task JalankanAsync(CancellationToken ct)
    {
        var opt = options.Value;
        if (opt.Kota.Count == 0)
            throw new InvalidOperationException(
                "Tidak ada kota di konfigurasi. Jalankan dari folder yang berisi appsettings.json.");

        log.LogInformation("Mulai pipeline untuk {Jumlah} kota", opt.Kota.Count);
        var semua = new List<CuacaHarian>();

        foreach (var kota in opt.Kota)
        {
            // Extract
            var json = await extractor.AmbilJsonAsync(kota, ct);
            if (string.IsNullOrWhiteSpace(opt.DariFile))
            {
                var file = await rawStore.SimpanAsync(kota.Nama, json, ct);
                log.LogInformation("Data mentah {Kota} disimpan di {File}", kota.Nama, file);
            }

            // Transform
            var data = CuacaTransformer.Transform(kota.Nama, json);
            log.LogInformation("{Kota}: {Jumlah} baris setelah dibersihkan", kota.Nama, data.Count);
            semua.AddRange(data);
        }

        // Validasi, lalu Load
        CuacaValidator.Validasi(semua);
        var jumlah = await repository.UpsertAsync(semua, ct);
        log.LogInformation("Selesai: {Jumlah} baris diproses", jumlah);

        // Laporan ringkas dari database
        foreach (var r in await repository.RingkasanAsync(ct))
        {
            log.LogInformation("Ringkasan {Kota}: rata-rata suhu maks {Suhu} C, total hujan {Hujan} mm ({Hari} hari)",
                r.Kota, r.RataSuhuMaks, r.TotalHujanMm, r.Hari);
        }
    }
}
