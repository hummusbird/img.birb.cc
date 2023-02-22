using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

// TODO:

// make stats filesize counter ignore .html, .js, .css and favicon.png
// make a release
// check foreach loops and use dict instead maybe possibly idk
// config
// log file?
// admin panel
// key rotation
// invite gen
// comments

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();  // redirect to 443
app.UseDefaultFiles();      // use index.html & index.cs
app.UseStaticFiles();       // use wwwroot
app.UseCors(MyAllowSpecificOrigins);

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

    List<Img> images = new List<Img>();
    User user = UserDB.GetUserFromKey(key.Value);

    foreach (var img in FileDB.GetDB())
    {
        if (img.UID == user.UID)
        {
            images.Add(img);
        }
    }

    return Results.Ok(images);
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
        UID = NewUID,
        APIKey = Hashing.HashString(NewKey),
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

    if (UserDB.GetUserFromKey(key.Value).IsAdmin) // return private info for admins
    {
        return Results.Ok(UserDB.GetDB().Select(x => x.UsrToDTO()).ToList());
    }

    return Results.Ok(UserDB.GetDB().Select(x => x.UsersToDTO()).ToList());
});

app.MapGet("/api/dashmsg", () => // get one random username + dashmsg 
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

    if (FileDB.GetDB().Count > 0) { stats.Newest = FileDB.GetDB().Last().Timestamp; }

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

    if (UserDB.GetUserFromKey(key.Value).IsAdmin == false) // only check magic bytes for non-admins
    {
        Stream? stream = new MemoryStream();
        await img.CopyToAsync(stream!);

        if (!Hashing.HasAllowedMagicBytes(stream!))
        {
            http.Response.StatusCode = 400;
            Console.WriteLine("illegal filetype");
            return;
        }
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
Hashing.LoadAllowedFileTypes();
FileDB.Load();
UserDB.Load();

app.Run();

public static class Hashing
{
    public static Random rand = new Random();
    private static string? salt;

    private static List<String> fileTypes = new List<string>(); //{ ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mp3", ".webm", ".mkv" };

    public static void LoadSalt()
    {
        try
        {
            using (StreamReader SR = new StreamReader("salt.txt"))
            {
                salt = SR.ReadToEnd().Trim();
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

    public static void LoadAllowedFileTypes()
    {
        fileTypes.Add("89504E470D0A1A0A");       // png
        fileTypes.Add("FFD8FF");                 // jpg
        fileTypes.Add("474946383761");           // gif
        fileTypes.Add("474946383961");           // gif
        fileTypes.Add("6674797069736F6D");       // mp4
        fileTypes.Add("FFFB");                   // mp3
        fileTypes.Add("FFF3");                   // mp3
        fileTypes.Add("FFF2");                   // mp3
        fileTypes.Add("494433");                 // mp3
        fileTypes.Add("1A45DFA3");               // webm & mkv+
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

    public static bool HasAllowedMagicBytes(Stream stream)
    {
        if (stream.Length < 16) { return false; }

        string magicbytes = "";
        stream.Position = 0;

        for (int i = 0; i < 8; i++) // load first 8 bytes from file
        {
            magicbytes += (stream.ReadByte().ToString("X2")); // convert from int to hex
        }

        foreach (string allowedMagicBytes in fileTypes)
        {
            if (magicbytes.StartsWith(allowedMagicBytes)) // compare to list of allowed filetypes
            {
                return true;
            }
        }

        return false;
    }
}
