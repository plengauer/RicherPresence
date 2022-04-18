
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

Sdk.CreateTracerProviderBuilder()
    .SetSampler(new AlwaysOnSampler())
    .AddSource(Observability.ACTIVITY_SOURCE_NAME)
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Discord Richer Presence").AddAttributes(dt_metadata))
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("https://ldj78075.sprint.dynatracelabs.com/api/v2/otlp/v1/traces");
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Headers = "Authorization=Api-Token dt0c01.JPEOKTVXI5MHU7SWQ3RU6P4O.IQBVZILPY5A4M2QEJC5AX2JQ4GOWL3PWKBU4N7W4NT6WKR4HKTSD64W3CNLQLXHV";
        options.ExportProcessorType = ExportProcessorType.Batch;
    })
    .Build();

Sdk.CreateMeterProviderBuilder()
    .AddMeter(Observability.METER_SOURCE_NAME)
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("RDR2 Discord Rich Presence").AddAttributes(dt_metadata))
    .AddDynatraceExporter(cfg =>
    {
        cfg.Url = "https://ldj78075.sprint.dynatracelabs.com/api/v2/metrics/ingest";
        cfg.ApiToken = "dt0c01.TFMGCJDV6345JGVKAM5DSUQT.7N4DX4NFSTS2LRGTCWGZRFUEC6ZFPS23QKD4IW7HX4OIMHW64X53K5JYRGMDE437";
        cfg.DefaultDimensions = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("service.name", "Discord Richer Presence") };
    })
    .Build();

Screen screen = new DXGIOutputDuplication();
OCR ocr = new Tesseract();

using (RDR2RichPresenceManager presence = new RDR2RichPresenceManager(screen, ocr, 1000))
{
    while(true)
    {
        Thread.Sleep(1000 * 60);
    }
}
