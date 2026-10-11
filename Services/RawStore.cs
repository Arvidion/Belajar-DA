using Microsoft.Extensions.Options;

namespace PipelineCuaca.Services;

/// <summary>Menyimpan salinan data mentah sebelum diolah, agar bisa diproses ulang.</summary>
public sealed class RawStore(IOptions<PipelineOptions> options)
{
    public async Task<string> SimpanAsync(string kota, string json, CancellationToken ct)
    {
        var dir = options.Value.RawDir;
        Directory.CreateDirectory(dir);

        var namaAman = kota.ToLowerInvariant().Replace(' ', '-');
        var path = Path.Combine(dir, $"cuaca_{namaAman}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(path, json, ct);
        return path;
    }
}
