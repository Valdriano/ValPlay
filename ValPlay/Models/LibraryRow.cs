using CommunityToolkit.Mvvm.ComponentModel;
using ValPlay.Helpers;
using ValPlay.Services;

namespace ValPlay.Models;

public partial class LibraryRow : ObservableObject
{
    public required LibraryEntryKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Path { get; init; }
    public MediaItem? Media { get; init; }

    [ObservableProperty]
    private bool _isSelected;

    public static LibraryRow FromFolder(MediaFolder folder, ILocalizationService localization) => new()
    {
        Kind = LibraryEntryKind.Folder,
        Title = folder.Name,
        Subtitle = localization.GetString("Library_FolderFiles", folder.ItemCount),
        Path = folder.Path
    };

    public static LibraryRow FromMedia(MediaItem media, ILocalizationService localization) => new()
    {
        Kind = LibraryEntryKind.Media,
        Title = media.DisplayTitle,
        Subtitle = media.GetLocalizedSubtitle(localization),
        Path = media.Path,
        Media = media
    };
}
