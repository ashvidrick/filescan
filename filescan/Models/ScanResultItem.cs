namespace FolderScanner.Models;

public class ScanResultItem
{
    public string Drive { get; set; } = "";        // "C:", "D:"
    public string FullPath { get; set; } = "";
    public string Name { get; set; } = "";
    public string FolderOrFile { get; set; } = ""; // "Папка" | "Файл"
    public long Size { get; set; }
    public DateTime Modified { get; set; }
}