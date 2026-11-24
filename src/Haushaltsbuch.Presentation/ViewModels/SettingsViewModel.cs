using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>ViewModel des Tabs „Einstellungen“: Kategorien anlegen und löschen (in der Datenbank gespeichert).</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ICategoryService _categories;
    private readonly IDialogService _dialogs;

    public SettingsViewModel(ICategoryService categories, IDialogService dialogs)
    {
        _categories = categories;
        _dialogs = dialogs;
    }

    public ObservableCollection<Category> Categories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string newName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private Category? selectedCategory;

    [ObservableProperty] private string errorText = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;

    public async Task RefreshAsync()
    {
        try
        {
            var all = await _categories.GetAllAsync();
            Categories.Clear();
            foreach (var c in all) Categories.Add(c);
        }
        catch (Exception ex)
        {
            ErrorText = "Die Kategorien konnten nicht geladen werden: " + ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        try
        {
            var created = await _categories.AddAsync(NewName);
            await RefreshAsync();
            NewName = string.Empty;
            ErrorText = string.Empty;
            StatusText = $"Kategorie „{created.Name}“ wurde gespeichert.";
        }
        catch (ValidationException ex)
        {
            StatusText = string.Empty;
            ErrorText = ex.Message;
        }
        catch (Exception ex)
        {
            StatusText = string.Empty;
            ErrorText = "Die Kategorie konnte nicht gespeichert werden: " + ex.Message;
        }
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewName);

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (SelectedCategory is not { } category) return;
        if (!_dialogs.Confirm($"Kategorie „{category.Name}“ wirklich löschen?", "Kategorie löschen")) return;

        try
        {
            await _categories.DeleteAsync(category.Id);
            await RefreshAsync();
            ErrorText = string.Empty;
            StatusText = $"Kategorie „{category.Name}“ wurde gelöscht.";
        }
        catch (ValidationException ex)
        {
            StatusText = string.Empty;
            ErrorText = ex.Message;
        }
        catch (Exception ex)
        {
            StatusText = string.Empty;
            ErrorText = "Die Kategorie konnte nicht gelöscht werden: " + ex.Message;
        }
    }

    private bool CanDelete() => SelectedCategory is not null;
}
