using PipelineCuaca.Models;

namespace PipelineCuaca.Services;

/// <summary>Tujuan Load. Ganti implementasinya (SQLite, PostgreSQL, ...) tanpa mengubah tahap lain.</summary>
public interface ICuacaRepository
{
    Task<int> UpsertAsync(IReadOnlyCollection<CuacaHarian> data, CancellationToken ct);
    Task<IReadOnlyList<RingkasanKota>> RingkasanAsync(CancellationToken ct);
}
