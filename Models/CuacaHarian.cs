namespace PipelineCuaca.Models;

/// <summary>Satu baris data cuaca harian untuk satu kota.</summary>
public sealed record CuacaHarian(
    string Kota,
    DateOnly Tanggal,
    double? SuhuMaks,
    double? SuhuMin,
    double? HujanMm)
{
    /// <summary>Kolom turunan: rata-rata suhu maksimum dan minimum.</summary>
    public double? SuhuRata =>
        SuhuMaks is { } maks && SuhuMin is { } min ? Math.Round((maks + min) / 2, 1) : null;

    /// <summary>Kolom turunan dengan aturan contoh (bukan standar resmi).</summary>
    public string KategoriHujan => HujanMm switch
    {
        null => "tidak diketahui",
        < 1 => "kering",
        < 20 => "ringan",
        < 50 => "sedang",
        _ => "lebat",
    };
}

public sealed record RingkasanKota(string Kota, double RataSuhuMaks, double TotalHujanMm, int Hari);
