using Newtonsoft.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

// TODO:
// actually check fileheaders
// fix salt for windows platforms
// make a release
// check foreach loops and use dict instead maybe possibly idk
// hostname loaded from file
// log file?
// admin panel
// key rotation

string[] fileTypes = { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mp3", ".wav" };

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod(); ;
        });
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseDefaultFiles();

app.UseStaticFiles();

app.MapPost("/api/img", async Task<IResult> (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    List<Img> temp = new List<Img>();
    User user = UserDB.GetUserFromKey(key.Value);

    foreach (var img in FileDB.GetDB())
    {
        if (img.UID == user.UID)
        {
            temp.Add(img);
        }
    }

    return Results.Ok(temp);
});

app.MapPost("/api/usr", async Task<IResult> (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    return Results.Ok(UserDB.GetDB().Find(uid => uid.UID == UserDB.GetUserFromKey(key.Value).UID)!.UsrToDTO());
});

app.MapPost("/api/usr/new", async Task<IResult> (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null || !UserDB.GetUserFromKey(key.Value).IsAdmin) // invalid key
    {
        return Results.Unauthorized();
    }

    var username = form.ToList().Find(username => username.Key.ToLower() == "username");
    var UID = form.ToList().Find(UID => UID.Key.ToLower() == "uid");

    string NewUsername;
    int NewUID = 0;
    string NewKey = Hashing.NewHash(40);

    if (string.IsNullOrEmpty(username.Value) || UserDB.GetUserFromUsername(username.Value) is not null)
    {
        return Results.BadRequest("Invalid Username");
    }

    NewUsername = username.Value;

    if (string.IsNullOrEmpty(UID.Value) || UID.Key is null)
    {
        while (UserDB.GetUserFromUID(NewUID) is not null)
        {
            NewUID += 1;
        }
    }
    else
    {
        NewUID = int.Parse(UID.Value);
        if (UserDB.GetUserFromUID(NewUID) is not null)
        {
            return Results.BadRequest("UID Taken");
        }
    }

    while (UserDB.GetUserFromKey(NewKey) is not null)
    {
        NewKey = Hashing.NewHash(40);
    }

    User newUser = new User
    {
        Username = NewUsername,
        IsAdmin = false,
        UID = NewUID,
        UploadCount = 0,
        APIKey = Hashing.HashString(NewKey),
        ShowURL = true,
        Domain = "img.birb.cc"
    };

    UserDB.AddUser(newUser);

    return Results.Text(NewUsername + ": " + NewKey);
});

app.MapPost("/api/usr/settings", async Task<IResult> (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    var domain = form.ToList().Find(newDomain => newDomain.Key == "domain");
    var dashMsg = form.ToList().Find(dashMsg => dashMsg.Key == "dashMsg");
    var showURL = form.ToList().Find(showURL => showURL.Key == "showURL");

    User user = UserDB.GetUserFromKey(key.Value);

    if (!string.IsNullOrEmpty(showURL.Value) && showURL.Value == "true" || showURL.Value == "false")
    {
        user.ShowURL = System.Convert.ToBoolean(showURL.Value);
    }

    if (!string.IsNullOrEmpty(dashMsg.Value))
    {
        user.DashMsg = Regex.Replace(dashMsg.Value.ToString().Length < 100 ? dashMsg.Value.ToString() : dashMsg.Value.ToString().Substring(0, 100), @"[^\u0020-\u007E]", string.Empty);
    }
    else
    {
        user.DashMsg = null;
    }

    if (!string.IsNullOrEmpty(domain.Value))
    {
        user.Domain = domain.Value;
    }

    UserDB.Save();

    return Results.Accepted();
});

app.MapPost("/api/users", async Task<IResult> (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    if (UserDB.GetUserFromKey(key.Value).IsAdmin)
    {
        return Results.Ok(UserDB.GetDB().Select(x => x.UsrToDTO()).ToList());
    }

    return Results.Ok(UserDB.GetDB().Select(x => x.UsersToDTO()).ToList());
});

app.MapGet("/api/dashmsg", () =>
{
    List<DashDTO> usrlist = new List<DashDTO>();
    foreach (User user in UserDB.GetDB())
    {
        if (!string.IsNullOrEmpty(user.DashMsg))
        {
            usrlist.Add(user.DashToDTO());
        }
    }

    return usrlist.Count == 0 ? Results.NoContent() : Results.Ok(usrlist[Hashing.rand.Next(usrlist.Count)]);
});

app.MapGet("/api/stats", async () =>
{
    Stats stats = new Stats();
    stats.Files = FileDB.GetDB().Count;
    stats.Users = UserDB.GetDB().Count;
    DirectoryInfo dirInfo = new DirectoryInfo(@"wwwroot/");
    stats.Bytes = await Task.Run(() => dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(file => file.Length));
    stats.Newest = FileDB.GetDB().Last().Timestamp;

    return Results.Ok(stats);
});

app.MapPost("/api/upload", async (http) =>
{
    http.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = null; // removes max filesize (set max in NGINX, not here)


    if (!http.Request.HasFormContentType)
    {
        http.Response.StatusCode = 400;
        return;
    }

    var form = await http.Request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        http.Response.StatusCode = 401;
        return;
    }

    var img = form.Files["img"];

    if (img is null || img.Length == 0) // no file or no file extention
    {
        Console.WriteLine("Invalid upload");
        http.Response.StatusCode = 400;
        return;
    }

    string extension = Path.GetExtension(img.FileName);

    if (extension is null || extension.Length == 0 || !fileTypes.Contains(extension) && !UserDB.GetUserFromKey(key.Value).IsAdmin) // invalid extension
    {
        http.Response.StatusCode = 400;
        return;
    }

    Img newFile = new Img();
    User user = UserDB.GetUserFromKey(key.Value);

    newFile.NewImg(user.UID, extension, img);

    using (var stream = System.IO.File.Create("wwwroot/" + newFile.Filename))
    {
        await img.CopyToAsync(stream);
    }

    Console.WriteLine($"New File: {newFile.Filename}");
    string[] domains = user.Domain!.Split("\r\n");
    string domain = domains[Hashing.rand.Next(domains.Length)];

    await http.Response.WriteAsync($"{(user.ShowURL ? "​" : "")}https://{domain}/" + newFile.Filename); // First "" contains zero-width space
    return;
});

app.MapDelete("/api/delete/{hash}", async Task<IResult> (HttpRequest request, string hash) =>
{
    if (!request.HasFormContentType || string.IsNullOrEmpty(hash))
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    Img deleteFile = FileDB.Find(hash);

    if (deleteFile == null)
    {
        return Results.NotFound();
    }

    if (deleteFile.UID == UserDB.GetUserFromKey(key.Value).UID || UserDB.GetUserFromKey(key.Value).IsAdmin)
    {
        FileDB.Remove(deleteFile);
        return Results.Ok();
    }

    return Results.Unauthorized();
});

app.MapDelete("/api/nuke", async Task<IResult> (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    FileDB.Nuke(UserDB.GetUserFromKey(key.Value));

    return Results.Ok();
});

Hashing.LoadSalt();
FileDB.Load();
UserDB.Load();

app.UseCors(MyAllowSpecificOrigins);

app.Run();

public static class Hashing
{
    public static Random rand = new Random();
    private static string? salt;

    public static void LoadSalt()
    {
        try
        {
            using (StreamReader SR = new StreamReader("salt.txt"))
            {
                salt = SR.ReadToEnd();
            }
            Console.WriteLine($"Loaded salt");
        }
        catch
        {
            Console.WriteLine($"Unable to load salt");
        }

        if (!File.Exists("salt.txt") || string.IsNullOrEmpty(salt))
        {
            Console.WriteLine("Generating new salt. Keep this safe!!!");
            using (StreamWriter SW = new StreamWriter("salt.txt"))
            {
                salt = NewHash(40);
                SW.WriteLine(salt);
            }
        }
    }

    public static string HashString(string input) // yes, i know the use of "hash" is very inconsistent. shut up.
    {
        byte[] hash;
        using (SHA512 shaM = SHA512.Create())
        {
            hash = shaM.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input + salt));
        }
        return Convert.ToBase64String(hash);
    }

    public static string NewHash(int length)
    {
        string b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        string hash = String.Empty;
        for (int i = 0; i < length; i++)
        {
            hash += b64[rand.Next(b64.Length)];
        }

        return hash;
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
    private readonly static string path = "img.json";
    private static List<Img> db = new List<Img>();

    public static List<Img> GetDB()
    {
        return db;
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

    public static void Load()
    {
        try
        {
            using (StreamReader SR = new StreamReader(path))
            {
                string json = SR.ReadToEnd();

                db = JsonConvert.DeserializeObject<List<Img>>(json)!;
                Save();
            }
            Console.WriteLine($"Loaded DB of length {db.Count}");
        }
        catch
        {
            Console.WriteLine($"Unable to load {path}");
        }

        if (!File.Exists(path))
        {
            Save();
        }
    }

    private static void Save()
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

public class Img
{
    public string? Hash { get; set; }
    public string? Filename { get; set; }
    public int UID { get; set; }

    public DateTime Timestamp { get; set; }

    public void NewImg(int uid, string extension, IFormFile img)
    {
        string newHash = Hashing.NewHash(8);
        while (FileDB.Find(newHash) is not null)
        {
            newHash = Hashing.NewHash(8);
        }

        this.Hash = newHash;
        this.Filename = this.Hash + extension;
        this.UID = uid;
        this.Timestamp = DateTime.Now;

        UserDB.GetUserFromUID(uid).UploadCount++;
        UserDB.GetUserFromUID(uid).UploadedBytes += img.Length;
        UserDB.Save();

        FileDB.Add(this);
    }
}

public class User
{
    public bool IsAdmin { get; set; }
    public string? Username { get; set; }
    public int UID { get; set; }
    public int UploadCount { get; set; } = 0;
    public long UploadedBytes { get; set; } = 0;
    public string? APIKey { get; set; }
    public string Domain { get; set; } = "img.birb.cc";
    public string? DashMsg { get; set; }
    public bool ShowURL { get; set; }

    public UsersDTO UsersToDTO()
    {
        return new UsersDTO
        {
            Username = this.Username,
            UID = this.UID,
            UploadedBytes = this.UploadedBytes,
            UploadCount = this.UploadCount
        };
    }

    public UsrDTO UsrToDTO()
    {
        return new UsrDTO
        {
            IsAdmin = this.IsAdmin,
            Username = this.Username,
            UID = this.UID,
            UploadCount = this.UploadCount,
            UploadedBytes = this.UploadedBytes,
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
    public bool IsAdmin { get; set; }
    public string? Username { get; set; }
    public int UID { get; set; }
    public int UploadCount { get; set; } = 0;
    public long UploadedBytes { get; set; } = 0;
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
                IsAdmin = true,
                UID = 0,
                UploadCount = 0,
                APIKey = Hashing.HashString(apikey),
                DashMsg = "literally sharex compatible",
                ShowURL = true,
                Domain = "img.birb.cc"
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

    public static List<User> GetDB()
    {
        return db;
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
        return db.Find(user => user.APIKey == Hashing.HashString(key));
    }
}