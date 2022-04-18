using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class MySpanSampler : Sampler
{

    private readonly double ratio;

    public MySpanSampler(double ratio)
    {
        this.ratio = Math.Min(1, Math.Max(0, ratio));
    }

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        return new SamplingResult(ratio > 0 && parameters.TraceId.GetHashCode() % ((int) (1 / ratio)) == 0 ? SamplingDecision.RecordAndSample : SamplingDecision.Drop);
    }
}

