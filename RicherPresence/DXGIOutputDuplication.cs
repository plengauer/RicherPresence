using System.Diagnostics;

public class DXGIOutputDuplication : Screen
{
    private static string TEMPORARY_DIR = Environment.GetEnvironmentVariable("TEMP_DIR") ?? ".";

    private static string CAPTURE_VIDEO_EXE_PATH = Environment.GetEnvironmentVariable("CAPTURE_VIDEO_EXE") ?? ".\\DXGIOutputDuplication.exe";

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    private int index = 0;

    public DXGIOutputDuplication()
    { }

    public bool IsDone()
    {
        return false;
    }

    public string Capture()
    {
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
                // process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                // process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);
                process?.BeginOutputReadLine();
                process?.BeginErrorReadLine();
                process?.WaitForExit();
            }
        }
        File.Move("DDATest_0.bmp", result);
        return result;
    }
}

