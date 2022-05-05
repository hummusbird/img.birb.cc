using Newtonsoft.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

string[] fileTypes = { ".jpg", ".jpeg", ".png", ".gif", ".mp4" };

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.MapGet("/api/img", async Task<IResult> (HttpRequest request) =>
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

    return Results.Ok(FileDB.GetDB());
});

app.MapPost("/api/usr", async Task<IResult> (HttpRequest request) =>
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

    User newUser = new User
    {
        Username = NewUsername,
        IsAdmin = false,
        UID = NewUID,
        UploadCount = 0,
        APIKey = User.NewHash(40),
        ShowURL = true,
        Domain = "img.birb.cc"
    };

    UserDB.AddUser(newUser);

    string SXCU = "{\n";
    SXCU += "\"Version\": \"13.7.0\",'\n";
    SXCU += "\"Name\": \"birb.cc\",";
    SXCU += "\"DestinationType\": \"ImageUploader\",\n";
    SXCU += "\"RequestMethod\": \"POST\",\n";
    SXCU += "\"RequestURL\": \"https://img.birb.cc/api/upload\",\n";
    SXCU += "\"Body\": \"MultipartFormData\",\n";
    SXCU += "\"Arguments\": {\n";
    SXCU += $"\"api_key\": \"{newUser.APIKey}\"\n";
    SXCU += "},\n";
    SXCU += "\"FileFormName\": \"img\"\n";
    SXCU += "}";

    return Results.Text(SXCU);
});

app.MapPost("/api/usr/domain", async Task<IResult> (HttpRequest request) =>
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

    string NewDomain;

    var showURL = form.ToList().Find(showURL => showURL.Key == "showURL");

    if (!string.IsNullOrEmpty(showURL.Value) && showURL.Value == "true" || showURL.Value == "false")
    {
        UserDB.GetUserFromKey(key.Value).ShowURL = System.Convert.ToBoolean(showURL.Value);
    }

    NewDomain = domain.Value;

    UserDB.GetUserFromKey(key.Value).Domain = NewDomain;

    UserDB.Save();

    return Results.Accepted();
});

app.MapGet("/api/usr", async Task<IResult> (HttpRequest request) =>
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
        return Results.Ok(UserDB.GetDB());
    }

    return Results.Ok(UserDB.GetDB().Select(x => x.UserToDTO()).ToList());
});

app.MapGet("/api/stats", async () =>
{
    Stats stats = new Stats();
    stats.Files = FileDB.GetDB().Count;
    stats.Users = UserDB.GetDB().Count;
    DirectoryInfo dirInfo = new DirectoryInfo(@"img/");
    stats.Bytes = await Task.Run(() => dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(file => file.Length));
    stats.Newest = FileDB.GetDB().Last().Timestamp;

    return Results.Ok(stats);
});

app.MapPost("/api/upload", async (http) =>
{
    http.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null; // removes max filesize (set max in NGINX, not here)


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

    if (img is null || img.Length == 0) // no file or no exention
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

    using (var stream = System.IO.File.Create("img/" + newFile.Filename))
    {
        await img.CopyToAsync(stream);
    }

    Console.WriteLine($"New File: {newFile.Filename}");
    await http.Response.WriteAsync($"{(user.ShowURL ? "​" : "")}https://{user.Domain}/" + newFile.Filename); // First "" contains zero-width space
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
    if (!request.HasFormContentType )
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

FileDB.Load();
UserDB.Load();

app.Run();

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
        if (Find(file.Hash) is null)
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

                db = JsonConvert.DeserializeObject<List<Img>>(json);
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

        File.Delete("img/" + file.Filename);
        Console.WriteLine("Removed file" + file.Filename);
    }

    public static void Nuke(User user)
    {
        List<Img> temp = new List<Img>(db);

        foreach (Img img in temp)
        {
            if (img.UID == user.UID)
            {
                Remove(img);
            }
        }
    }
}

public class Img
{
    static Random random = new Random();

    public string? Hash { get; set; }
    public string? Filename { get; set; }
    public int UID { get; set; }

    public DateTime Timestamp { get; set; }

    public void NewImg(int uid, string extension, IFormFile img)
    {
        this.Hash = NewHash(8);
        this.Filename = this.Hash + extension;
        this.UID = uid;
        this.Timestamp = DateTime.Now;

        UserDB.GetUserFromUID(uid).UploadCount++;
        UserDB.GetUserFromUID(uid).UploadedBytes += img.Length;
        UserDB.Save();

        FileDB.Add(this);
    }

    static string NewHash(int length)
    {
        string b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string hash = "";

        while (FileDB.Find(hash) is not null || string.IsNullOrEmpty(hash))
        {
            hash = String.Empty;
            for (int i = 0; i < length; i++)
            {
                hash += b64[random.Next(b64.Length)];
            }
        }

        return hash;
    }
}

public class User
{
    static Random random = new Random();

    public bool IsAdmin { get; set; }
    public string? Username { get; set; }
    public int UID { get; set; }
    public int UploadCount { get; set; } = 0;
    public long UploadedBytes { get; set; } = 0;
    public string? APIKey { get; set; }
    public string? Domain { get; set; } = "img.birb.cc";
    public bool ShowURL { get; set; }

    public UserDTO UserToDTO()
    {
        return new UserDTO
        {
            Username = this.Username,
            UID = this.UID,
            UploadedBytes = this.UploadedBytes,
            UploadCount = this.UploadCount
        };
    }

    public static string NewHash(int length)
    {
        string b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string hash = "";

        while (UserDB.GetUserFromKey(hash) is not null || string.IsNullOrEmpty(hash))
        {
            hash = String.Empty;
            for (int i = 0; i < length; i++)
            {
                hash += b64[random.Next(b64.Length)];
            }
        }

        return hash;
    }
}

public class UserDTO
{
    public string? Username { get; set; }
    public int UID { get; set; }
    public long UploadedBytes { get; set; } = 0;
    public int UploadCount { get; set; } = 0;
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

                db = JsonConvert.DeserializeObject<List<User>>(json);
                Save();
            }
            Console.WriteLine($"Loaded DB of length {db.Count}");
        }
        catch
        {
            Console.WriteLine($"Unable to load {path}");
        }

        if (!File.Exists(path) || db.Count == 0)
        {
            Console.WriteLine("Generated default Admin account");
            User newUser = new User
            {
                Username = "Admin",
                IsAdmin = true,
                UID = 0,
                UploadCount = 0,
                APIKey = User.NewHash(40),
                ShowURL = true,
                Domain = "img.birb.cc"
            };
            Console.WriteLine("API-KEY: " + newUser.APIKey + " \nKeep this safe");
            UserDB.AddUser(newUser);
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
        return db.Find(user => user.APIKey == key);
    }
}