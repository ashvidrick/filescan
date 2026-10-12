using System.Xml.Serialization;

namespace FolderScanner.Models;

[XmlRoot("Settings")]
public class AppSettings
{
    [XmlArray("Folders")]
    [XmlArrayItem("Folder")]
    public List<FolderEntry> Folders { get; set; } = new();

    /// <summary>Разрешённые расширения (например ".txt", ".log"). Пусто = все.</summary>
    [XmlArray("Extensions")]
    [XmlArrayItem("Extension")]
    public List<string> Extensions { get; set; } = new();

    /// <summary>Минимальный размер файла в байтах. 0 = без фильтра.</summary>
    [XmlElement("MinFileSize")]
    public long MinFileSize { get; set; } = 0;

    /// <summary>Показывать ли подпапки (с их суммарным размером) в списке файлов родителя.</summary>
    [XmlElement("IncludeSubfolderSizeInParent")]
    public bool IncludeSubfolderSizeInParent { get; set; } = true;

    /// <summary>Каталог для CSV. Пусто = рядом с исходной папкой.</summary>
    [XmlElement("OutputDirectory")]
    public string OutputDirectory { get; set; } = "";
}

public class FolderEntry
{
    [XmlAttribute("path")]
    public string Path { get; set; } = "";

    public override string ToString() => Path;
}