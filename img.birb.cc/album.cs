using Newtonsoft.Json;

// TODO:
// add endpoints for creating and deleting albums
// create standard page for viewing (based off dashboard)
// different content for owner vs public (settings available)
// modify dashboard to add "add to album" button below every image
// add album tag to enable automatic uploads
// add batch uploads

public class Album
{
    public string? Hash { get; set; }
    public string? Name { get; set; }
    public List<Img>? Images { get; set; }
    public int UID { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsPublic { get; set; }

    internal Album NewAlbum(int uid)
    {
        return this;
    }
}

public static class AlbumDB
{
    private static List<Album> db = new List<Album>();

    public static List<Album> GetDB() { return db; }

    public static void Load()
    {
        try
        {
            using (StreamReader SR = new StreamReader(Config.AlbumDBPath!))
            {
                string json = SR.ReadToEnd();

                db = JsonConvert.DeserializeObject<List<Album>>(json)!;
                Save();
            }
            Log.Info($"{Config.FileDBPath} loaded - {db.Count} items");
        }
        catch
        {
            Log.Warning($"Unable to load {Config.FileDBPath}!");
        }

        if (!File.Exists(Config.FileDBPath!))
        {
            Log.Info($"Generating {Config.FileDBPath}...");
            Save();
        }
    }

    private static void Save()
    {
        try
        {
            using (StreamWriter SW = new StreamWriter(Config.FileDBPath!, false))
            {
                SW.WriteLine(JsonConvert.SerializeObject(db, Formatting.Indented));
                Log.Info($"{Config.FileDBPath!} saved!");
            }
        }
        catch
        {
            Log.Critical($"Unable to save {Config.FileDBPath!}!");
        }
    }

    public static Album Find(string hash)
    {
        return db.Find(file => file.Hash == hash);
    }

    public static void Add(Album album)
    {
        if (Find(album.Hash!) is null)
        {
            db.Add(album);
            Save();
        }
    }

    public static void Remove(Album album)
    {
        db.Remove(album);
        Save();

        Log.Info("Removed album " + album.Hash);
    }
}
