using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IRacingSmartPlug.ViewModels;

namespace IRacingSmartPlug.Views;

public partial class AppsPage : UserControl
{
    public AppsPage() => InitializeComponent();

    // Clicking anywhere on the card header (except the action buttons/toggle,
    // which handle their own clicks) expands or collapses the inline editor.
    private void Header_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AppItemViewModel vm })
            vm.ToggleEditCommand.Execute(null);
    }
}
