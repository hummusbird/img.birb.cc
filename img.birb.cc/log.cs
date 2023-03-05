using System.Runtime.CompilerServices;

public static partial class Log
{
    public static readonly TimeSpan MAX_LOG_AGE = new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0);

    private static string prefix = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}";

    public static Action<string, string>? MessageBoxProvider { get; set; }
    private static Mutex LogMutex = new Mutex();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Info(object value,
                    [CallerMemberName] string name = "",
                    [CallerLineNumber] int line = -1,
                    [CallerFilePath] string path = "")
    {
        WriteLog(value, name, line, path, LogLevel.INFO, ConsoleColor.Green, ConsoleColor.Black);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Debug(object value,
                    [CallerMemberName] string name = "",
                    [CallerLineNumber] int line = -1,
                    [CallerFilePath] string path = "")
    {
        WriteLog(value, name, line, path, LogLevel.DBUG, ConsoleColor.Blue, ConsoleColor.Black);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Warning(object value,
                    [CallerMemberName] string name = "",
                    [CallerLineNumber] int line = -1,
                    [CallerFilePath] string path = "")
    {
        WriteLog(value, name, line, path, LogLevel.WARN, ConsoleColor.Yellow, ConsoleColor.Black);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Error(object value,
                    [CallerMemberName] string name = "",
                    [CallerLineNumber] int line = -1,
                    [CallerFilePath] string path = "")
    {
        WriteLog(value, name, line, path, LogLevel.ERRR, ConsoleColor.Red, ConsoleColor.Black);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Critical(object value,
                    [CallerMemberName] string name = "",
                    [CallerLineNumber] int line = -1,
                    [CallerFilePath] string path = "")
    {
        WriteLog(value, name, line, path, LogLevel.CRIT, ConsoleColor.White, ConsoleColor.Red);
    }

    private static void WriteLog(object value, string functionName, int line, string path, LogLevel level, ConsoleColor fg, ConsoleColor bg)
    {
        lock (LogMutex)
        {
            string logMessage = $"[{level.ToString()}] [{DateTime.Now.ToString("HH:mm:ss.fff")}] [{new FileInfo(path.ToString()).Name}:{functionName}:{line}] {value}";

            Console.Write("[");
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
            Console.Write($"{level.ToString()}");
            Console.ResetColor();
            Console.Write($"] [{DateTime.Now.ToString("HH:mm:ss")}] ");
            if (level == LogLevel.CRIT) { Console.Write($"[{new FileInfo(path.ToString()).Name}:{functionName}:{line}] "); }
            Console.WriteLine($"{value}");

            using (System.IO.StreamWriter file = new System.IO.StreamWriter($@"{Config.LogPath}/latest.log", append: true)) { file.WriteLine(logMessage); }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter($@"{Config.LogPath}/{prefix}.log", append: true)) { file.WriteLine(logMessage); }
        }
    }

    public static void Initialize()
    {
        if (!Directory.Exists($@"{Config.LogPath}"))
            Directory.CreateDirectory($@"{Config.LogPath}");
        if (File.Exists($@"{Config.LogPath}/latest.log"))
        {
            System.IO.File.WriteAllText($@"{Config.LogPath}/latest.log", string.Empty);
        }

        Thread cleanupThread = new Thread(RunLogCleanup);
        cleanupThread.Start();
    }

    private static void RunLogCleanup()
    {
        foreach (FileInfo file in new DirectoryInfo($@"{Config.LogPath}/").GetFiles())
        {
            if (file.LastWriteTime < DateTime.Now - MAX_LOG_AGE)
            {
                Log.Debug($"Deleting old log file {file.Name}");
                file.Delete();
            }
        }
    }
}

public enum LogLevel
{
    INFO = 0,
    DBUG = 1,
    WARN = 2,
    ERRR = 3,
    CRIT = 4,
}
