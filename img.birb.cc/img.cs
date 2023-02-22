using Newtonsoft.Json;

public class Img
{
    public string? Hash { get; set; }
    public string? Filename { get; set; }
    public int UID { get; set; }
    public DateTime Timestamp { get; set; }

    internal Img NewImg(int uid, string extension, IFormFile img)
    {
        string newHash = Hashing.NewHash(8);
        while (FileDB.Find(newHash) is not null)
        {
            newHash = Hashing.NewHash(8);
        }

        Hash = newHash;
        Filename = Hash + extension;
        UID = uid;
        Timestamp = DateTime.Now;

        UserDB.GetUserFromUID(uid).UploadCount++;
        UserDB.GetUserFromUID(uid).UploadedBytes += img.Length;
        UserDB.Save();

        FileDB.Add(this);
        return (this);
    }
}

public class Stats
{
    public long Bytes { get; set; }
    public long Users { get; set; }
    public long Files { get; set; }
    public DateTime Newest { get; set; }
}

public static class FileDB
{
    private static List<Img> db = new List<Img>();

    public static List<Img> GetDB() { return db; }

    public static void Load()
    {
        try
        {
            using (StreamReader SR = new StreamReader(Config.FileDBPath!))
            {
                string json = SR.ReadToEnd();

                db = JsonConvert.DeserializeObject<List<Img>>(json)!;
                Save();
            }
            Console.WriteLine($"Loaded DB of length {db.Count}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);//$"Unable to load {path}");
        }

        if (!File.Exists(Config.FileDBPath!))
        {
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
                Console.WriteLine($"{Config.FileDBPath!} saved!");
            }
        }
        catch
        {
            Console.WriteLine($"Error saving {Config.FileDBPath!}!");
        }
    }

    public static Img Find(string hash)
    {
        return db.Find(file => file.Hash == hash);
    }

    public static void Add(Img file)
    {
        if (Find(file.Hash!) is null)
        {
            db.Add(file);
            Save();
        }
    }

    public static void Remove(Img file)
    {
        db.Remove(file);
        Save();

        File.Delete("wwwroot/" + file.Filename);
        Console.WriteLine("Removed file " + file.Filename);
    }

    public static void Nuke(User user)
    {
        List<Img> images = new List<Img>(db);

        foreach (Img img in images)
        {
            if (img.UID == user.UID)
            {
                db.Remove(img);
                File.Delete("wwwroot/" + img.Filename);
            }
        }
        Console.WriteLine($"nuked {user.Username}");
        Save();
    }
}
