using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public class Tesseract : OCR
{
    private const string PATH = "C:\\Program Files\\Tesseract-OCR\\tesseract.exe";

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    public string Parse(string file)
    {
        using var s = ACTIVITIES.StartActivity("tesseract", ActivityKind.Client);
        List<string?> output = new List<string?>();
        List<string?> log = new List<string?>();
        ProcessStartInfo info = new ProcessStartInfo()
        {
            FileName = PATH,
            Arguments = "\"" + file + "\" stdout",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (Process? process = Process.Start(info))
        {
            process.OutputDataReceived += (sender, args) => output.Add(args.Data);
            process.ErrorDataReceived += (sender, args) => log.Add(args.Data);
            process?.BeginOutputReadLine();
            process?.BeginErrorReadLine();
            process?.WaitForExit();
        }
        string str = string.Join('\n', output.ToArray());
        s?.AddTag("tesseract.result", str);
        s?.AddTag("tesseract.log", string.Join('\n', log.ToArray()));
        return str;
    }
}

