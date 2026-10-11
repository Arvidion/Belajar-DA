using PipelineCuaca;
using PipelineCuaca.Services;

// Host menyediakan konfigurasi (appsettings.json, variabel environment, argumen CLI),
// dependency injection, dan logging.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PipelineOptions>(builder.Configuration.GetSection("Pipeline"));

// Selalu beri timeout pada HttpClient.
builder.Services.AddHttpClient<CuacaExtractor>(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient<WebhookNotifier>(c => c.Timeout = TimeSpan.FromSeconds(10));

builder.Services.AddSingleton<RawStore>();
builder.Services.AddSingleton<ICuacaRepository, SqliteCuacaRepository>();
builder.Services.AddTransient<PipelineRunner>();

using var host = builder.Build();
var log = host.Services.GetRequiredService<ILogger<Program>>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await host.Services.GetRequiredService<PipelineRunner>().JalankanAsync(cts.Token);
    return 0;
}
catch (Exception e)
{
    log.LogError(e, "Pipeline gagal");
    await host.Services.GetRequiredService<WebhookNotifier>().KirimAsync($"Pipeline cuaca GAGAL: {e.Message}");

    // Kode keluar bukan nol memberi tahu cron, Task Scheduler, atau GitHub Actions bahwa tugas gagal.
    return 1;
}
