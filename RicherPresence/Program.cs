﻿
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Dynatrace.OpenTelemetry.Exporter.Metrics;

List<KeyValuePair<string, object>> dt_metadata = new List<KeyValuePair<string, object>>();
foreach (string name in new string[] { "dt_metadata_e617c525669e072eebe3d0f08212e8f2.properties", "/var/lib/dynatrace/enrichment/dt_metadata.properties" })
{
    try
    {
        foreach (string line in System.IO.File.ReadAllLines(name.StartsWith("/var") ? name : System.IO.File.ReadAllText(name)))
        {
            var keyvalue = line.Split("=");
            dt_metadata.Add(new KeyValuePair<string, object>(keyvalue[0], keyvalue[1]));
        }
    }
    catch { }
}

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

string token = Environment.GetEnvironmentVariable("DISCORD_RICHER_PRESENCE_DYNATRACE_API_TOKEN") ?? "<no token>";

Sdk.CreateTracerProviderBuilder()
    .SetSampler(new AlwaysOnSampler())
    .AddSource(Observability.ACTIVITY_SOURCE_NAME)
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Discord Richer Presence").AddAttributes(dt_metadata))
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("https://ldj78075.sprint.dynatracelabs.com/api/v2/otlp/v1/traces");
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Headers = "Authorization=Api-Token " + token;
        options.ExportProcessorType = ExportProcessorType.Batch;
    })
    .Build();

Sdk.CreateMeterProviderBuilder()
    .AddMeter(Observability.METER_SOURCE_NAME)
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("RDR2 Discord Rich Presence").AddAttributes(dt_metadata))
    .AddDynatraceExporter(cfg =>
    {
        cfg.Url = "https://ldj78075.sprint.dynatracelabs.com/api/v2/metrics/ingest";
        cfg.ApiToken = token;
        cfg.DefaultDimensions = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("service.name", "Discord Richer Presence") };
    })
    .Build();

bool[] running = new bool[] { true };

using (Updater updater = new Updater("https://github.com/plengauer/RicherPresence", () =>
{
    lock (running)
    {
        running[0] = false;
        Monitor.PulseAll(running);
    }
}))
{
    Screen screen = new DXGIOutputDuplication();
    OCR ocr = new Tesseract();
    using RDR2RichPresenceManager presence = new RDR2RichPresenceManager(screen, ocr, 1000);
    lock (running)
    {
        while (running[0]) Monitor.Wait(running);
    }
}
