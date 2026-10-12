using System.Xml.Serialization;
using FolderScanner.Models;

namespace FolderScanner.Services;

public static class SettingsService
{
    public static AppSettings Load(string path)
    {
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var serializer = new XmlSerializer(typeof(AppSettings));
            using var fs = File.OpenRead(path);
            return (AppSettings?)serializer.Deserialize(fs) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings, string path)
    {
        var serializer = new XmlSerializer(typeof(AppSettings));
        using var fs = File.Create(path);
        serializer.Serialize(fs, settings);
    }
}