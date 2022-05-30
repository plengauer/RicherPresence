using System.Diagnostics;

public class DXGIOutputDuplication : Screen
{

    private static string TEMPORARY_DIR = Environment.GetEnvironmentVariable("TEMP_DIR") ?? ".";
    private static string CAPTURE_VIDEO_EXE_PATH = Environment.GetEnvironmentVariable("CAPTURE_VIDEO_EXE") ?? ".\\DXGIOutputDuplication.exe";// DXGIOutputDuplication

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    private int index = 0;

    public DXGIOutputDuplication()
    { }

    public bool IsDone()
    {
        return false;
    }

    public string? Capture(long id)
    {
        string filename = TEMPORARY_DIR + "\\screenshot_" + id + ".bmp";
        File.Delete(filename);
        ProcessStartInfo infoCaptureVideo = new ProcessStartInfo()
        {
            FileName = CAPTURE_VIDEO_EXE_PATH,
            Arguments = "\"" + filename + "\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        List<string?> @out = new List<string?>();
        List<string?> err = new List<string?>();
        using (var s = ACTIVITIES.StartActivity("DXGI_output_duplication", ActivityKind.Client))
        {
            try
            {
                using (Process? process = Process.Start(infoCaptureVideo))
                {
                    process.PriorityClass = ProcessPriorityClass.RealTime;
                    process.OutputDataReceived += (sender, args) => @out.Add(args.Data);
                    process.ErrorDataReceived += (sender, args) => err.Add(args.Data);
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    int code = process.ExitCode;
                    s?.AddTag("process.exit.code", "" + code);
                    if (code != 0)
                    {
                        var tags = new ActivityTagsCollection();
                        tags.Add("exception.type", "Non-zero exit code");
                        tags.Add("exception.message", "" + code);
                        s?.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
                        s?.SetStatus(ActivityStatusCode.Error);
                        throw new Exception("Non-zero exit code: " + code);
                    }
                }
                if (!File.Exists(filename))
                {
                    var tags = new ActivityTagsCollection();
                    tags.Add("exception.type", "File error");
                    tags.Add("exception.message", "File " + filename + " does not exist");
                    s?.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
                    s?.SetStatus(ActivityStatusCode.Error);
                    throw new Exception("File does not exist: " + filename);
                }
            }
            finally
            {
                s?.AddTag("output_duplication.out", string.Join('\n', @out));
                s?.AddTag("output_duplication.err", string.Join('\n', err));
            }
        }
        return filename;
    }
}

