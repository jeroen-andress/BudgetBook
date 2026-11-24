using System.Windows;
using Haushaltsbuch.Presentation.ViewModels;

namespace Haushaltsbuch.Wpf.Views;

public partial class BookingEditorWindow : Window
{
    public BookingEditorWindow(BookingEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Das ViewModel entscheidet, wann geschlossen wird; die View setzt nur das Dialogergebnis.
        viewModel.CloseRequested += confirmed => DialogResult = confirmed;
        Loaded += (_, _) => DescriptionBox.Focus();
    }
}
