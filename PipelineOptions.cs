namespace PipelineCuaca;

/// <summary>Pengaturan pipeline, dibaca dari bagian "Pipeline" di appsettings.json.</summary>
public sealed class PipelineOptions
{
    public string ApiUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";
    public string DbPath { get; set; } = "data/pipeline.db";
    public string RawDir { get; set; } = "data/raw";
    public int MaxRetries { get; set; } = 3;

    /// <summary>Alamat webhook (Discord/Slack) untuk notifikasi gagal. Kosong = nonaktif.</summary>
    public string WebhookUrl { get; set; } = "";

    /// <summary>Mode uji: kalau diisi, data dibaca dari file JSON ini, bukan dari API.</summary>
    public string DariFile { get; set; } = "";

    public List<KotaOption> Kota { get; set; } = new();
}

public sealed class KotaOption
{
    public string Nama { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
}
