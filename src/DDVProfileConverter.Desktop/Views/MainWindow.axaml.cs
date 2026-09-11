using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DDVProfileConverter.Core.Conversion;

namespace DDVProfileConverter.Desktop.Views;

public partial class MainWindow : Window
{
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();

        DragDrop.AddDragOverHandler(
            DropZone,
            OnDragOver);

        DragDrop.AddDropHandler(
            DropZone,
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
        ChooseFileButton.IsEnabled = false;
        DetailsPanel.IsVisible = false;

        StatusText.Text =
            $"Processing {Path.GetFileName(path)}...";

        try
        {
            var result =
                await Task.Run(
                    () => ProfileFileConverter
                        .ConvertInPlace(path));

            ShowSuccess(result);
        }
        catch (Exception exception)
        {
            ShowError(
                $"Conversion failed: {exception.Message}");
        }
        finally
        {
            ChooseFileButton.IsEnabled = true;
            _isBusy = false;
        }
    }

    private void ShowSuccess(
        ProfileConversionResult result)
    {
        StatusText.Text =
            result.OutputFormat == ProfileFormat.PlainJson
                ? "Decrypted successfully."
                : "Encrypted successfully.";

        DirectionValue.Text =
            result.OutputFormat == ProfileFormat.PlainJson
                ? "Encrypted → readable JSON"
                : "Readable JSON → encrypted";

        PlayerValue.Text =
            string.IsNullOrWhiteSpace(
                result.Metadata.PlayerName)
                ? "—"
                : result.Metadata.PlayerName;

        VersionValue.Text =
            result.Metadata.Version.ToString();

        MdcValue.Text =
            string.IsNullOrWhiteSpace(
                result.Metadata.LastCustomIdOwner)
                ? "—"
                : $"mdc:{result.Metadata.LastCustomIdOwner}";

        ModifiedValue.Text =
            string.IsNullOrWhiteSpace(
                result.Metadata.Modified)
                ? "—"
                : result.Metadata.Modified;

        BackupValue.Text =
            result.Backup is null
                ? "Not created"
                : FormatBackup(result);

        DetailsPanel.IsVisible = true;
    }

    private static string FormatBackup(
        ProfileConversionResult result)
    {
        var backup =
            result.Backup!;

        var fileName =
            Path.GetFileName(backup.Path);

        return backup.Created
            ? $"backups/{fileName}"
            : $"backups/{fileName} (existing)";
    }

    private void ShowError(
        string message)
    {
        StatusText.Text = message;
        DetailsPanel.IsVisible = false;
    }
}
