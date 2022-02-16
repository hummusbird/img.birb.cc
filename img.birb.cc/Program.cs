using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

string[] fileTypes = { ".jpg", ".jpeg", ".png", ".gif" };


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Welcome to img.birb.cc");

app.MapGet("/{hash}", (string hash) =>
{
    Img image = FileDB.Find(hash);
    if (image == null) { return Results.NotFound("File not found"); }
    return Results.Redirect("img/" + image.filename);
});

app.MapPost("/upload",
    async Task<IResult> (HttpRequest request) =>
    {
        if (!request.HasFormContentType) { return Results.BadRequest(); }

        var form = await request.ReadFormAsync();
        var img = form.Files["img"];
        
        if (img is null || img.Length == 0) // no file or no exention
        {
            return Results.BadRequest();
        }

        Console.WriteLine($"New File: {img.FileName}");

        string extension = Path.GetExtension(img.FileName);

        if (extension is null || extension.Length == 0 || !fileTypes.Contains(extension)) // invalid extension
        {
            return Results.BadRequest("Invalid filetype or extension");
        }

        Img newFile = new Img("hummusbird", extension);

        using (var stream = System.IO.File.Create("img/"+newFile.filename))
        {
            await img.CopyToAsync(stream);
        }

        return Results.Ok("img.birb.cc/" + newFile.hash);
    });

FileDB.Load();

app.Run();

public static class FileDB
{
    private static string path = "files.json";
    private static List<Img> db = new List<Img>();

    public static Img? Find(string hash)
    {
        return db.Find(file => file.hash == hash);
    }
    public static void Add(Img file)
    {
        db.Add(file);
        Save();
    }
    public static void Load()
    {

        try
        {
            using (StreamReader SR = new StreamReader(path))
            {
                string json = SR.ReadToEnd();

                db = JsonConvert.DeserializeObject<List<Img>>(json);
            }

            Console.WriteLine($"Loaded DB of length {db.Count()}");
            Save();
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex);
            Console.WriteLine($"Unable to load {path}");
        }

    }
    private static void Save()
    {
        using (StreamWriter SW = new StreamWriter(path, false))
        {
            try
            {
                SW.WriteLine(JsonConvert.SerializeObject(db));
                Console.WriteLine($"{path} saved!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving {path}!\n{ex}");
            }
        }
    }
}

public class Img
{
    static Random random = new Random();
    public string? hash;
    public string? filename;
    public string? user;

    public Img(string user, string extension)
    {
        this.hash = newHash(8);
        this.filename = this.hash + extension;
        this.user = user;

        FileDB.Add(this);
    }

    string newHash(int length)
    {
        string b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string hash = "";
        bool used = true;

        while (used)
        {
            hash = String.Empty;
            for (int i = 0; i < length; i++)
            {
                hash += b64[random.Next(b64.Length)];
            }

            if (FileDB.Find(hash) is null) { used = false; }
        }

        return hash;
    }
}

