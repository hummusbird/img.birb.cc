using Newtonsoft.Json;

public class Config
{
    private const string path = "config.json";

    public string? UserDBPath { get; init; } = "user.json";
    public string? FileDBPath { get; init; } = "img.json";

    private readonly string? salt;
    private List<String>? fileTypes;

    public static Config Load()
    {
        Config loadConfig = new Config();

        try
        {
            using (StreamReader SR = new StreamReader(path))
            {
                string json = SR.ReadToEnd();

                loadConfig = JsonConvert.DeserializeObject<Config>(json)!;
                loadConfig.Save();
            }
            Console.WriteLine($"Loaded configuration file...");
        }
        catch
        {
            Console.WriteLine($"Unable to load {path}");

        }

        if (!File.Exists(path))
        {
            loadConfig!.Save();
        }

        return loadConfig;
    }

    public void Save()
    {

    }

    public void LoadAllowedFileTypes()
    {

    }
}