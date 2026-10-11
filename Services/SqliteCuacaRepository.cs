using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using PipelineCuaca.Models;

namespace PipelineCuaca.Services;

/// <summary>Tahap Load ke SQLite dengan upsert, sehingga pipeline aman dijalankan berulang (idempotent).</summary>
public sealed class SqliteCuacaRepository(IOptions<PipelineOptions> options) : ICuacaRepository
{
    private const string Ddl = """
        CREATE TABLE IF NOT EXISTS cuaca_kota (
            kota           TEXT NOT NULL,
            tanggal        TEXT NOT NULL,
            suhu_maks      REAL,
            suhu_min       REAL,
            suhu_rata      REAL,
            hujan_mm       REAL,
            kategori_hujan TEXT,
            diperbarui     TEXT NOT NULL,
            PRIMARY KEY (kota, tanggal)
        )
        """;

    private const string Upsert = """
        INSERT INTO cuaca_kota
            (kota, tanggal, suhu_maks, suhu_min, suhu_rata, hujan_mm, kategori_hujan, diperbarui)
        VALUES
            ($kota, $tanggal, $maks, $min, $rata, $hujan, $kategori, $waktu)
        ON CONFLICT(kota, tanggal) DO UPDATE SET
            suhu_maks      = excluded.suhu_maks,
            suhu_min       = excluded.suhu_min,
            suhu_rata      = excluded.suhu_rata,
            hujan_mm       = excluded.hujan_mm,
            kategori_hujan = excluded.kategori_hujan,
            diperbarui     = excluded.diperbarui
        """;

    private const string QueryRingkasan = """
        SELECT kota,
               ROUND(AVG(suhu_maks), 1),
               ROUND(SUM(hujan_mm), 1),
               COUNT(*)
        FROM cuaca_kota
        GROUP BY kota
        ORDER BY AVG(suhu_maks) DESC
        """;

    private string ConnectionString => $"Data Source={options.Value.DbPath}";

    public async Task<int> UpsertAsync(IReadOnlyCollection<CuacaHarian> data, CancellationToken ct)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(options.Value.DbPath));
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);

        // Semua baris dalam satu transaksi: berhasil semua, atau tidak sama sekali.
        await using var tx = conn.BeginTransaction();

        await using (var buat = conn.CreateCommand())
        {
            buat.Transaction = tx;
            buat.CommandText = Ddl;
            await buat.ExecuteNonQueryAsync(ct);
        }

        var waktu = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        foreach (var x in data)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = Upsert;
            cmd.Parameters.AddWithValue("$kota", x.Kota);
            cmd.Parameters.AddWithValue("$tanggal", x.Tanggal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$maks", Nilai(x.SuhuMaks));
            cmd.Parameters.AddWithValue("$min", Nilai(x.SuhuMin));
            cmd.Parameters.AddWithValue("$rata", Nilai(x.SuhuRata));
            cmd.Parameters.AddWithValue("$hujan", Nilai(x.HujanMm));
            cmd.Parameters.AddWithValue("$kategori", x.KategoriHujan);
            cmd.Parameters.AddWithValue("$waktu", waktu);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return data.Count;
    }

    public async Task<IReadOnlyList<RingkasanKota>> RingkasanAsync(CancellationToken ct)
    {
        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = QueryRingkasan;

        var hasil = new List<RingkasanKota>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            hasil.Add(new RingkasanKota(
                reader.GetString(0),
                reader.GetDouble(1),
                reader.GetDouble(2),
                reader.GetInt32(3)));
        }
        return hasil;
    }

    // SQLite membutuhkan DBNull.Value untuk menyimpan NULL.
    private static object Nilai(double? d) => d.HasValue ? d.Value : DBNull.Value;
}
