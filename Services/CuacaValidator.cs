using PipelineCuaca.Models;

namespace PipelineCuaca.Services;

/// <summary>Validasi data sebelum dimuat. Lebih baik berhenti daripada menyimpan data rusak.</summary>
public static class CuacaValidator
{
    public static void Validasi(IReadOnlyCollection<CuacaHarian> data)
    {
        var masalah = new List<string>();

        if (data.Count == 0)
            masalah.Add("data kosong");
        if (data.Any(x => x.SuhuMaks is null || x.SuhuMin is null || x.HujanMm is null))
            masalah.Add("ada nilai kosong");
        if (data.Any(x => x.SuhuMaks is < -10 or > 50))
            masalah.Add("suhu maksimum di luar rentang wajar (-10 sampai 50)");
        if (data.Any(x => x.SuhuMin > x.SuhuMaks))
            masalah.Add("suhu minimum lebih besar dari suhu maksimum");
        if (data.Any(x => x.HujanMm < 0))
            masalah.Add("curah hujan negatif");

        if (masalah.Count > 0)
            throw new InvalidDataException("Validasi gagal: " + string.Join("; ", masalah));
    }
}
