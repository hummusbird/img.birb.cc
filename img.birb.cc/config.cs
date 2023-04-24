using Newtonsoft.Json;

// TODO:

// remove settings class
// deserialise into dict
// interate through fields and set accordingly

public class Settings
{
    public string? DefaultDomain;
    public string? UserDBPath;
    public string? FileDBPath;
    public string? LogPath;
    public bool LoggingEnabled;
    public List<String>? AllowedFileTypes;
}

public static class Config
{
    private const string path = "config.json";

    public static string? DefaultDomain { get; private set; } = "img.birb.cc";
    public static string? UserDBPath { get; private set; } = "user.json";
    public static string? FileDBPath { get; private set; } = "img.json";
    public static string? LogPath { get; private set; } = "logs";
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

                DefaultDomain = config!.DefaultDomain;
                UserDBPath = config!.UserDBPath;
                FileDBPath = config!.FileDBPath;
                LogPath = config!.LogPath;
                LoggingEnabled = config.LoggingEnabled;
                AllowedFileTypes = config.AllowedFileTypes;
            }
            if (!Config.LoggingEnabled) { Log.Warning("Logging disabled! No logs will be written"); }
            if (AllowedFileTypes!.Count == 0) { Log.Warning("No allowed filetypes specified! All files will be accepted"); }
            Log.Info($"Loaded configuration file");
        }
        catch
        {
            Log.Error($"Unable to load {path}");
        }

        if (!File.Exists(path))
        {
            Save();
            Log.Info("Generated new configuration file");
        }
    }

    private static Settings Serialize()
    {
        Settings? config = new Settings();

        config.DefaultDomain = DefaultDomain;
        config.UserDBPath = UserDBPath;
        config.FileDBPath = FileDBPath;
        config.LogPath = LogPath;
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
                Log.Info($"{path} saved!");
            }
        }
        catch
        {
            Log.Critical($"Unable to save {path}!");
        }
    }
}