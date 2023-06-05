using Newtonsoft.Json;

// TODO:
// different content for owner vs public (settings available)
// modify dashboard to add "add to album" button below every image
// add album tag to enable automatic uploads
// add batch uploads
// modify GetAlbumAsJson to lookup entire image objects
// create standard .js file for dashboard and album to have consistent functions and reduce complexity
// match accessors "init" with user object, remove NewAlbum() // can't do this because of deserialisation shenanigans

public class Album
{
    public string? Hash { get; set; }
    public string? Name { get; set; }
    public List<string>? ImageHashes { get; set; }
    public int UID { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsPublic { get; set; }

    internal Album NewAlbum(int uid, string name = "")
    {
        string NewHash = Hashing.NewHash(6);

        while (AlbumDB.Find(NewHash) is not null)
        {
            NewHash = Hashing.NewHash(6);
        }

        Hash = NewHash;
        Name = String.IsNullOrEmpty(name) ? NewHash : name;
        ImageHashes = new List<string>();
        UID = uid;
        IsPublic = false;
        Timestamp = DateTime.Now;

        AlbumDB.New(this);

        return this;
    }

    internal void AddImage(string hash)
    {
        // if image is already in list, remove then add to front
        if (ImageHashes!.Contains(hash)) { ImageHashes.Remove(hash); }
        ImageHashes?.Add(hash);
    }
}

public static class AlbumDB
{
    private static List<Album> db = new List<Album>();

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
            Log.Info($"{Config.AlbumDBPath} loaded - {db.Count} items");
        }
        catch
        {
            Log.Warning($"Unable to load {Config.AlbumDBPath}!");
        }

        if (!File.Exists(Config.AlbumDBPath!))
        {
            Log.Info($"Generating {Config.AlbumDBPath}...");
            Save();
        }
    }

    private static void Save()
    {
        try
        {
            using (StreamWriter SW = new StreamWriter(Config.AlbumDBPath!, false))
            {
                SW.WriteLine(JsonConvert.SerializeObject(db, Formatting.Indented));
                Log.Info($"{Config.AlbumDBPath!} saved!");
            }
        }
        catch
        {
            Log.Critical($"Unable to save {Config.AlbumDBPath!}!");
        }
    }

    public static Album Find(string hash)
    {
        return db.Find(album => album.Hash == hash);
    }

    public static void New(Album album)
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

    public static void AddImageToAlbum(string imagehash, string albumhash)
    {
        AlbumDB.Find(albumhash).AddImage(imagehash);
        Save();
    }

    public static void RemoveImageFromAlbum(string imagehash, string albumhash)
    {
        AlbumDB.Find(albumhash).ImageHashes!.Remove(imagehash);
        Save();
    }

    public static Album GetAlbum(string albumhash, User user) // checks permission compared to internal "find"
    {
        if (AlbumDB.Find(albumhash).IsPublic || (user is not null && AlbumDB.Find(albumhash).UID == user.UID))
        {
            return AlbumDB.Find(albumhash);
        }

        return null; // return null if private & invalid UID provided
    }

    public static List<Img> GetAlbumImages(string albumhash, User user) // checks permission compared to internal "find"
    {
        if (AlbumDB.Find(albumhash).IsPublic || (user is not null && AlbumDB.Find(albumhash).UID == user.UID))
        {
            List<Img> images = new List<Img>();

            foreach (string image in AlbumDB.Find(albumhash).ImageHashes!)
            {
                images.Add(FileDB.Find(image));
            }

            return images;
        }

        return null; // return null if private & invalid UID provided
    }
}
