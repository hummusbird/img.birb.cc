using System.Security.Cryptography;

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
                salt = SR.ReadToEnd().Trim();
            }
            Log.Info($"Loaded salt");
        }
        catch
        {
            Log.Warning($"Unable to load salt");
        }

        if (!File.Exists("salt.txt") || string.IsNullOrEmpty(salt))
        {
            Log.Info("Generating new salt. Keep this safe!!!");
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
