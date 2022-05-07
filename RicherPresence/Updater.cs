using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

public class Updater : IDisposable
{

    public delegate void Action();

    private string githubRepository;
    private Action restart;
    private Thread thread;
    private bool active;
    private HttpClient http;

    public Updater(string githubRepository, Action restart)
    {
        this.githubRepository = githubRepository;
        this.restart = restart;
        thread = new Thread(() => Run()) { Name = "Updater" };
        active = true;
        http = new HttpClient();

        if (Environment.GetEnvironmentVariable("DISABLE_UPDATE") == null)
        {
            thread.Start();
        }
    }

    private void Run()
    {
        while (active)
        {
            try
            {
                if (Cleanup() | Update())
                {
                    restart.Invoke();
                    active = false;
                }
                else
                {
                    Thread.Sleep(1000 * 60 * 60);
                }
            }
            catch (ThreadInterruptedException)
            {
                active = false;
            }
        }
    }

    private bool Cleanup()
    {
        if (ProvideFiles().Any(file => file.EndsWith(".new")))
        {
            ProvideFiles().Where(file => file.EndsWith(".new")).ToList().ForEach(File.Delete);
            ProvideFiles().Where(file => file.EndsWith(".old")).ToList().ForEach(file =>
            {
                string other = file.Substring(0, file.Length - ".old".Length);
                File.Delete(other);
                Move(file, other);
            });
            return true;
        }
        else
        {
            ProvideFiles().Where(file => file.EndsWith(".old")).ToList().ForEach(File.Delete);
            return false;
        }
    }

    private bool Update()
    {
        string json = doHttpGetString(githubRepository + "/releases/latest");
        string? date = ReadJSONField(json, "published_at");
        if (date == null || !IsNewerVersion(date))
        {
            return false;
        }

        List<string> oldFiles = ProvideFiles().ToList();
        List<string> newFiles = new List<string>();

        string file = ".new.zip";
        File.WriteAllBytes(file, doHttpGetBytes(ReadJSONField(json, "browser_download_url")));
        using (ZipArchive zip = ZipFile.OpenRead(file))
        {
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                string name = entry.FullName; // zip paths are always forward slashes
                if (entry.FullName.Contains("/"))
                    Directory.CreateDirectory(name.Substring(0, entry.FullName.LastIndexOf("/")));
                entry.ExtractToFile(entry.FullName + ".new");
                newFiles.Add(entry.FullName);
            }
        }
        File.Delete(file);

        if (oldFiles.Select(file => Move(file, file + ".old")).All(moved => moved))
        {
            if (!newFiles.AsEnumerable().Select(file =>
            {
                File.Delete(file);
                return Move(file + ".new", file);
            }).All(moved => moved))
            {
                return false;
            }
        }
        else
        {
            if (!newFiles.AsEnumerable().Select(file => Move(file + ".new", file)).All(moved => moved))
            {
                return false;
            }
        }

        return true;
    }

    private string? ReadJSONField(string json, string field)
    {
        String haystack = json;
        String needle = "\"" + field + "\"";
        int from = haystack.IndexOf(needle);
        if (from < 0)
        {
            return null;
        }
        from = from + needle.Length;

        while (char.IsWhiteSpace(haystack[from])) from++;
        if (haystack[from] != ':')
        {
            return null;
        }
        from++;
        while (char.IsWhiteSpace(haystack[from])) from++;

        bool usingQuotes = haystack[from] == '"';
        if (usingQuotes)
        {
            from++;
        }
        int to;
        if (usingQuotes)
        {
            to = haystack.IndexOf("\"", from);
        }
        else
        {
            to = haystack.IndexOf("}", from);
            if (to < 0) to = haystack.IndexOf(",", from);
            if (to < 0) to = haystack.IndexOf(" ", from);
            if (to < 0) return null;
        }

        if (to < 0)
        {
            return null;
        }
        return haystack.Substring(from, to - from);
    }

    private string doHttpGetString(string uri)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Add("Accept", "application/json");
        message.Headers.Add("User-Agent", ".NET");
        HttpResponseMessage response = http.Send(message);
        Task<string> task = response.Content.ReadAsStringAsync();
        task.Wait(1000 * 60);
        return task.Result;
    }

    private byte[] doHttpGetBytes(string uri)
    {
        Task<byte[]> task = http.GetByteArrayAsync(uri);
        task.Wait(1000 * 60);
        return task.Result;
    }

    private IEnumerable<string> ProvideFiles()
    {
        return Directory.EnumerateFiles(".", "", SearchOption.AllDirectories)
            .Where(file => (File.GetAttributes(file) & FileAttributes.Directory) == 0)
            .Where(file => !file.EndsWith(".log"))
            .Where(file => !file.EndsWith(VERSION_FILE.Substring(VERSION_FILE.LastIndexOf('\\'))));
    }

    private bool Move(string old, string _new)
    {
        try
        {
            File.Move(old, _new);
            return true;
        }
        catch (Exception)
        {
            // mimimi
        }
        
        try
        {
            //TODO maybe we wanna stream here to not be forced to have all in memory
            File.WriteAllBytes(_new, File.ReadAllBytes(old));
            return true;
        }
        catch (Exception)
        {
            // mimimi
        }
        
        return false;
    }

    private static string VERSION_FILE = ".\\.version.txt";

    private bool IsNewerVersion(string newVersion)
    {
        if (!IsNewerVersion(ReadCurrentVersion(), newVersion))
        {
            return false;
        }
        SaveCurrentVersion(newVersion);
        return true;
    }

    private static bool IsNewerVersion(string current, string other)
    {
        return !other.Equals(current);
    }

    private static string ReadCurrentVersion()
    {
        try
        {
            return File.ReadAllText(VERSION_FILE);
        }
        catch (FileNotFoundException)
        {
            return "";
        }
    }

    private static void SaveCurrentVersion(string version)
    {
        File.WriteAllText(VERSION_FILE, version);
    }

    public void Dispose()
    {
        active = false;
        thread.Interrupt();
    }
}

