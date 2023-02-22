using Newtonsoft.Json;

public class User
{
    // public
    public string? Username { get; init; }
    public int UID { get; init; }
    public long UploadedBytes = 0;
    public int UploadCount = 0;

    // private
    public bool IsAdmin { get; set; }
    [JsonProperty] public string? APIKey { private get; init; }
    public string Domain = "img.birb.cc";
    public string? DashMsg;
    public bool ShowURL;

    public UsersDTO UsersToDTO() // public user info
    {
        return new UsersDTO
        {
            Username = this.Username,
            UID = this.UID,
            UploadedBytes = this.UploadedBytes,
            UploadCount = this.UploadCount
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
    public string Domain { get; set; } = "img.birb.cc";
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
    private readonly static string path = "user.json";
    private static List<User> db = new List<User>();

    public static void Load()
    {
        try
        {
            using (StreamReader SR = new StreamReader(path))
            {
                string json = SR.ReadToEnd();

                db = JsonConvert.DeserializeObject<List<User>>(json)!;
                Save();
            }
            Console.WriteLine($"Loaded DB of length {db!.Count}");
        }
        catch
        {
            Console.WriteLine($"Unable to load {path}");
        }

        if (!File.Exists(path) || db!.Count == 0) // Generate default admin account
        {
            string apikey = Hashing.NewHash(40);
            Console.WriteLine("Generated default Admin account");

            User newUser = new User
            {
                Username = "Admin",
                UID = 0,
                APIKey = Hashing.HashString(apikey),
                IsAdmin = true,
                DashMsg = "literally sharex compatible"
            };

            Console.WriteLine("API-KEY: " + apikey + "\nKeep this safe!");
            AddUser(newUser);
        }
    }

    public static void Save()
    {
        try
        {
            using (StreamWriter SW = new StreamWriter(path, false))
            {
                SW.WriteLine(JsonConvert.SerializeObject(db, Formatting.Indented));
                Console.WriteLine($"{path} saved!");
            }
        }
        catch
        {
            Console.WriteLine($"Error saving {path}!");
        }
    }

    public static void AddUser(User user)
    {
        db.Add(user);
        Save();
    }

    public static List<User> GetDB() { return db; }

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