# PipelineCuaca (.NET 8)

Contoh data automation dengan .NET: mengambil prakiraan cuaca harian beberapa kota dari
[Open-Meteo](https://open-meteo.com/en/docs), membersihkan dan memvalidasinya, lalu menyimpannya
ke SQLite dengan upsert. Alurnya sama dengan materi Python (Extract, Transform, Load).

## Prasyarat

- .NET SDK 8 atau lebih baru (`dotnet --version`)
- Koneksi internet untuk restore paket NuGet dan memanggil API

## Menjalankan

```bash
dotnet run
```

Hasilnya ada di `data/pipeline.db` (tabel `cuaca_kota`) dan salinan data mentah di `data/raw/`.
Jalankan dua kali: jumlah baris tidak bertambah karena upsert memakai kunci `(kota, tanggal)`.

### Mode uji tanpa internet

```bash
dotnet run -- --Pipeline:DariFile=contoh/cuaca_contoh.json
```

Berguna untuk mencoba alur tanpa API, atau memproses ulang file di `data/raw/`.

## Struktur

| File | Tugas |
| --- | --- |
| `Program.cs` | Host, DI, penanganan error, kode kel uar |
| `PipelineOptions.cs` | Model konfigurasi (`appsettings.json`) |
| `Services/CuacaExtractor.cs` | Extract: HttpClient, timeout, retry |
| `Services/RawStore.cs` | Menyimpan data mentah |
| `Services/CuacaTransformer.cs` | Transform: parse JSON, buang duplikat |
| `Services/CuacaValidator.cs` | Validasi sebelum Load |
| `Services/SqliteCuacaRepository.cs` | Load: upsert SQLite dan query ringkasan |
| `Services/WebhookNotifier.cs` | Notifikasi saat gagal |
| `Services/PipelineRunner.cs` | Merangkai semua tahap |

## Konfigurasi

Semua ada di bagian `Pipeline` pada `appsettings.json`, dan bisa ditimpa lewat variabel environment
dengan pemisah `__`, misalnya `Pipeline__WebhookUrl=https://...` (cocok untuk secrets di GitHub Actions).

## Menjadwalkan

Publikasikan, lalu jadwalkan hasilnya. Atur **working directory** ke folder publish, karena
`appsettings.json` dan folder `data/` dibaca relatif terhadap folder kerja.

```bash
dotnet publish -c Release -o publish
cd publish && ./PipelineCuaca        # Windows: PipelineCuaca.exe
```

Kode keluar `1` saat gagal, sehingga cron, Task Scheduler, dan GitHub Actions menandai tugasnya gagal.

## Pindah ke PostgreSQL

Implementasikan `ICuacaRepository` dengan paket `Npgsql`, lalu ganti satu baris di `Program.cs`:

```csharp
builder.Services.AddSingleton<ICuacaRepository, PostgresCuacaRepository>();
```

Sintaks `INSERT ... ON CONFLICT (kota, tanggal) DO UPDATE` sama di PostgreSQL, dengan parameter `@nama`
sebagai pengganti `$nama`.
