using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using System.Text.RegularExpressions;

// TODO:

// make a release
// key rotation
// EXIF strip
// checksum + multiple file check
// invite gen
// comments

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
        });
});

WebApplication app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();  // redirect 80 to 443
app.UseDefaultFiles();      // use index.html & index.cs
app.UseStaticFiles();       // enable static file serving
app.UseCors(MyAllowSpecificOrigins);

app.MapPost("/api/img", async Task<IResult> (HttpRequest request) => // get your uploaded files
{
    if (!request.HasFormContentType) { return Results.BadRequest(); }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");
    var specified_uid = form.ToList().Find(uid => uid.Key == "uid");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    User user = UserDB.GetUserFromKey(key.Value);

    if (specified_uid.Key is not null && UserDB.GetUserFromUID(int.Parse(specified_uid.Value)) is not null && UserDB.GetUserFromKey(key.Value).IsAdmin) // allow admins to request images via UID
    {
        user = UserDB.GetUserFromUID(int.Parse(specified_uid.Value));
    }

    List<Img> images = new List<Img>();

    foreach (var img in FileDB.GetDB().Where(img => img.UID == user.UID)) // every image which has a matching UID
    {
        images.Add(img);
    }

    return Results.Ok(images);
});

app.MapPost("/api/usr", async Task<IResult> (HttpRequest request) => // get your user data
{
    if (!request.HasFormContentType) { return Results.BadRequest(); }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    return Results.Ok(UserDB.GetDB().Find(uid => uid.UID == UserDB.GetUserFromKey(key.Value).UID)!.UsrToDTO());
});

app.MapPost("/api/usr/new", async Task<IResult> (HttpRequest request) => // create new user
{
    if (!request.HasFormContentType) { return Results.BadRequest(); }

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
        while (UserDB.GetUserFromUID(NewUID) is not null) // increase UID until a non-taken one is found
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

    Log.Info($"New user! USERNAME: {NewUsername} UID: {NewUID}");

    return Results.Json("{\"" + NewUsername + "\": \"" + NewKey + "\"}");
});

app.MapPost("/api/usr/settings", async Task<IResult> (HttpRequest request) => // update user settings
{
    if (!request.HasFormContentType) { return Results.BadRequest(); }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    var domain = form.ToList().Find(newDomain => newDomain.Key == "domain");
    var dashMsg = form.ToList().Find(dashMsg => dashMsg.Key == "dashMsg");
    var showURL = form.ToList().Find(showURL => showURL.Key == "showURL");
    var stripExif = form.ToList().Find(stripExif => stripExif.Key == "stripEXIF");

    User user = UserDB.GetUserFromKey(key.Value);

    if (!string.IsNullOrEmpty(showURL.Value) && showURL.Value == "true" || showURL.Value == "false")
    {
        user.ShowURL = System.Convert.ToBoolean(showURL.Value);
    }

    if (!string.IsNullOrEmpty(stripExif.Value) && stripExif.Value == "true" || stripExif.Value == "false")
    {
        user.StripEXIF = System.Convert.ToBoolean(stripExif.Value);
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

app.MapPost("/api/users", async Task<IResult> (HttpRequest request) => // get registered users
{
    if (!request.HasFormContentType) { return Results.BadRequest(); }

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
    foreach (User user in UserDB.GetDB().Where(user => !string.IsNullOrEmpty(user.DashMsg)))
    {
        usrlist.Add(user.DashToDTO());
    }

    return usrlist.Count == 0 ? Results.NoContent() : Results.Ok(usrlist[Hashing.rand.Next(usrlist.Count)]);
});

app.MapGet("/api/stats", async () => // get host stats
{
    Stats stats = new Stats();
    stats.Files = FileDB.GetDB().Count;
    stats.Users = UserDB.GetDB().Count;

    // iterate through every file in wwwroot, ignoring .* and *.html and favicon - then sum filesize.
    DirectoryInfo dirInfo = new DirectoryInfo(@"wwwroot/");
    stats.Bytes = await Task.Run(() => dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(file => file.Extension != ".html" && !file.Name.StartsWith(".") && file.Name != "favicon.png").Sum(file => file.Length));

    // get timestamp of latest uploaded file
    if (FileDB.GetDB().Count > 0) { stats.Newest = FileDB.GetDB().Last().Timestamp; }

    return Results.Ok(stats);
});

app.MapPost("/api/upload", async (http) => // upload file
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
        Log.Warning("Invalid upload");
        http.Response.StatusCode = 400;
        return;
    }

    string extension = Path.GetExtension(img.FileName);

    Stream? stream = new MemoryStream();
    await img.CopyToAsync(stream); // copy image to memorystream

    if (UserDB.GetUserFromKey(key.Value).IsAdmin != true) // only check magic bytes for non-admins
    {
        if (!Img.HasAllowedMagicBytes(stream))
        {
            http.Response.StatusCode = 400;
            Log.Warning("Illegal filetype - Upload rejected");
            return;
        }
    }

    User user = UserDB.GetUserFromKey(key.Value);

    if (user.StripEXIF) { stream = Img.StripExif(stream); }
    else { Log.Warning("Exif NOT stripped - user disabled"); }

    Img newFile = new Img().NewImg(user.UID, extension, img);

    using (var filestream = System.IO.File.Create("wwwroot/" + newFile.Filename))
    {
        stream.Position = 0;
        stream.CopyTo(filestream);
    }

    Log.Info($"New upload: {newFile.Filename}");
    Log.Info($"UID: {user.UID} Username: {user.Username}");
    string[] domains = user.Domain!.Split("\r\n");
    string domain = domains[Hashing.rand.Next(domains.Length)];

    await http.Response.WriteAsync($"{(user.ShowURL ? "​" : "")}https://{domain}/" + newFile.Filename); // First "" contains zero-width space
    return;
});

app.MapDelete("/api/delete/{hash}", async Task<IResult> (HttpRequest request, string hash) => // delete specific file
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

app.MapDelete("/api/nuke", async Task<IResult> (HttpRequest request) => // delete all your files
{
    if (!request.HasFormContentType) { return Results.BadRequest(); }

    var form = await request.ReadFormAsync();
    var key = form.ToList().Find(key => key.Key == "api_key");

    if (key.Key is null || UserDB.GetUserFromKey(key.Value) is null) // invalid key
    {
        return Results.Unauthorized();
    }

    FileDB.Nuke(UserDB.GetUserFromKey(key.Value));

    return Results.Ok();
});

Log.Initialize();
Config.Load();
Hashing.LoadSalt();
FileDB.Load();
UserDB.Load();

app.Run();
