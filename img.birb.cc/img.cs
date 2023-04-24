using Newtonsoft.Json;

public class Img
{
    public string? Hash { get; set; }
    public string? Filename { get; set; }
    public int UID { get; set; }
    public DateTime Timestamp { get; set; }

    internal Img NewImg(int uid, string extension, IFormFile img)
    {
        string NewHash = Hashing.NewHash(8);
        while (FileDB.Find(NewHash) is not null)
        {
            NewHash = Hashing.NewHash(8);
        }

        Hash = NewHash;
        Filename = Hash + extension;
        UID = uid;
        Timestamp = DateTime.Now;

        UserDB.GetUserFromUID(uid).UploadCount++;
        UserDB.GetUserFromUID(uid).UploadedBytes += img.Length;
        UserDB.Save();

        FileDB.Add(this);
        return (this);
    }

    public static bool HasAllowedMagicBytes(Stream stream)
    {
        if (Config.AllowedFileTypes!.Count == 0) { return true; } // if no filetypes listed, allow all

        stream.Position = 0;

        byte[] header = new byte[16]; // new 16 byte buffer
        int num = stream.Read(header, 0, 16); // read first 16 bytes

        string magicbytes = BitConverter.ToString(header).Replace("-", ""); // convert byte array to hex string

        foreach (string allowedMagicBytes in Config.AllowedFileTypes!.Where(allowedMagicBytes => magicbytes.Contains(allowedMagicBytes)))
        { // compare to list of allowed filetypes
            return true;
        }

        return false;
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
        Log.Info("Removed file " + file.Filename);
    }

    public static void Nuke(User user)
    {
        List<Img> images = new List<Img>(db);

        foreach (Img img in images.Where(img => img.UID == user.UID))
        {
            db.Remove(img);
            File.Delete("wwwroot/" + img.Filename);
        }
        Log.Info($"nuked {user.Username}");
        Save();
    }
}
