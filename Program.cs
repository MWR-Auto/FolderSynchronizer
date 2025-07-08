using System.Security.Cryptography;
using System.Collections;
using Timer = System.Timers.Timer;

class FolderSynchronizer
{
    private static string sourcePath;
    private static string replicaPath;
    private static int syncIntervalSeconds;
    private static Timer syncTimer;
    static string logFile = "log_sync.txt";

    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: SyncTool.exe <sourceDir> <replicaDir> <intervalSeconds>");
            return;
        }

        sourcePath = Path.GetFullPath(args[0]);
        replicaPath = Path.GetFullPath(args[1]);

        if (!int.TryParse(args[2], out syncIntervalSeconds) || syncIntervalSeconds <= 0)
        {
            Console.WriteLine("Invalid sync interval.");
            return;
        }

        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine($"Source directory does not exist: {sourcePath}");
            return;
        }

        if (!Directory.Exists(replicaPath))
        {
            try
            {
                Directory.CreateDirectory(replicaPath);
            }
            catch
            {
                Console.WriteLine("Couldn't create replica directory.");
                return;
            }
        }

        // Run once on startup
        Sync();

        // Set up timer for repeated sync
        syncTimer = new Timer(syncIntervalSeconds * 1000);
        syncTimer.Elapsed += (s, e) => Sync();
        syncTimer.Start();

        Console.WriteLine("Sync running. Press Enter to exit.");
        Console.ReadLine();
    }

    static void Sync()
    {
        Log("== Sync started ==");
        try
        {
            // Make sure all folders from source exist in replica
            foreach (var dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                var rel = dir.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                var targetDir = Path.Combine(replicaPath, rel);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    Log("Created directory: " + targetDir);
                }
            }

            // Copy new and changed files from source to replica
            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                var targetFile = Path.Combine(replicaPath, rel);

                if (!File.Exists(targetFile) || !FilesAreEqual(file, targetFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                    File.Copy(file, targetFile, true);
                    Log("Copied/updated: " + targetFile);
                }
            }

            // Delete files that were removed from the source
            foreach (var file in Directory.GetFiles(replicaPath, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(replicaPath.Length).TrimStart(Path.DirectorySeparatorChar);
                var srcFile = Path.Combine(sourcePath, rel);
                if (!File.Exists(srcFile))
                {
                    File.Delete(file);
                    Log("Deleted file: " + file);
                }
            }

            // Delete directories that no longer exist in the source
            foreach (var dir in Directory.GetDirectories(replicaPath, "*", SearchOption.AllDirectories))
            {
                var rel = dir.Substring(replicaPath.Length).TrimStart(Path.DirectorySeparatorChar);
                var srcDir = Path.Combine(sourcePath, rel);
                if (!Directory.Exists(srcDir))
                {
                    Directory.Delete(dir, true);
                    Log("Deleted directory: " + dir);
                }
            }
        }
        catch (Exception ex)
        {
            Log("Error during sync: " + ex.Message);
        }
        Log("== Sync finished ==\n");
    }

    // Quick MD5-based file comparison
    static bool FilesAreEqual(string f1, string f2)
    {
        try
        {
            using var md5 = MD5.Create();
            using var s1 = File.OpenRead(f1);
            using var s2 = File.OpenRead(f2);
            var h1 = md5.ComputeHash(s1);
            var h2 = md5.ComputeHash(s2);
            return StructuralComparisons.StructuralEqualityComparer.Equals(h1, h2);
        }
        catch
        {
            return false; // If we can't compare, treat them as different
        }
    }

    // Simple console + file logging
    static void Log(string msg)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} {msg}";
        Console.WriteLine(entry);
        File.AppendAllText(logFile, entry + Environment.NewLine);
    }
}
