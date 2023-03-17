using Newtonsoft.Json;

public class User
{
    // public
    public string? Username { get; init; }
    public int UID { get; init; }
    public long UploadedBytes = 0;
    public int UploadCount = 0;

    // private
    public bool IsAdmin { get; init; }
    [JsonProperty] public string? APIKey { private get; init; }
    public string Domain = Config.DefaultDomain!;
    public string? DashMsg;
    public bool ShowURL;

    public UsersDTO UsersToDTO() // public user info
    {
        return new UsersDTO
        {
            Username = this.Username,
            UID = this.UID
        };
    }

    public UsrDTO UsrToDTO() // private user info
    {
        return new UsrDTO
        {
            Username = this.Username,
            UID = this.UID,
            UploadedBytes = this.UploadedBytes,
            UploadCount = this.UploadCount,

            IsAdmin = this.IsAdmin,
            Domain = this.Domain,
            DashMsg = this.DashMsg,
            ShowURL = this.ShowURL
        };
    }

    public DashDTO DashToDTO()
    {
        return new DashDTO
        {
            Username = this.Username,
            DashMsg = this.DashMsg
        };
    }

    public bool KeyMatches(string input)
    {
        return APIKey == input;
    }
}

public class UsersDTO // used for /api/users
{
    public string? Username { get; set; }
    public int UID { get; set; }
    public long UploadedBytes { get; set; } = 0;
    public int UploadCount { get; set; } = 0;
}

public class UsrDTO // used for /api/usr
{
    public string? Username { get; set; }
    public int UID { get; set; }
    public long UploadedBytes { get; set; } = 0;
    public int UploadCount { get; set; } = 0;

    public bool IsAdmin { get; set; }
    public string Domain { get; set; } = Config.DefaultDomain!;
    public string? DashMsg { get; set; }
    public bool ShowURL { get; set; }
}

public class DashDTO // used for /api/dashmsg
{
    public string? Username { get; set; }
    public string? DashMsg { get; set; }
}

public static class UserDB
{
    private static List<User> db = new List<User>();

    public static List<User> GetDB() { return db; }

    public static void Load()
    {
        try
        {
            using (StreamReader SR = new StreamReader(Config.UserDBPath!))
            {
                string json = SR.ReadToEnd();

                db = JsonConvert.DeserializeObject<List<User>>(json)!;
                Save();
            }
            Log.Info($"{Config.UserDBPath} loaded - {db.Count} items");
        }
        catch
        {
            Log.Warning($"Unable to load {Config.UserDBPath!}");
        }

        if (!File.Exists(Config.UserDBPath!) || db.Count == 0) // Generate default admin account
        {
            string apikey = Hashing.NewHash(40);
            Log.Info("Generating default Admin account...");

            User newUser = new User
            {
                Username = "Admin",
                UID = 0,
                APIKey = Hashing.HashString(apikey),
                IsAdmin = true,
                DashMsg = "literally sharex compatible"
            };

            Log.Info("API-KEY:");
            Console.WriteLine(apikey);
            Log.Info("Keep this safe!");
            AddUser(newUser);
        }
    }

    public static void Save()
    {
        try
        {
            using (StreamWriter SW = new StreamWriter(Config.UserDBPath!, false))
            {
                SW.WriteLine(JsonConvert.SerializeObject(db, Formatting.Indented));
                Log.Info($"{Config.UserDBPath!} saved!");
            }
        }
        catch
        {
            Log.Critical($"Unable to save {Config.UserDBPath!}!");
        }
    }

    public static void AddUser(User user)
    {
        db.Add(user);
        Save();
    }

    public static User GetUserFromUsername(string username)
    {
        return db.Find(user => user.Username == username);
    }

    public static User GetUserFromUID(int uid)
    {
        return db.Find(user => user.UID == uid);
    }

    public static User GetUserFromKey(string key)
    {
        return db.Find(user => user.KeyMatches(Hashing.HashString(key)));
    }
}