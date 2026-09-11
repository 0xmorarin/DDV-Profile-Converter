using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DDVProfileConverter.Core.Conversion;
using DDVProfileConverter.Core.Profile;

namespace DDVProfileConverter.Desktop.Views;

public partial class MainWindow : Window
{
    private const double CompactHeight = 430;
    private const double EncryptedSuccessHeight = 600;
    private const double DecryptedSuccessHeight = 700;

    private static readonly string BackupDirectory =
        Path.Combine(
            AppContext.BaseDirectory,
            "backups");

    private bool _isBusy;
    private string? _currentBackupDirectory;

    public MainWindow()
    {
        InitializeComponent();

        DragDrop.AddDragOverHandler(
            this,
            OnDragOver);

        DragDrop.AddDropHandler(
            this,
            OnDrop);
    }

    private async void OnChooseFileClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        if (!StorageProvider.CanOpen)
        {
            ShowError(
                "The file picker is not available on this platform.");

            return;
        }

        var files =
            await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Choose DDV profile.json",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType(
                            "JSON files")
                        {
                            Patterns =
                            [
                                "*.json"
                            ]
                        },
                        new FilePickerFileType(
                            "All files")
                        {
                            Patterns =
                            [
                                "*.*"
                            ]
                        }
                    ]
                });

        var file =
            files.FirstOrDefault();

        if (file is null)
        {
            return;
        }

        await ProcessFileAsync(
            file.Path.LocalPath);
    }

    private void OnOpenBackupFolderClick(
        object? sender,
        RoutedEventArgs e)
    {
        BackupOpenErrorText.IsVisible = false;

        if (string.IsNullOrWhiteSpace(_currentBackupDirectory) ||
            !Directory.Exists(_currentBackupDirectory))
        {
            BackupOpenErrorText.IsVisible = true;
            return;
        }

        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = _currentBackupDirectory,
                    UseShellExecute = true
                });
        }
        catch
        {
            BackupOpenErrorText.IsVisible = true;
        }
    }

    private void OnDragOver(
        object? sender,
        DragEventArgs e)
    {
        e.DragEffects =
            !_isBusy &&
            e.DataTransfer.Formats.Contains(
                DataFormat.File)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
    }

    private async void OnDrop(
        object? sender,
        DragEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        var items =
            e.DataTransfer
                .TryGetFiles()?
                .ToArray();

        if (items is not { Length: 1 } ||
            items[0] is not IStorageFile file)
        {
            ShowError(
                "Drop exactly one file.");

            return;
        }

        await ProcessFileAsync(
            file.Path.LocalPath);
    }

    private async Task ProcessFileAsync(
        string path)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        ShowProcessing(path);

        try
        {
            var result =
                await Task.Run(
                    () => ProfileFileConverter
                        .ConvertInPlace(
                            path,
                            BackupDirectory));

            ShowSuccess(result);
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ShowProcessing(string path)
    {
        Height = CompactHeight;
        _currentBackupDirectory = null;

        ProcessingFileText.Text =
            $"Converting {Path.GetFileName(path)}";

        ShowOnly(ProcessingPanel);
    }

    private void ShowSuccess(
        ProfileConversionResult result)
    {
        var decrypted =
            result.OutputFormat == ProfileFormat.PlainJson;

        SuccessTitleText.Text =
            decrypted
                ? "DECRYPTED"
                : "ENCRYPTED";

        SuccessSubtitleText.Text =
            decrypted
                ? "Profile decrypted successfully."
                : "Profile encrypted successfully.";

        if (result.Backup is not null)
        {
            _currentBackupDirectory =
                Path.GetDirectoryName(result.Backup.Path) ??
                BackupDirectory;

            BackupStatusText.Text =
                result.Backup.Created
                    ? "Backup created"
                    : "Backup already exists";

            BackupDirectoryValue.Text =
                _currentBackupDirectory;

            BackupOpenErrorText.IsVisible = false;
            BackupSection.IsVisible = true;
            Height = DecryptedSuccessHeight;
        }
        else
        {
            _currentBackupDirectory = null;
            BackupSection.IsVisible = false;
            Height = EncryptedSuccessHeight;
        }

        var metadata = result.Metadata;

        PlayerNameValue.Text =
            ValueOrDash(metadata.PlayerName);

        PlayerIdValue.Text =
            FormatPlayerId(metadata.LastCustomIdOwner);

        SaveVersionValue.Text =
            metadata.Version.ToString(
                CultureInfo.InvariantCulture);

        PlayTimeValue.Text =
            FormatPlayTime(metadata.TimePlayedInMinutes);

        CreatedValue.Text =
            FormatTimestamp(metadata.Created);

        LastModifiedValue.Text =
            FormatTimestamp(metadata.Modified);

        LastSavedOnValue.Text =
            FormatDevice(metadata.LastSaveDeviceType);

        ShowOnly(SuccessPanel);
    }

    private void ShowError(string message)
    {
        Height = CompactHeight;
        _currentBackupDirectory = null;

        ErrorMessageText.Text =
            string.IsNullOrWhiteSpace(message)
                ? "The selected file could not be converted."
                : message;

        ShowOnly(ErrorPanel);
    }

    private void ShowOnly(Control panel)
    {
        IdlePanel.IsVisible = ReferenceEquals(panel, IdlePanel);
        ProcessingPanel.IsVisible = ReferenceEquals(panel, ProcessingPanel);
        SuccessPanel.IsVisible = ReferenceEquals(panel, SuccessPanel);
        ErrorPanel.IsVisible = ReferenceEquals(panel, ErrorPanel);
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "—"
            : value;
    }

    private static string FormatPlayerId(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "—"
            : $"mdc:{value}";
    }

    private static string FormatPlayTime(long? minutes)
    {
        if (minutes is null)
        {
            return "—";
        }

        var hours = minutes.Value / 60;
        var remainingMinutes = minutes.Value % 60;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{hours}h {remainingMinutes}m");
    }

    private static string FormatTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "—";
        }

        if (value.Length >= 20 &&
            value.EndsWith(
                "Z",
                StringComparison.Ordinal) &&
            (value.Length == 20 || value[19] == '.') &&
            DateTime.TryParseExact(
                value[..19],
                "yyyy-MM-dd'T'HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var utcTimestamp))
        {
            return utcTimestamp.ToString(
                "yyyy-MM-dd HH:mm 'UTC'",
                CultureInfo.InvariantCulture);
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            return timestamp.ToString(
                "yyyy-MM-dd HH:mm 'UTC'",
                CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string FormatDevice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "—";
        }

        const string prefix = "DeviceType_";

        var displayName = value.StartsWith(
                prefix,
                StringComparison.Ordinal)
            ? value[prefix.Length..]
            : value;

        if (displayName.Equals(
                "Switch",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Nintendo Switch";
        }

        return displayName.Replace('_', ' ');
    }
}
