using FolderScanner.Models;
using FolderScanner.Services;

namespace FolderScanner;

/*public class MainForm : Form
{
    private readonly string _settingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

    private AppSettings _settings = new();

    // Элементы управления
    private ListBox _folderList = null!;
    private TextBox _newFolderBox = null!;
    private TextBox _extensionsBox = null!;
    private NumericUpDown _minSizeBox = null!;
    private CheckBox _includeSubfoldersCheck = null!;
    private TextBox _outputDirBox = null!;
    private ListBox _logBox = null!;
    private Button _scanButton = null!;

    public MainForm()
    {
        Text = "Сканер папок";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        LoadSettings();
    }

    // ---------- Построение интерфейса ----------
    private void BuildUi()
    {
        // Верхняя панель с кнопками
        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(6)
        };

        var loadBtn = new Button { Text = "Загрузить настройки", AutoSize = true };
        loadBtn.Click += (_, _) => LoadSettings();

        var saveBtn = new Button { Text = "Сохранить настройки", AutoSize = true };
        saveBtn.Click += (_, _) => SaveSettings();

        _scanButton = new Button { Text = "Сканировать", AutoSize = true };
        _scanButton.Click += async (_, _) => await RunScanAsync();

        topPanel.Controls.Add(loadBtn);
        topPanel.Controls.Add(saveBtn);
        topPanel.Controls.Add(_scanButton);

        // Вкладки
        var tabs = new TabControl { Dock = DockStyle.Fill };

        // --- Вкладка «Папки» ---
        var foldersTab = new TabPage("Папки");
        var folderPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };

        _folderList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false
        };

        var addFolderPanel = new Panel { Dock = DockStyle.Bottom, Height = 34 };
        _newFolderBox = new TextBox { Dock = DockStyle.Fill };
        var addBtn = new Button { Text = "Добавить", Dock = DockStyle.Right, Width = 90 };
        addBtn.Click += (_, _) =>
        {
            var p = _newFolderBox.Text.Trim();
            if (string.IsNullOrEmpty(p)) return;
            _settings.Folders.Add(new FolderEntry { Path = p });
            RefreshFolderList();
            _newFolderBox.Clear();
        };
        var delBtn = new Button { Text = "Удалить", Dock = DockStyle.Right, Width = 90 };
        delBtn.Click += (_, _) =>
        {
            if (_folderList.SelectedItem is FolderEntry fe)
            {
                _settings.Folders.Remove(fe);
                RefreshFolderList();
            }
        };
        addFolderPanel.Controls.Add(_newFolderBox);
        addFolderPanel.Controls.Add(addBtn);
        addFolderPanel.Controls.Add(delBtn);

        folderPanel.Controls.Add(_folderList);
        folderPanel.Controls.Add(addFolderPanel);
        foldersTab.Controls.Add(folderPanel);

        // --- Вкладка «Фильтры» ---
        var filtersTab = new TabPage("Фильтры");
        var filtersPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(10),
            AutoSize = true
        };
        filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        filtersPanel.Controls.Add(new Label
        {
            Text = "Расширения (через запятую, пусто = все):",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        _extensionsBox = new TextBox { Dock = DockStyle.Fill };
        filtersPanel.Controls.Add(_extensionsBox, 1, 0);

        filtersPanel.Controls.Add(new Label
        {
            Text = "Минимальный размер файла (байт, 0 = все):",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);
        _minSizeBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10_000_000_000,
            Width = 200
        };
        filtersPanel.Controls.Add(_minSizeBox, 1, 1);

        filtersPanel.Controls.Add(new Label
        {
            Text = "Каталог для CSV (пусто = рядом с папкой):",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 2);
        _outputDirBox = new TextBox { Dock = DockStyle.Fill };
        filtersPanel.Controls.Add(_outputDirBox, 1, 2);

        _includeSubfoldersCheck = new CheckBox
        {
            Text = "Показывать папки внутри папок (с суммарным размером)",
            AutoSize = true
        };
        filtersPanel.Controls.Add(_includeSubfoldersCheck, 1, 3);

        filtersTab.Controls.Add(filtersPanel);

        tabs.TabPages.Add(foldersTab);
        tabs.TabPages.Add(filtersTab);

        // Лог
        _logBox = new ListBox
        {
            Dock = DockStyle.Bottom,
            Height = 180,
            IntegralHeight = false,
            HorizontalScrollbar = true
        };

        Controls.Add(tabs);
        Controls.Add(_logBox);
        Controls.Add(topPanel);

        // Порядок Dock имеет значение: Fill должен быть добавлен ПЕРВЫМ по Dock-приоритету,
        // но в WinForms важен порядок Add. tabs уже добавлен до _logBox, что ок:
        // topPanel (Top) → tabs (Fill) → logBox (Bottom).
        // Чтобы logBox был ниже tabs, а не под ним, поменяем порядок:
        Controls.SetChildIndex(_logBox, 1);
        Controls.SetChildIndex(tabs, 0);
    }

    // ---------- Загрузка / сохранение ----------
    private void LoadSettings()
    {
        _settings = SettingsService.Load(_settingsPath);

        _extensionsBox.Text = string.Join(",", _settings.Extensions);
        _minSizeBox.Value = Math.Min(_minSizeBox.Maximum, Math.Max(0, _settings.MinFileSize));
        _outputDirBox.Text = _settings.OutputDirectory;
        _includeSubfoldersCheck.Checked = _settings.IncludeSubfolderSizeInParent;

        RefreshFolderList();
        Log($"Настройки загружены: {_settingsPath}");
    }

    private void SaveSettings()
    {
        // Считываем значения из UI
        _settings.Extensions = _extensionsBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _settings.MinFileSize = (long)_minSizeBox.Value;
        _settings.OutputDirectory = _outputDirBox.Text.Trim();
        _settings.IncludeSubfolderSizeInParent = _includeSubfoldersCheck.Checked;

        SettingsService.Save(_settings, _settingsPath);
        Log($"Настройки сохранены: {_settingsPath}");
    }

    private void RefreshFolderList()
    {
        _folderList.Items.Clear();
        foreach (var f in _settings.Folders)
            _folderList.Items.Add(f);
        _folderList.DisplayMember = nameof(FolderEntry.Path);
    }

    // ---------- Сканирование ----------
    private async Task RunScanAsync()
    {
        // Сохраняем текущие значения из UI в настройки
        SaveSettings();

        _scanButton.Enabled = false;
        Log("--- Начало сканирования ---");

        var progress = new Progress<string>(Log);

        try
        {
            var scanner = new ScannerService(_settings);
            var created = await Task.Run(() => scanner.ScanAll(progress));

            Log($"--- Готово. Создано файлов: {created.Count} ---");
            MessageBox.Show(this,
                $"Готово. Создано файлов: {created.Count}",
                "Сканирование",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log("Ошибка: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _scanButton.Enabled = true;
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(Log), message);
            return;
        }

        _logBox.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        _logBox.TopIndex = _logBox.Items.Count - 1;
    }
}
*/