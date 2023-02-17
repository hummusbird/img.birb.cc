using Newtonsoft.Json;

public class User
{
    // public
    public readonly string? Username;
    public readonly int UID;
    public long UploadedBytes { get; set; } = 0;
    public int UploadCount { get; set; } = 0;

    // private
    public bool IsAdmin { get; }
    public string? APIKey { get; private set; }
    public string Domain { get; set; } = "img.birb.cc";
    public string? DashMsg { get; set; }
    public bool ShowURL { get; set; }

    public User(string username, int uid, string apikey, bool isadmin = false, string? dashmsg = null, long uploadedbytes = 0, int uploadcount = 0, string domain = "img.birb.cc", bool showurl = true)
    {
        Username = username;
        UID = uid;
        APIKey = apikey;
        IsAdmin = isadmin;
        DashMsg = dashmsg;

        // used for deserialization
        UploadedBytes = uploadedbytes;
        UploadCount = uploadcount;
        Domain = domain;
        ShowURL = showurl;
    }

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

            User newUser = new User("Admin", 0, Hashing.HashString(apikey), true, "literally sharex compatible");

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