using System.Text;
using FolderScanner.Models;

namespace FolderScanner.Services;

public class ScannerService
{
    private readonly AppSettings _settings;

    // Нормализованный набор заданных папок (полные пути, без хвостового слеша),
    // чтобы сравнивать без учёта регистра и формата.
    private readonly HashSet<string> _configuredFolders;

    public ScannerService(AppSettings settings)
    {
        _settings = settings;

        _configuredFolders = new HashSet<string>(
            settings.Folders
                .Select(f => NormalizePath(f.Path))
                .Where(p => !string.IsNullOrEmpty(p)),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Сканирует все папки и пишет один общий CSV. Возвращает путь к CSV.</summary>
    public string ScanAll(IProgress<string>? progress = null)
    {
        var allItems = new List<ScanResultItem>();

        foreach (var folder in _settings.Folders)
        {
            if (!Directory.Exists(folder.Path))
            {
                progress?.Report($"Пропущено (нет папки): {folder.Path}");
                continue;
            }

            progress?.Report($"Сканирую: {folder.Path}");
            var items = ScanFolder(folder.Path);
            progress?.Report($"  найдено элементов: {items.Count}");
            allItems.AddRange(items);
        }

        var csvPath = BuildCsvPath();
        WriteCsv(csvPath, allItems);
        progress?.Report($"CSV сохранён: {csvPath}");
        return csvPath;
    }

    private string BuildCsvPath()
    {
        var dir = string.IsNullOrWhiteSpace(_settings.OutputDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : _settings.OutputDirectory;

        Directory.CreateDirectory(dir);
        var name = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(dir, name);
    }

    private List<ScanResultItem> ScanFolder(string folder)
    {
        var result = new List<ScanResultItem>();
        var root = Path.GetPathRoot(folder) ?? "";

        // 1) Файлы верхнего уровня (без рекурсии)
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            if (!PassesFilters(file)) continue;

            FileInfo fi;
            try { fi = new FileInfo(file); }
            catch { continue; }

            result.Add(new ScanResultItem
            {
                Drive = root,
                FullPath = fi.FullName,
                Name = fi.Name,
                FolderOrFile = "Файл",
                Size = fi.Length,
                Modified = fi.LastWriteTime
            });
        }

        // 2) Подпапки
        foreach (var sub in Directory.EnumerateDirectories(folder))
        {
            // Логика дублирования:
            // - если IncludeSubfolderSizeInParent = true  → включаем все подпапки;
            // - если IncludeSubfolderSizeInParent = false → включаем подпапки,
            //   КРОМЕ тех, что сами перечислены в Folders (у них будет своя секция).
            if (!_settings.IncludeSubfolderSizeInParent)
            {
                if (_configuredFolders.Contains(NormalizePath(sub)))
                {
                    // Эта подпапка задана отдельно в настройках — пропускаем,
                    // чтобы не дублировать её в CSV родителя.
                    continue;
                }
            }

            DirectoryInfo di;
            try { di = new DirectoryInfo(sub); }
            catch { continue; }

            long size = GetDirectorySize(sub);

            if (_settings.MinFileSize > 0 && size < _settings.MinFileSize)
                continue;

            result.Add(new ScanResultItem
            {
                Drive = root,
                FullPath = di.FullName,
                Name = di.Name,
                FolderOrFile = "Папка",
                Size = size,
                Modified = di.LastWriteTime
            });
        }

        return result;
    }

    private bool PassesFilters(string filePath)
    {
        if (_settings.Extensions.Count > 0)
        {
            var ext = Path.GetExtension(filePath);
            bool match = _settings.Extensions.Any(e =>
                string.Equals(e, ext, StringComparison.OrdinalIgnoreCase));
            if (!match) return false;
        }

        if (_settings.MinFileSize > 0)
        {
            try
            {
                if (new FileInfo(filePath).Length < _settings.MinFileSize)
                    return false;
            }
            catch { return false; }
        }

        return true;
    }

    private static long GetDirectorySize(string path)
    {
        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(file).Length; }
                catch { }
            }
        }
        catch { }
        return total;
    }

    private static void WriteCsv(string path, List<ScanResultItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Диск;Путь;Название;Папка\\Файл;Размер (байт);Дата/Время");

        foreach (var it in items)
        {
            sb.Append(Escape(it.Drive)).Append(';')
              .Append(Escape(it.FullPath)).Append(';')
              .Append(Escape(it.Name)).Append(';')
              .Append(Escape(it.FolderOrFile)).Append(';')
              .Append(it.Size).Append(';')
              .Append(it.Modified.ToString("yyyy-MM-dd HH:mm:ss"))
              .AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') ||
            value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    /// <summary>
    /// Приводит путь к каноничному виду для сравнения:
    /// - полный путь (без относительных сегментов);
    /// - без хвостового слеша;
    /// - без учёта регистра (сравнение в HashSet задано OrdinalIgnoreCase).
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";

        string full;
        try { full = Path.GetFullPath(path); }
        catch { return ""; }

        // Убираем хвостовой слеш, но оставляем, если это корень диска ("C:\")
        if (full.Length > 3 && full.EndsWith(Path.DirectorySeparatorChar))
            full = full.TrimEnd(Path.DirectorySeparatorChar);

        return full;
    }
}