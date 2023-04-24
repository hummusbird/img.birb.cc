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

        byte[] header = new byte[16];
        stream.Read(header, 0, 16); // read first 16 bytes

        string magicbytes = BitConverter.ToString(header).Replace("-", ""); // convert byte array to hex string

        foreach (string allowedMagicBytes in Config.AllowedFileTypes!.Where(allowedMagicBytes => magicbytes.Contains(allowedMagicBytes)))
        { // compare to list of allowed filetypes
            return true;
        }

        return false;
    }

    public static Stream StripExif(Stream stream)
    {

        // todo: replace with 
        // byte[] rest;
        // ms.Write(rest, ms.Length-ms.Position, rest.Length);

        int exif_pos = 0;
        int exif_len = 0;

        for (int i = 0; i < 16; i++) // read first 16 bytes
        {
            stream.Position = i; // reset position if 0xFF is read without 0xE1

            if (stream.ReadByte() == 255 && stream.ReadByte() == 225) // match 0xFFE1
            {
                exif_pos = (int)stream.Position;
                exif_len = stream.ReadByte() * 256 + stream.ReadByte();

                Stream strippedstream = new MemoryStream();
                strippedstream.SetLength(stream.Length - exif_len); // set newstream length to length of old stream minus Exif

                Log.Debug("streamlen: " + stream.Length);
                Log.Debug("newlen: " + (stream.Length - exif_len));

                stream.Position = 0;
                byte[] pre_exif = new byte[16];
                stream.Read(pre_exif, 0, exif_pos - 2); // read bytes up until header into pre_exif

                stream.Position = exif_len + exif_pos;
                byte[] post_exif = new byte[(int)stream.Length - exif_len];
                stream.Read(post_exif, 0, (int)stream.Length - exif_len - exif_pos); // read bytes from end of exif to end of stream into post_exif

                Log.Debug("exif len: " + (exif_len));
                Log.Debug("exif pos: " + (exif_pos));

                strippedstream.Position = 0;
                strippedstream.Write(pre_exif, 0, exif_pos - 2); // write pre_exif into stripped stream
                strippedstream.Write(post_exif, 0, (int)stream.Length - exif_len - exif_pos); // write post_exif into stripped stream

                return strippedstream;
            }
        }

        return stream;
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
