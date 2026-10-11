using System.Globalization;
using System.Text.Json;
using PipelineCuaca.Models;

namespace PipelineCuaca.Services;

/// <summary>Tahap Transform: JSON mentah menjadi daftar CuacaHarian yang rapi.</summary>
public static class CuacaTransformer
{
    public static List<CuacaHarian> Transform(string kota, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var daily = doc.RootElement.GetProperty("daily");

        var waktu = daily.GetProperty("time");
        var maks = daily.GetProperty("temperature_2m_max");
        var min = daily.GetProperty("temperature_2m_min");
        var hujan = daily.GetProperty("precipitation_sum");

        var hasil = new List<CuacaHarian>();
        for (var i = 0; i < waktu.GetArrayLength(); i++)
        {
            // Tanggal yang tidak bisa dibaca dibuang, sama seperti dropna di pandas.
            if (!DateOnly.TryParseExact(waktu[i].GetString(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var tanggal))
                continue;

            hasil.Add(new CuacaHarian(kota, tanggal, Angka(maks, i), Angka(min, i), Angka(hujan, i)));
        }

        // Buang duplikat (kota + tanggal), sama seperti drop_duplicates.
        return hasil.DistinctBy(x => (x.Kota, x.Tanggal)).ToList();
    }

    // Nilai null di JSON atau indeks yang hilang menjadi null, bukan error.
    private static double? Angka(JsonElement array, int i) =>
        i < array.GetArrayLength() && array[i].ValueKind == JsonValueKind.Number
            ? array[i].GetDouble()
            : null;
}
