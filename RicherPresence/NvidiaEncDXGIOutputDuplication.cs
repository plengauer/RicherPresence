#define V2

using System.Diagnostics;

public class NvidiaEncDXGIOutputDuplication : Screen
{
    private static string TEMPORARY_DIR = Environment.GetEnvironmentVariable("TEMP_DIR") ?? ".";

    private static string CAPTURE_VIDEO_EXE_PATH = Environment.GetEnvironmentVariable("CAPTURE_VIDEO_EXE") ?? ".\\nvEncDXGIOutputDuplicationSample.exe";
    private const string OUTPUT_VIDEO_FILE = "DDATest_0.h264";
    private static string FFMPEG_EXE_PATH = Environment.GetEnvironmentVariable("FFMPEG_EXE") ?? ".\\ffmpeg.exe";

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    private int index = 0;

    public NvidiaEncDXGIOutputDuplication()
    { }

    public bool IsDone()
    {
        return false;
    }

    public string Capture()
    {
#if V2
        string result = TEMPORARY_DIR + "\\screenshot_" + (index++) + ".bmp";
        File.Delete(result);
        ProcessStartInfo infoCaptureVideo = new ProcessStartInfo()
        {
            FileName = CAPTURE_VIDEO_EXE_PATH,
            Arguments = "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var s = ACTIVITIES.StartActivity("DXGI_output_duplication", ActivityKind.Client))
        {
            using (Process? process = Process.Start(infoCaptureVideo))
            {
                process.PriorityClass = ProcessPriorityClass.RealTime;
                process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);
                process?.BeginOutputReadLine();
                process?.BeginErrorReadLine();
                process?.WaitForExit();
            }
        }
        File.Move("DDATest_0.bmp", result);
        return result;
#else
        string result = TEMPORARY_DIR + "\\screenshot_" + (index++) + ".bmp";
        File.Delete(OUTPUT_VIDEO_FILE);
        File.Delete(result);
        ProcessStartInfo infoCaptureVideo = new ProcessStartInfo()
        {
            FileName = CAPTURE_VIDEO_EXE_PATH,
            Arguments = "-frames 0",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        ProcessStartInfo infoVideo2Screenshot = new ProcessStartInfo()
        {
            FileName = FFMPEG_EXE_PATH,
            Arguments = "-i \"" + OUTPUT_VIDEO_FILE + "\" -r 1 \"" + result + "\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var s = ACTIVITIES.StartActivity("DXGI_output_duplication.stream", ActivityKind.Client))
        {
            using (Process? process = Process.Start(infoCaptureVideo))
            {
                // process.PriorityClass = ProcessPriorityClass.RealTime;
                // process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                // process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);
                process?.BeginOutputReadLine();
                process?.BeginErrorReadLine();
                process?.WaitForExit();
            }
        }
        using (var s = ACTIVITIES.StartActivity("DXGI_output_duplication.stream2picture", ActivityKind.Client))
        {
            using (Process? process = Process.Start(infoVideo2Screenshot))
            {
                // process.PriorityClass = ProcessPriorityClass.RealTime;
                process?.BeginOutputReadLine();
                process?.BeginErrorReadLine();
                process?.WaitForExit();
            }
        }
        File.Delete(OUTPUT_VIDEO_FILE);
        return result;
#endif
    }
}

