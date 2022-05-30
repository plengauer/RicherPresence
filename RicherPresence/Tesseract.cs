using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public class Tesseract : OCR
{
    private static string[] PATHS = { ".\\tesseract.exe", "C:\\Program Files\\Tesseract-OCR\\tesseract.exe" };

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    private static string GetPath()
    {
        for (int i = 0; i < PATHS.Length; i++)
        {
            if (File.Exists(PATHS[i]))
            {
                return PATHS[i];
            }
        }
        throw new Exception("Could not find tesseract");
    }

    public string Parse(string file)
    {
        using var s = ACTIVITIES.StartActivity("tesseract", ActivityKind.Client);
        List<string?> output = new List<string?>();
        List<string?> log = new List<string?>();
        ProcessStartInfo info = new ProcessStartInfo()
        {
            FileName = GetPath(),
            Arguments = "\"" + file + "\" stdout",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        string outputStr, logStr;
        try
        {
            using (Process? process = Process.Start(info))
            {
                process.OutputDataReceived += (sender, args) => output.Add(args.Data);
                process.ErrorDataReceived += (sender, args) => log.Add(args.Data);
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
        }
        finally
        {
            outputStr = string.Join('\n', output.ToArray());
            logStr = string.Join('\n', log.ToArray());
            s?.AddTag("tesseract.result", outputStr);
            s?.AddTag("tesseract.log", logStr);
        }
        if (logStr.ToLower().Contains("error"))
        {
            var tags = new ActivityTagsCollection();
            tags.Add("exception.type", "Error log");
            tags.Add("exception.message", log);
            s?.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
            s?.SetStatus(ActivityStatusCode.Error);
            throw new Exception("Error OCR'ing " + file);
        }
        return outputStr;
    }
}

