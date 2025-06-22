namespace MAEPS.Data.Processor.Preprocessors;

public static class DataPreProcessor
{
    public static string CopyDataFolder(string experimentsFolderPath, string folderName = "Copy", bool regenerate = true)
    {
        var experimentsFolderCopyPath = experimentsFolderPath.TrimEnd('/', '\\') + folderName;
        
        if (Directory.Exists(experimentsFolderCopyPath))
        {
            if (!regenerate)
            {
                Console.WriteLine("Experiments folder copy already exists: " + experimentsFolderCopyPath);
                return experimentsFolderCopyPath;
            }
            
            Console.WriteLine("Removing existing experiments folder copy: " + experimentsFolderCopyPath);
            Directory.Delete(experimentsFolderCopyPath, true);
        }
        
        Directory.CreateDirectory(experimentsFolderCopyPath);
        foreach (var file in Directory.GetFiles(experimentsFolderPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(experimentsFolderPath, file);
            var destinationPath = Path.Combine(experimentsFolderCopyPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath);
        }
        Console.WriteLine("Copied experiments data to folder: " + experimentsFolderCopyPath);

        return experimentsFolderCopyPath;
    }
}