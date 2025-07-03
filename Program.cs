using System.Security.Cryptography;
using System.Collections;

class FolderSynchronizer
{
    private static string sourcePath;
    private static string replicaPath;
    private static string syncIntervalSeconds;
    private static System.Timers.Timer syncTimer;

    static void Main(string[] args)
    {
        sourcePath = args[0];
        replicaPath = args[1];
        syncIntervalSeconds = args[2];

        Synchronize();

        syncTimer = new System.Timers.Timer(Int32.Parse(syncIntervalSeconds) * 1000);
        syncTimer.Elapsed += (sender, e) => Synchronize();
        syncTimer.Start();
    }

    private static void Synchronize()
    {
        SyncDirectories(sourcePath, replicaPath);
        DeleteObsoleteFiles(replicaPath, sourcePath);
    }

    private static void SyncDirectories(string sourceDir, string replicaDir)
    {
        foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var replicaSubDir = dirPath.Replace(sourceDir, replicaDir);
            if (!Directory.Exists(replicaSubDir))
            {
                Directory.CreateDirectory(replicaSubDir);
            }
        }

        foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var replicaFilePath = filePath.Replace(sourceDir, replicaDir);

            if (!File.Exists(replicaFilePath) || !FilesAreEqual(filePath, replicaFilePath))
            {
                File.Copy(filePath, replicaFilePath, true);
            }
        }
    }

    private static void DeleteObsoleteFiles(string replicaDir, string sourceDir)
    {
        foreach (var filePath in Directory.GetFiles(replicaDir, "*", SearchOption.AllDirectories))
        {
            var sourceFilePath = filePath.Replace(replicaDir, sourceDir);

            if (!File.Exists(sourceFilePath))
            {
                File.Delete(filePath);
            }
        }

        foreach (var dirPath in Directory.GetDirectories(replicaDir, "*", SearchOption.AllDirectories))
        {
            var sourceDirPath = dirPath.Replace(replicaDir, sourceDir);

            if (!Directory.Exists(sourceDirPath))
            {
                Directory.Delete(dirPath, true);
            }
        }
    }

    private static bool FilesAreEqual(string file1, string file2)
    {
        using var md5 = MD5.Create();
        using var fs1 = File.OpenRead(file1);
        using var fs2 = File.OpenRead(file2);

        var hash1 = md5.ComputeHash(fs1);
        var hash2 = md5.ComputeHash(fs2);

        return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
    }
}
