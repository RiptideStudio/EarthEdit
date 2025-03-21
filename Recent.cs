using Newtonsoft.Json;
static class RecentFiles
{
    public static string ConfigPath => Path.Combine(Application.StartupPath, "recent.json");
    private static int MaxRecentCount = 15;

    public static List<string> Load()
    {
        if (!File.Exists(ConfigPath)) return new List<string>();
        return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(ConfigPath)) ?? new List<string>();
    }

    public static void Save(List<string> files)
    {
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(files, Formatting.Indented));
    }

    public static void Add(string path)
    {
        var files = Load();
        files.Remove(path);
        files.Insert(0, path); // Most recent at top
        while (files.Count > MaxRecentCount)
            files.RemoveAt(files.Count - 1);
        Save(files);
    }

}
