using Newtonsoft.Json;

public class Settings
{
    public string? DefaultURL;
    public string? UserDBPath;
    public string? FileDBPath;
    public bool LoggingEnabled;
    public List<String>? AllowedFileTypes;
}


public static class Config
{
    private const string path = "config.json";

    public static string? DefaultURL { get; private set; } = "img.birb.cc";
    public static string? UserDBPath { get; private set; } = "user.json";
    public static string? FileDBPath { get; private set; } = "img.json";
    public static bool LoggingEnabled { get; private set; } = false;

    public static List<String>? AllowedFileTypes { get; private set; } = new List<String>() {
        "89504E470D0A1A0A",     // png
        "FFD8FF",               // jpg
        "474946383761",         // gif
        "474946383961",         // gif
        "6674797069736F6D",     // mp4
        "FFFB",                 // mp3
        "FFF3",                 // mp3
        "FFF2",                 // mp3
        "494433",               // mp3
        "1A45DFA3"              // webm & mkv+
    };

    public static void Load()
    {
        try
        {
            using (StreamReader SR = new StreamReader(path))
            {
                string json = SR.ReadToEnd();

                Settings? config = JsonConvert.DeserializeObject<Settings>(json);

                DefaultURL = config!.DefaultURL;
                UserDBPath = config!.UserDBPath;
                FileDBPath = config!.FileDBPath;
                LoggingEnabled = config.LoggingEnabled;
                AllowedFileTypes = config.AllowedFileTypes;
            }
            Console.WriteLine($"Loaded configuration file");
        }
        catch
        {
            Console.WriteLine($"Unable to load {path}");

        }

        if (!File.Exists(path))
        {
            Save();
            Console.WriteLine("Generated new configuration file");
        }
    }

    private static Settings Serialize()
    {
        Settings? config = new Settings();

        config.DefaultURL = DefaultURL;
        config.UserDBPath = UserDBPath;
        config.FileDBPath = FileDBPath;
        config.LoggingEnabled = LoggingEnabled;
        config.AllowedFileTypes = AllowedFileTypes;

        return config;
    }

    public static void Save()
    {
        try
        {
            using (StreamWriter SW = new StreamWriter(path, false))
            {
                SW.WriteLine(JsonConvert.SerializeObject(Serialize(), Formatting.Indented));
                Console.WriteLine($"{path} saved!");
            }
        }
        catch
        {
            Console.WriteLine($"Error saving {path}!");
        }
    }

    public static bool HasAllowedMagicBytes(Stream stream)
    {
        if (stream.Length < 16) { return false; }

        string magicbytes = "";
        stream.Position = 0;

        for (int i = 0; i < 8; i++) // load first 8 bytes from file
        {
            magicbytes += (stream.ReadByte().ToString("X2")); // convert from int to hex
        }

        foreach (string allowedMagicBytes in AllowedFileTypes!)
        {
            if (magicbytes.StartsWith(allowedMagicBytes)) // compare to list of allowed filetypes
            {
                return true;
            }
        }

        return false;
    }
}