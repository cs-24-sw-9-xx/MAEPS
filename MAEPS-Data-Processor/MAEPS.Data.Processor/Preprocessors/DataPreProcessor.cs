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
    
    public static string[] SplitMapTypesDataFolder(string experimentsCopyFolderPath, bool regenerate = true)
    {
        var experimentsFolderBuildingMapPath = Split(experimentsCopyFolderPath, "BuildingMap", regenerate);
        var experimentsFolderCaveMapPath = Split(experimentsCopyFolderPath, "CaveMap", regenerate);
        
        return [experimentsFolderBuildingMapPath, experimentsFolderCaveMapPath];
    }


    private static string Split(string experimentsCopyFolderPath, string mapType, bool regenerate)
    {
        var experimentsFolderMapTypePath = Path.Combine(experimentsCopyFolderPath, mapType);
        
        if (Directory.Exists(experimentsFolderMapTypePath))
        {
            if (!regenerate)
            {
                Console.WriteLine($"Folder for mapType {mapType} already exists: " + experimentsFolderMapTypePath);
                return experimentsFolderMapTypePath;
            }
            
            Console.WriteLine($"Removing existing mapType {mapType} folder: " + experimentsFolderMapTypePath);
            Directory.Delete(experimentsFolderMapTypePath, true);
        }
        
        Directory.CreateDirectory(experimentsFolderMapTypePath);
        
        foreach (var file in Directory.GetFiles(experimentsCopyFolderPath, "*", SearchOption.AllDirectories))
        {
            if (!file.Contains(mapType))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(experimentsCopyFolderPath, file);
            var destinationPath = Path.Combine(experimentsFolderMapTypePath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Move(file, destinationPath);
        }
        Console.WriteLine($"Copy folder including {mapType} data to folder: " + experimentsFolderMapTypePath);

        return experimentsFolderMapTypePath;
        
    }
}